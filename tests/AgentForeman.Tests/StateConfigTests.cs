using AgentForeman.Infrastructure.Configuration;

namespace AgentForeman.Tests;

public sealed class StateConfigTests
{
    [Fact]
    public void DatabaseConfigValidatesSuccessfullyWithPostgresqlProviderAndConnectionString()
    {
        using var tempFile = TempConfigFile.Create(ConfigLoaderTests.ValidConfigYaml);
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Config);
        Assert.Equal("postgresql", result.Config.Database.Provider);
        Assert.Equal(
            "Host=localhost;Port=5432;Database=agent_foreman;Username=agent_foreman;Password=agent_foreman",
            result.Config.Database.ConnectionString);
    }

    [Fact]
    public void DatabaseConfigFailsWhenProviderIsMissing()
    {
        using var tempFile = TempConfigFile.Create(ConfigLoaderTests.ValidConfigYaml.Replace("  provider: postgresql\n", string.Empty));
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.False(result.IsValid);
        Assert.Contains("database.provider is required.", result.Errors);
    }

    [Fact]
    public void DatabaseConfigFailsWhenConnectionStringIsMissing()
    {
        using var tempFile = TempConfigFile.Create(ConfigLoaderTests.ValidConfigYaml.Replace(
            "  connectionString: Host=localhost;Port=5432;Database=agent_foreman;Username=agent_foreman;Password=agent_foreman\n",
            string.Empty));
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.False(result.IsValid);
        Assert.Contains("database.connectionString is required.", result.Errors);
    }

    [Fact]
    public void UnsupportedStateProviderReturnsValidationError()
    {
        using var tempFile = TempConfigFile.Create(ConfigLoaderTests.ValidConfigYaml.Replace("provider: postgresql", "provider: sqlite"));
        var loader = new YamlAgentForemanConfigLoader();

        var result = loader.Load(tempFile.Path);

        Assert.False(result.IsValid);
        Assert.Contains("database.provider must be postgresql.", result.Errors);
    }
}
