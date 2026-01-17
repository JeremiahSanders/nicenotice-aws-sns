using Jds.TestingUtils.Randomization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class ConfigurationSnsTopicOptionsTests
{
  [Test]
  public void ISnsTopicOptions_Map_ReturnsStreams()
  {
    string defaultTopic = Randomizer.Shared.AwsSnsArn();
    Dictionary<string, string> streams = new()
    {
      {
        "application-lifecycle", Randomizer.Shared.AwsSnsArn()
      },
      {
        "infrastructure", Randomizer.Shared.AwsSnsArn()
      },
      {
        "user-session", Randomizer.Shared.AwsSnsArn()
      }
    };
    var options = new ConfigurationSnsTopicOptions
    {
      Default = defaultTopic,
      Streams = streams
    };
    var asInterface = (ISnsTopicOptions)options;

    IReadOnlyDictionary<string, string> actual = asInterface.Map;

    actual.ShouldBe(streams);
  }

  [Test]
  public void WhenResolvedFromOptions_ReturnsExpectedConfiguration()
  {
    const string configSectionBase = "MyApp:SnsTopicOptions";
    string defaultTopic = Randomizer.Shared.AwsSnsArn();
    Dictionary<string, string> streams = new()
    {
      {
        "application-lifecycle", Randomizer.Shared.AwsSnsArn()
      },
      {
        "infrastructure", Randomizer.Shared.AwsSnsArn()
      },
      {
        "user-session", Randomizer.Shared.AwsSnsArn()
      }
    };
    // Convert the logical arrangement data into an IConfiguration API dictionary.
    Dictionary<string, string?> configuration = streams
      .Select(kvp => new KeyValuePair<string, string?>(
          $"{configSectionBase}:{nameof(ConfigurationSnsTopicOptions.Streams)}:{kvp.Key}",
          kvp.Value
        )
      )
      .Append(
        new KeyValuePair<string, string?>(
          $"{configSectionBase}:{nameof(ConfigurationSnsTopicOptions.Default)}",
          defaultTopic
        )
      )
      .ToDictionary();
    IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
      .AddInMemoryCollection(configuration);
    IConfigurationRoot configRoot = configurationBuilder.Build();

    IServiceCollection serviceCollection = new ServiceCollection()
      .AddSingleton(configRoot)
      .AddSingleton<IConfiguration>(provider => provider.GetRequiredService<IConfigurationRoot>());

    serviceCollection
      .AddNiceNotice(nnBuilder => nnBuilder.DispatchToSns(configSectionBase, ServiceLifetime.Scoped));

    ServiceProvider provider = serviceCollection.BuildServiceProvider();

    // Act
    IOptions<ConfigurationSnsTopicOptions> asInterface =
      provider.GetRequiredService<IOptions<ConfigurationSnsTopicOptions>>();

    // Assert
    asInterface.Value.Default.ShouldBe(defaultTopic);
    asInterface.Value.Streams.ShouldBeEquivalentTo(streams);
  }
}
