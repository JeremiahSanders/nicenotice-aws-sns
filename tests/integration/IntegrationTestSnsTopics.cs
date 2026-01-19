namespace Jds.NiceNotice.Aws.Sns.Tests.Integration;

public record IntegrationTestSnsTopics
{
  public const string Key = "Tests:Topics";

  public string Errors { get; init; } = string.Empty;
  public string HighPriority { get; init; } = string.Empty;
  public string Primary { get; init; } = string.Empty;
}

internal static class IntegrationTestConfiguration
{
  public static IConfigurationRoot LoadConfiguration()
  {
    IConfigurationRoot configuration = new ConfigurationBuilder()
      .AddJsonFile(path: "appsettings.local.json", optional: true, reloadOnChange: true)
      .AddEnvironmentVariables()
      .Build();

    return configuration;
  }
}
