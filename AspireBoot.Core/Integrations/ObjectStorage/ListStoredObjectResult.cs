namespace AspireBoot.Core.Integrations.ObjectStorage;

public record ListStoredObjectResult(IReadOnlyList<ListStoredObjectItemResult> Items, string? ContinuationToken = null);
