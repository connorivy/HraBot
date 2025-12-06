// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.ML.Tokenizers;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DataIngestion;


namespace HraBot.Api.Services.Ingestion;

/// <summary>
/// Splits a <see cref="IngestionDocument"/> into chunks based on semantic similarity between its elements based on cosine distance of their embeddings.
/// </summary>
public sealed class SemanticSimilarityChunkerDebug : IngestionChunker<string>
{
    private readonly ElementsChunker _elementsChunker;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly float _thresholdPercentile;

    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSimilarityChunkerDebug"/> class.
    /// </summary>
    /// <param name="embeddingGenerator">Embedding generator.</param>
    /// <param name="options">The options for the chunker.</param>
    /// <param name="thresholdPercentile">Threshold percentile to consider the chunks to be sufficiently similar. 95th percentile will be used if not specified.</param>
    public SemanticSimilarityChunkerDebug(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IngestionChunkerOptions options,
        float? thresholdPercentile = null)
    {
        _embeddingGenerator = embeddingGenerator;
        _elementsChunker = new(options);

        if (thresholdPercentile < 0f || thresholdPercentile > 100f)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdPercentile), "Threshold percentile must be between 0 and 100.");
        }

        _thresholdPercentile = thresholdPercentile ?? 95.0f;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(IngestionDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        List<(IngestionDocumentElement, float)> distances = await CalculateDistancesAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var chunk in MakeChunks(document, distances))
        {
            yield return chunk;
        }
    }

    private async Task<List<(IngestionDocumentElement element, float distance)>> CalculateDistancesAsync(IngestionDocument documents, CancellationToken cancellationToken)
    {
        List<(IngestionDocumentElement element, float distance)> elementDistances = [];
        List<string> semanticContents = [];

        var x = documents.EnumerateContent().ToList();
        foreach (IngestionDocumentElement element in documents.EnumerateContent())
        {
            string? semanticContent = element is IngestionDocumentImage img
                ? img.AlternativeText ?? img.Text
                : element.GetMarkdown();

            if (!string.IsNullOrEmpty(semanticContent))
            {
                elementDistances.Add((element, default));
                semanticContents.Add(semanticContent!);
            }
        }

        if (elementDistances.Count > 0)
        {
            var embeddings = await _embeddingGenerator.GenerateAsync(semanticContents, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (embeddings.Count != elementDistances.Count)
            {
                throw new InvalidOperationException("The number of embeddings returned does not match the number of document elements.");
            }

            for (int i = 0; i < elementDistances.Count - 1; i++)
            {
                float distance = 1 - TensorPrimitives.CosineSimilarity(embeddings[i].Vector.Span, embeddings[i + 1].Vector.Span);
                elementDistances[i] = (elementDistances[i].element, distance);
            }
        }

        return elementDistances;
    }

    private IEnumerable<IngestionChunk<string>> MakeChunks(IngestionDocument document, List<(IngestionDocumentElement element, float distance)> elementDistances)
    {
        float distanceThreshold = Percentile(elementDistances);

        List<IngestionDocumentElement> elementAccumulator = [];
        string context = string.Empty;
        for (int i = 0; i < elementDistances.Count; i++)
        {
            var (element, distance) = elementDistances[i];

            elementAccumulator.Add(element);
            if (distance > distanceThreshold || i == elementDistances.Count - 1)
            {
                foreach (var chunk in _elementsChunker.Process(document, context, elementAccumulator))
                {
                    yield return chunk;
                }
                elementAccumulator.Clear();
            }
        }
    }

    private float Percentile(List<(IngestionDocumentElement element, float distance)> elementDistances)
    {
        if (elementDistances.Count == 0)
        {
            return 0f;
        }
        else if (elementDistances.Count == 1)
        {
            return elementDistances[0].distance;
        }

        float[] sorted = new float[elementDistances.Count];
        for (int elementIndex = 0; elementIndex < elementDistances.Count; elementIndex++)
        {
            sorted[elementIndex] = elementDistances[elementIndex].distance;
        }
        Array.Sort(sorted);

        float i = (_thresholdPercentile / 100f) * (sorted.Length - 1);
        int i0 = (int)i;
        int i1 = Math.Min(i0 + 1, sorted.Length - 1);
        return sorted[i0] + ((i - i0) * (sorted[i1] - sorted[i0]));
    }
}

internal sealed class ElementsChunker
{
    private readonly Tokenizer _tokenizer;
    private readonly int _maxTokensPerChunk;
    private readonly StringBuilder _currentChunk;

    internal ElementsChunker(IngestionChunkerOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _tokenizer = options.Tokenizer;
        _maxTokensPerChunk = options.MaxTokensPerChunk;

        // Token count != character count, but StringBuilder will grow as needed.
        _currentChunk = new(capacity: _maxTokensPerChunk);
    }

