using System.Diagnostics;

namespace AspireBoot.Infrastructure.Observability;

public static class DependencySources
{
    public static readonly ActivitySource Caching = new("AspireBoot.Infrastructure.Caching");
    public static readonly ActivitySource ObjectStorage = new("AspireBoot.Infrastructure.ObjectStorage");
    public static readonly ActivitySource Smtp = new("AspireBoot.Infrastructure.Smtp");
}
