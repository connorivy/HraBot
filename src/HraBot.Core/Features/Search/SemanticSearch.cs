using HraBot.Api.Services.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;

namespace HraBot.Api.Services;

public class SemanticSearch(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
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
            10,
            new VectorSearchOptions<IngestedChunk>
            {
                //     Filter = documentIdFilter is { Length: > 0 }
                //         ? record => record.DocumentId == documentIdFilter
                //         : null,
            }
        );
        return await nearest.Select(result => result.Record.ToDto()).ToListAsync();
    }
}