    // Goals:
    // 1. Create chunks that do not exceed _maxTokensPerChunk when tokenized.
    // 2. Maintain context in each chunk.
    // 3. If a single IngestionDocumentElement exceeds _maxTokensPerChunk, it should be split intelligently (e.g., paragraphs can be split into sentences, tables into rows).
    internal IEnumerable<IngestionChunk<string>> Process(IngestionDocument document, string context, List<IngestionDocumentElement> elements)
    {
        // Not using yield return here as we use ref structs.
        List<IngestionChunk<string>> chunks = [];

        int contextTokenCount = CountTokens(context.AsSpan());
        int totalTokenCount = contextTokenCount;

        // If the context itself exceeds the max tokens per chunk, we can't do anything.
        if (contextTokenCount >= _maxTokensPerChunk)
        {
            ThrowTokenCountExceeded();
        }

        _currentChunk.Append(context);

        for (int elementIndex = 0; elementIndex < elements.Count; elementIndex++)
        {
            IngestionDocumentElement element = elements[elementIndex];
            string? semanticContent = element switch
            {
                // Image exposes:
                // - Markdown: ![Alt Text](url) which is not very useful for embedding.
                // - AlternativeText: usually a short description of the image, can be null or empty. It is usually less than 50 words.
                // - Text: result of OCR, can be longer, but also can be null or empty. It can be several hundred words.
                // We prefer  AlternativeText over Text, as it is usually more relevant.
                IngestionDocumentImage image => image.AlternativeText ?? image.Text,
                _ => element.GetMarkdown()
            };

            if (string.IsNullOrEmpty(semanticContent))
            {
                continue; // An image can come with Markdown, but no AlternativeText or Text.
            }

            int elementTokenCount = CountTokens(semanticContent.AsSpan());
            if (elementTokenCount + totalTokenCount <= _maxTokensPerChunk)
            {
                totalTokenCount += elementTokenCount;
                AppendNewLineAndSpan(_currentChunk, semanticContent.AsSpan());
            }
            else if (element is IngestionDocumentTable table)
            {
                ValueStringBuilder tableBuilder = new(initialCapacity: 8000);

                try
                {
                    AddMarkdownTableRow(table, rowIndex: 0, ref tableBuilder);
                    AddMarkdownTableSeparatorRow(columnCount: table.Cells.GetLength(1), ref tableBuilder);

                    int headerLength = tableBuilder.Length;
                    int headerTokenCount = CountTokens(tableBuilder.AsSpan());

                    // We can't respect the limit if context and header themselves use more tokens.
                    if (contextTokenCount + headerTokenCount >= _maxTokensPerChunk)
                    {
                        ThrowTokenCountExceeded();
                    }

                    if (headerTokenCount + totalTokenCount >= _maxTokensPerChunk)
                    {
                        // We can't add the header row, so commit what we have accumulated so far.
                        Commit();
                    }

                    totalTokenCount += headerTokenCount;
                    int tableLength = headerLength;

                    int rowCount = table.Cells.GetLength(0);
                    for (int rowIndex = 1; rowIndex < rowCount; rowIndex++)
                    {
                        AddMarkdownTableRow(table, rowIndex, ref tableBuilder);

                        int lastRowTokens = CountTokens(tableBuilder.AsSpan(tableLength));

                        // Appending this row would exceed the limit.
                        if (totalTokenCount + lastRowTokens > _maxTokensPerChunk)
                        {
                            // We append the table as long as it's not just the header.
                            if (rowIndex != 1)
                            {
                                AppendNewLineAndSpan(_currentChunk, tableBuilder.AsSpan(0, tableLength - Environment.NewLine.Length));
                            }

                            // And commit the table we built so far.
                            Commit();

                            // Erase previous rows and keep only the header.
                            tableBuilder.Length = headerLength;
                            tableLength = headerLength;
                            totalTokenCount += headerTokenCount;

                            if (totalTokenCount + lastRowTokens > _maxTokensPerChunk)
                            {
                                // This row is simply too big even for a fresh chunk:
                                ThrowTokenCountExceeded();
                            }

                            AddMarkdownTableRow(table, rowIndex, ref tableBuilder);
                        }

                        tableLength = tableBuilder.Length;
                        totalTokenCount += lastRowTokens;
                    }

                    AppendNewLineAndSpan(_currentChunk, tableBuilder.AsSpan(0, tableLength - Environment.NewLine.Length));
                }
                finally
                {
                    tableBuilder.Dispose();
                }
            }
            else
            {
                ReadOnlySpan<char> remainingContent = semanticContent.AsSpan();

                while (!remainingContent.IsEmpty)
                {
                    int index = _tokenizer.GetIndexByTokenCount(
                        text: remainingContent,
                        maxTokenCount: _maxTokensPerChunk - totalTokenCount,
                        out string? normalizedText,
                        out int tokenCount,
                        considerNormalization: false); // We don't normalize, just append as-is to keep original content.

                    // some tokens fit
                    if (index > 0)
                    {
                        // We could try to split by sentences or other delimiters, but it's complicated.
                        // For simplicity, we will just split at the last new line that fits.
                        // Our promise is not to go over the max token count, not to create perfect chunks.
                        int newLineIndex = remainingContent.Slice(0, index).LastIndexOf('\n');
                        if (newLineIndex > 0)
                        {
                            index = newLineIndex + 1; // We want to include the new line character (works for "\r\n" as well).
                            tokenCount = CountTokens(remainingContent.Slice(0, index));
                        }

                        totalTokenCount += tokenCount;
                        ReadOnlySpan<char> spanToAppend = remainingContent.Slice(0, index);
                        AppendNewLineAndSpan(_currentChunk, spanToAppend);
                        remainingContent = remainingContent.Slice(index);
                    }
                    else if (totalTokenCount == contextTokenCount)
                    {
                        // We are at the beginning of a chunk, and even a single token does not fit.
                        ThrowTokenCountExceeded();
                    }

                    if (!remainingContent.IsEmpty)
                    {
                        Commit();
                    }
                }
            }

            if (totalTokenCount == _maxTokensPerChunk)
            {
                Commit();
            }
        }

        if (totalTokenCount > contextTokenCount)
        {
            chunks.Add(new(_currentChunk.ToString(), document, context));
        }

        _currentChunk.Clear();

        return chunks;

        void Commit()
        {
            chunks.Add(new(_currentChunk.ToString(), document, context));

            // We keep the context in the current chunk as it's the same for all elements.
            _currentChunk.Remove(
                startIndex: context.Length,
                length: _currentChunk.Length - context.Length);
            totalTokenCount = contextTokenCount;
        }

        static void ThrowTokenCountExceeded()
            => throw new InvalidOperationException("Can't fit in the current chunk. Consider increasing max tokens per chunk.");
    }

