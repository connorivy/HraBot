using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;

namespace HraBot.Api.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    ILoggerFactory loggerFactory,
    VectorStore vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator
)
{
    public async Task IngestDataAsync(DirectoryInfo directory, string searchPattern)
    {
        var files = directory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);
        List<FileInfo> filesToAdd = [];

        // Initialize cache file in the same directory as the data
        var cacheFilePath = Path.Combine(directory.FullName, "uploaded_files_cache.txt");
        var uploadedFilesCache = new UploadedFilesCache(cacheFilePath);

        foreach (var file in files)
        {
            if (file.Extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Skipping txt file '{file}'.", file.FullName);
                continue;
            }
            if (file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Skipping json file '{file}'.", file.FullName);
                continue;
            }
            if (!uploadedFilesCache.IsUploaded(file.Name))
            {
                logger.LogInformation(
                    "No existing vectors for file '{file}'. Adding for ingestion.",
                    file.FullName
                );
                filesToAdd.Add(file);
                continue;
            }
        }

        using var writer = new VectorStoreWriter<string>(
            vectorStore,
            dimensionCount: IngestedChunk.VectorDimensions,
            new()
            {
                CollectionName = IngestedChunk.CollectionName,
                DistanceFunction = IngestedChunk.VectorDistanceFunction,
                IncrementalIngestion = false,
            }
        );

        using var pipeline = new IngestionPipeline<string>(
            reader: new DocumentReader(directory),
            chunker: new VerySlowSemanticSimilarityChunker(
                logger,
                embeddingGenerator,
                // new(TiktokenTokenizer.CreateForModel("gpt-4.1"))
                new(TiktokenTokenizer.CreateForModel("gpt-4o-mini"))
            ),
            writer: writer,
            loggerFactory: loggerFactory
        );

        await foreach (var result in pipeline.ProcessAsync(filesToAdd, CancellationToken.None))
        {
            logger.LogInformation(
                "Completed processing '{id}'. Succeeded: '{succeeded}'.",
                result.DocumentId,
                result.Succeeded
            );
            if (result.Succeeded)
            {
                uploadedFilesCache.AddFile(result.DocumentId);
            }
        }
    }
}

public class VerySlowSemanticSimilarityChunker(
    ILogger logger,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IngestionChunkerOptions options,
    float? thresholdPercentile = null
) : IngestionChunker<string>
{
    private readonly SemanticSimilarityChunker _innerChunker = new(
        embeddingGenerator,
        options,
        thresholdPercentile
    );

    /// <inheritdoc/>
    public override async IAsyncEnumerable<IngestionChunk<string>> ProcessAsync(
        IngestionDocument document,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await foreach (var chunk in _innerChunker.ProcessAsync(document, cancellationToken))
        {
            yield return chunk;
            logger.LogInformation(
                "Processed a chunk for document '{documentId}'.",
                document.Identifier
            );
            // Delay to work around rate limits
            await Task.Delay(10_000, cancellationToken);
        }
    }
}
