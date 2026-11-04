using System.Reflection;
using System.Runtime.Loader;
using AspireBoot.Core;

namespace AspireBoot.Tests.Unit.Core;

[CollectionDefinition(nameof(CoreConstantsTests), DisableParallelization = true)]
[Collection(nameof(CoreConstantsTests))]
public class CoreConstantsTests
{
    [Theory]
    [InlineData(true, "Development", null)]
    [InlineData(true, "dEvElOpMeNt", "Production")]
    [InlineData(true, null, "Development")]
    [InlineData(false, "Production", "Development")]
    [InlineData(false, " ", "Development")]
    [InlineData(false, null, "Production")]
    [InlineData(false, null, null)]
    public void ShouldReturnTheDevelopmentFlagWithAspNetCoreTakingPrecedence(bool expected, string? aspNetCoreEnvironment = null, string? dotNetEnvironment = null)
    {
        string? originalAspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string? originalDotNetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", dotNetEnvironment);

            bool isDevelopment = ReadIsDevelopmentInAnIsolatedAssembly();

            Assert.Equal(expected, isDevelopment);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCoreEnvironment);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotNetEnvironment);
        }
    }

    private static bool ReadIsDevelopmentInAnIsolatedAssembly()
    {
        AssemblyLoadContext assemblyLoadContext = new(Guid.NewGuid().ToString(), true);
        try
        {
            Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(typeof(CoreConstants).Assembly.Location);
            Type type = assembly.GetType(typeof(CoreConstants).FullName!, true)!;
            PropertyInfo propertyInfo = type.GetProperty(nameof(CoreConstants.IsDevelopment))!;
            return Assert.IsType<bool>(propertyInfo.GetValue(null));
        }
        finally
        {
            assemblyLoadContext.Unload();
        }
    }
}