    private static void AppendNewLineAndSpan(StringBuilder stringBuilder, ReadOnlySpan<char> chars)
    {
        // Don't start an empty chunk (no context provided) with a new line.
        if (stringBuilder.Length > 0)
        {
            stringBuilder.AppendLine();
        }

#if NET
        stringBuilder.Append(chars);
#else
        stringBuilder.Append(chars.ToString());
#endif
    }

    private static void AddMarkdownTableRow(IngestionDocumentTable table, int rowIndex, ref ValueStringBuilder vsb)
    {
        for (int columnIndex = 0; columnIndex < table.Cells.GetLength(1); columnIndex++)
        {
            vsb.Append('|');
            vsb.Append(' ');
            string? cellContent = table.Cells[rowIndex, columnIndex] switch
            {
                null => null,
                IngestionDocumentImage img => img.AlternativeText ?? img.Text,
                IngestionDocumentElement other => other.GetMarkdown()
            };
            vsb.Append(cellContent);
            vsb.Append(' ');
        }

        vsb.Append('|');
        vsb.Append(Environment.NewLine);
    }

    private static void AddMarkdownTableSeparatorRow(int columnCount, ref ValueStringBuilder vsb)
    {
        const int DashCount = 3; // The dash count does not need to match the header length.
        for (int columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            vsb.Append('|');
            vsb.Append(' ');
            vsb.Append('-', DashCount);
            vsb.Append(' ');
        }

        vsb.Append('|');
        vsb.Append(Environment.NewLine);
    }

    private int CountTokens(ReadOnlySpan<char> input)
        => _tokenizer.CountTokens(input, considerNormalization: false);
}

