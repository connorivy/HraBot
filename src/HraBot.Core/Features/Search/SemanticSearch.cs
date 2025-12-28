using HraBot.Api.Services.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

namespace HraBot.Api.Services;

public class SemanticSearch(
    // VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    VectorStoreCollection<object, Dictionary<string, object?>> vectorCollection,
    [FromKeyedServices("ingestion_directory")] DirectoryInfo ingestionDirectory,
    DataIngestor dataIngestor
)
{
    private Task? _ingestionTask;

    public async Task LoadDocumentsAsync() =>
        await (
            _ingestionTask ??= dataIngestor.IngestDataAsync(
                ingestionDirectory,
                searchPattern: "*.*"
            )
        );

    public async Task<IReadOnlyList<IngestedChunkDto>> SearchAsync(
        string searchText
    // string? documentIdFilter
    // int maxResults
    )
    {
        var nearest = vectorCollection.SearchAsync(
            searchText,
            6,
            new()
        // new VectorSearchOptions<IngestedChunk>
        // {
        //     //     Filter = documentIdFilter is { Length: > 0 }
        //     //         ? record => record.DocumentId == documentIdFilter
        //     //         : null,
        // }
        );
        var x = await nearest.ToListAsync();
        return await nearest
            .Select(result =>
            {
                var docIdObj =
                    result.Record.GetValueOrDefault("documentid")
                    ?? throw new InvalidOperationException(
                        "Retreived record does not have a documentid property"
                    );
                var contentObj =
                    result.Record.GetValueOrDefault("content")
                    ?? throw new InvalidOperationException(
                        "Retreived record does not have a content property"
                    );
                return new IngestedChunkDto(
                    docIdObj as string
                        ?? throw new InvalidOperationException(
                            $"Unable document id of type {docIdObj.GetType()} to string"
                        ),
                    contentObj as string
                        ?? throw new InvalidOperationException(
                            $"Unable content of type {contentObj.GetType()} to string"
                        )
                );
            })
            .ToListAsync();
    }
}
