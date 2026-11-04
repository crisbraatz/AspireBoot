namespace AspireBoot.Core.Integrations.ObjectStorage;

public record ListStoredObjectItemResult(string Key, DateTimeOffset LastModifiedAtUtc);