internal ref partial struct ValueStringBuilder
{
    private char[]? _arrayToReturnToPool;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturnToPool = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public ValueStringBuilder(int initialCapacity)
    {
        _arrayToReturnToPool = ArrayPool<char>.Shared.Rent(initialCapacity);
        _chars = _arrayToReturnToPool;
        _pos = 0;
    }

    public int Length
    {
        get => _pos;
        set
        {
            Debug.Assert(value >= 0);
            Debug.Assert(value <= _chars.Length);
            _pos = value;
        }
    }

    public int Capacity => _chars.Length;

    public void EnsureCapacity(int capacity)
    {
        // This is not expected to be called this with negative capacity
        Debug.Assert(capacity >= 0);

        // If the caller has a bug and calls this with negative capacity, make sure to call Grow to throw an exception.
        if ((uint)capacity > (uint)_chars.Length)
        {
            Grow(capacity - _pos);
        }
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// Does not ensure there is a null char after <see cref="Length"/>
    /// This overload is pattern matched in the C# 7.3+ compiler so you can omit
    /// the explicit method call, and write eg "fixed (char* c = builder)"
    /// </summary>
    public ref char GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(_chars);
    }

    /// <summary>
    /// Get a pinnable reference to the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ref char GetPinnableReference(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }
        return ref MemoryMarshal.GetReference(_chars);
    }

    public ref char this[int index]
    {
        get
        {
            Debug.Assert(index < _pos);
            return ref _chars[index];
        }
    }

    public override string ToString()
    {
        string s = _chars.Slice(0, _pos).ToString();
        Dispose();
        return s;
    }

    /// <summary>Returns the underlying storage of the builder.</summary>
    public Span<char> RawChars => _chars;

    /// <summary>
    /// Returns a span around the contents of the builder.
    /// </summary>
    /// <param name="terminate">Ensures that the builder has a null char after <see cref="Length"/></param>
    public ReadOnlySpan<char> AsSpan(bool terminate)
    {
        if (terminate)
        {
            EnsureCapacity(Length + 1);
            _chars[Length] = '\0';
        }
        return _chars.Slice(0, _pos);
    }

    public ReadOnlySpan<char> AsSpan() => _chars.Slice(0, _pos);
    public ReadOnlySpan<char> AsSpan(int start) => _chars.Slice(start, _pos - start);
    public ReadOnlySpan<char> AsSpan(int start, int length) => _chars.Slice(start, length);

    public bool TryCopyTo(Span<char> destination, out int charsWritten)
    {
        if (_chars.Slice(0, _pos).TryCopyTo(destination))
        {
            charsWritten = _pos;
            Dispose();
            return true;
        }
        else
        {
            charsWritten = 0;
            Dispose();
            return false;
        }
    }

    public void Insert(int index, char value, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
        _chars.Slice(index, count).Fill(value);
        _pos += count;
    }

    public void Insert(int index, string? s)
    {
        if (s == null)
        {
            return;
        }

        int count = s.Length;

        if (_pos > (_chars.Length - count))
        {
            Grow(count);
        }

        int remaining = _pos - index;
        _chars.Slice(index, remaining).CopyTo(_chars.Slice(index + count));
        s.AsSpan().CopyTo(_chars.Slice(index));
        _pos += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        int pos = _pos;
        if ((uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? s)
    {
        if (s == null)
        {
            return;
        }

        int pos = _pos;
        if (s.Length == 1 && (uint)pos < (uint)_chars.Length) // very common case, e.g. appending strings from NumberFormatInfo like separators, percent symbols, etc.
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    private void AppendSlow(string s)
    {
        int pos = _pos;
        if (pos > _chars.Length - s.Length)
        {
            Grow(s.Length);
        }

        s.AsSpan().CopyTo(_chars.Slice(pos));
        _pos += s.Length;
    }

    public void Append(char c, int count)
    {
        if (_pos > _chars.Length - count)
        {
            Grow(count);
        }

        Span<char> dst = _chars.Slice(_pos, count);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = c;
        }
        _pos += count;
    }

    public unsafe void Append(char* value, int length)
    {
        int pos = _pos;
        if (pos > _chars.Length - length)
        {
            Grow(length);
        }

        Span<char> dst = _chars.Slice(_pos, length);
        for (int i = 0; i < dst.Length; i++)
        {
            dst[i] = *value++;
        }
        _pos += length;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        int pos = _pos;
        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars.Slice(_pos));
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<char> AppendSpan(int length)
    {
        int origPos = _pos;
        if (origPos > _chars.Length - length)
        {
            Grow(length);
        }

        _pos = origPos + length;
        return _chars.Slice(origPos, length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    /// <summary>
    /// Resize the internal buffer either by doubling current buffer size or
    /// by adding <paramref name="additionalCapacityBeyondPos"/> to
    /// <see cref="_pos"/> whichever is greater.
    /// </summary>
    /// <param name="additionalCapacityBeyondPos">
    /// Number of chars requested beyond current position.
    /// </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacityBeyondPos)
    {
        Debug.Assert(additionalCapacityBeyondPos > 0);
        Debug.Assert(_pos > _chars.Length - additionalCapacityBeyondPos, "Grow called incorrectly, no resize is needed.");

        // Make sure to let Rent throw an exception if the caller has a bug and the desired capacity is negative
        char[] poolArray = ArrayPool<char>.Shared.Rent((int)Math.Max((uint)(_pos + additionalCapacityBeyondPos), (uint)_chars.Length * 2));

        _chars.Slice(0, _pos).CopyTo(poolArray);

        char[]? toReturn = _arrayToReturnToPool;
        _chars = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        char[]? toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}