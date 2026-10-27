using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Zuppeto.Infrastructure.Auth;
using Zuppeto.Infrastructure.Perplexity;
using Zuppeto.Infrastructure.Persistence;
using Zuppeto.Infrastructure.RabbitMq;
using Zuppeto.Infrastructure.Security;

namespace Backend.Activation.Tests;

public sealed class SecretManagementTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("PerplexityApiKey")]
    [InlineData("database_password")]
    [InlineData("ConnectionStrings__Zuppeto")]
    public void Sensitive_property_names_are_detected(string propertyName)
    {
        Assert.True(SensitiveDataPolicy.IsSensitiveName(propertyName));
    }

    [Fact]
    public void Disabled_Perplexity_does_not_retain_a_configured_credential()
    {
        var configuration = Configuration(
            ("Perplexity:Enabled", "false"),
            ("PERPLEXITY_API_KEY", "synthetic-credential"));
        var options = new PerplexityOptions();

        new PerplexityOptionsSetup(configuration).Configure(options);

        Assert.Null(options.ApiKey);
        Assert.True(new PerplexityOptionsValidator().Validate(null, options).Succeeded);
    }

    [Fact]
    public void Enabled_Perplexity_reads_a_synthetic_file_secret()
    {
        var secretFile = Path.Combine(Path.GetTempPath(), $"zuppeto-secret-{Guid.NewGuid():N}");
        File.WriteAllText(secretFile, "synthetic-file-credential\n");
        try
        {
            var configuration = Configuration(
                ("Perplexity:Enabled", "true"),
                ("PERPLEXITY_API_KEY_FILE", secretFile));
            var options = new PerplexityOptions();

            new PerplexityOptionsSetup(configuration).Configure(options);
            var validation = new PerplexityOptionsValidator().Validate(null, options);

            Assert.True(validation.Succeeded);
            Assert.Equal("synthetic-file-credential", options.ApiKey);
        }
        finally
        {
            File.Delete(secretFile);
        }
    }

    [Fact]
    public void Conflicting_secret_sources_fail_without_echoing_values()
    {
        const string firstCredential = "synthetic-first-credential";
        const string secondCredential = "synthetic-second-credential";
        var configuration = Configuration(
            ("Perplexity:Enabled", "true"),
            ("Perplexity:ApiKey", firstCredential),
            ("PERPLEXITY_API_KEY", secondCredential));
        var options = new PerplexityOptions();

        new PerplexityOptionsSetup(configuration).Configure(options);
        var validation = new PerplexityOptionsValidator().Validate(null, options);

        Assert.True(validation.Failed);
        Assert.DoesNotContain(firstCredential, validation.FailureMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(secondCredential, validation.FailureMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_Jwt_secret_fails_without_database_access()
    {
        var configuration = Configuration(
            ("Auth:Jwt:Issuer", "Zuppeto"),
            ("Auth:Jwt:Audience", "Zuppeto.Web"));

        var exception = Assert.Throws<InvalidOperationException>(() => AuthOptionsFactory.Create(configuration));

        Assert.Contains("Auth:Jwt:SigningKey", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Synthetic_Jwt_secret_builds_auth_configuration_without_database_access()
    {
        var configuration = Configuration(
            ("Auth:Jwt:Issuer", "Zuppeto"),
            ("Auth:Jwt:Audience", "Zuppeto.Web"),
            ("Auth:Jwt:SigningKey", new string('x', 48)));

        var options = AuthOptionsFactory.Create(configuration);

        Assert.Equal(48, options.Jwt.SigningKey.Length);
    }

    [Fact]
    public void Enabled_RabbitMq_requires_an_external_password_without_connecting()
    {
        var configuration = Configuration(
            ("RabbitMq:Enabled", "true"),
            ("RabbitMq:UserName", "synthetic-user"),
            ("RabbitMq:Password", ""));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddRabbitMq(configuration));

        Assert.DoesNotContain("synthetic-user", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ordinary_api_arguments_never_select_database_maintenance()
    {
        Assert.Equal(
            DatabaseMaintenanceMode.None,
            DatabaseMaintenanceRequest.Parse(["--urls", "http://0.0.0.0:5211"]));
    }

    [Theory]
    [InlineData("migrate", DatabaseMaintenanceMode.Migrate)]
    [InlineData("seed-development-data", DatabaseMaintenanceMode.SeedDevelopmentData)]
    [InlineData("initialize-development-data", DatabaseMaintenanceMode.InitializeDevelopmentData)]
    public void Database_maintenance_requires_an_explicit_known_command(
        string command,
        DatabaseMaintenanceMode expected)
    {
        Assert.Equal(
            expected,
            DatabaseMaintenanceRequest.Parse([$"--database-maintenance={command}"]));
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
}
