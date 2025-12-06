using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Microsoft.Extensions.DataIngestion;

namespace HraBot.Api.Services.Ingestion;

public class PdfDocumentReader : IngestionDocumentReader
{
    private static IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        // Read the stream into a MemoryStream
        using var ms = new MemoryStream();
        await source.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        // Use PdfPig to open the PDF from the memory stream
        using var pdf = PdfDocument.Open(ms);
        var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();

        // Construct IngestionDocument and add sections for each chunk
        var doc = new IngestionDocument(identifier);
        // foreach (var chunk in chunks)
        foreach (var chunk in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(chunk.Text))
            {
                continue;
            }

            var paragraph = new IngestionDocumentParagraph(chunk.Text);
            var section = new IngestionDocumentSection();
            section.Elements.Add(paragraph);
            doc.Sections.Add(section);
        }
        return doc;
    }
}