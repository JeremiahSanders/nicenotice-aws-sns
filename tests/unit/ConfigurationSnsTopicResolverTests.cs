using Jds.TestingUtils.Randomization;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class ConfigurationSnsTopicResolverTests
{
  private const string exampleStreamNameFromEnvironment = "MAXIMUMITEMSPERBATCH";
  private const string exampleStreamNameFromInternalConstant = "MaximumItemsPerBatch";
  private const string exampleStreamNameFromJsonConfiguration = "maximumItemsPerBatch";


  [Test]
  public void CorrectlyRoutesToConfiguredTopics()
  {
    var sourceOptions = new ConfigurationSnsTopicOptions
    {
      Default = Randomizer.Shared.AwsSnsArn(),
      Streams = new Dictionary<string, string>
      {
        {
          "system", Randomizer.Shared.AwsSnsArn()
        },
        {
          "app-lifetime", Randomizer.Shared.AwsSnsArn()
        }
      }
    };
    OptionsMonitor<ConfigurationSnsTopicOptions> optionsMonitor = CreateOptionsMonitor(sourceOptions);

    ISnsTopicResolver resolver = TopicResolvers.Configuration(optionsMonitor);

    // Act
    IEnumerable<(string Actual, string Expected)> results = sourceOptions.Streams
      .Select(kvp => (Actual: resolver.GetTopicArn(EventStreamId.From(kvp.Key)), Expected: kvp.Value));
    // Assert
    results.ShouldAllBe(tuple => tuple.Actual == tuple.Expected);
  }

  [Test]
  public void CorrectlyRoutesToDefaultTopic()
  {
    var sourceOptions = new ConfigurationSnsTopicOptions
    {
      Default = Randomizer.Shared.AwsSnsArn(),
      Streams = new Dictionary<string, string>
      {
        {
          "system", Randomizer.Shared.AwsSnsArn()
        },
        {
          "app-lifetime", Randomizer.Shared.AwsSnsArn()
        }
      }
    };
    OptionsMonitor<ConfigurationSnsTopicOptions> optionsMonitor = CreateOptionsMonitor(sourceOptions);

    ISnsTopicResolver resolver = TopicResolvers.Configuration(optionsMonitor);

    // Act
    resolver
      .GetTopicArn(EventStreamId.From(Guid.NewGuid().ToString()))
      // Assert
      .ShouldBe(sourceOptions.Default);
  }

  [Test]
  [Arguments(exampleStreamNameFromEnvironment, exampleStreamNameFromEnvironment, true)]
  [Arguments(exampleStreamNameFromEnvironment, exampleStreamNameFromJsonConfiguration, true)]
  [Arguments(exampleStreamNameFromEnvironment, exampleStreamNameFromInternalConstant, true)]
  [Arguments(exampleStreamNameFromJsonConfiguration, exampleStreamNameFromEnvironment, true)]
  [Arguments(exampleStreamNameFromJsonConfiguration, exampleStreamNameFromJsonConfiguration, true)]
  [Arguments(exampleStreamNameFromJsonConfiguration, exampleStreamNameFromInternalConstant, true)]
  [Arguments(exampleStreamNameFromInternalConstant, exampleStreamNameFromEnvironment, true)]
  [Arguments(exampleStreamNameFromInternalConstant, exampleStreamNameFromJsonConfiguration, true)]
  [Arguments(exampleStreamNameFromInternalConstant, exampleStreamNameFromInternalConstant, true)]
  [Arguments(exampleStreamNameFromEnvironment, "nonexistentStream", false)]
  public void ResolvesFromConfigurationMapCaseInsensitively(
    string configuredStreamName,
    string requestStreamName,
    bool shouldReturnRoute
  )
  {
    string defaultArn = Randomizer.Shared.AwsSnsArn();
    string configuredArn = Randomizer.Shared.AwsSnsArn();
    var sourceOptions = new ConfigurationSnsTopicOptions
    {
      Default = defaultArn,
      Streams = new Dictionary<string, string>
      {
        {
          configuredStreamName, configuredArn
        }
      }
    };
    OptionsMonitor<ConfigurationSnsTopicOptions> optionsMonitor = CreateOptionsMonitor(sourceOptions);

    ISnsTopicResolver resolver = TopicResolvers.Configuration(optionsMonitor);

    // Act
    string result = resolver.GetTopicArn(EventStreamId.From(requestStreamName));

    // Assert
    if (shouldReturnRoute)
    {
      result.ShouldBe(configuredArn);
      result.ShouldNotBe(defaultArn);
    }
    else
    {
      result.ShouldBe(defaultArn);
      result.ShouldNotBe(configuredArn);
    }
  }

  [Test]
  public void ThrowsWhenNoDefaultTopicIsConfiguredAndCannotResolveTopic()
  {
    var sourceOptions = new ConfigurationSnsTopicOptions();
    OptionsMonitor<ConfigurationSnsTopicOptions> optionsMonitor = CreateOptionsMonitor(sourceOptions);

    ISnsTopicResolver resolver = TopicResolvers.Configuration(optionsMonitor);

    Func<string> action = () => resolver.GetTopicArn(EventStreamId.From(Guid.NewGuid().ToString()));
    action.ShouldThrow<SnsStreamRoutingException>();
  }

  private static OptionsMonitor<ConfigurationSnsTopicOptions> CreateOptionsMonitor(
    ConfigurationSnsTopicOptions sourceOptions)
  {
    IOptionsFactory<ConfigurationSnsTopicOptions> factory = new OptionsFactory<ConfigurationSnsTopicOptions>(
      new List<IConfigureOptions<ConfigurationSnsTopicOptions>>
      {
        new ConfigureOptions<ConfigurationSnsTopicOptions>(optionsToConfigure =>
          {
            optionsToConfigure.Default = sourceOptions.Default;
            optionsToConfigure.Streams = sourceOptions.Streams;
          }
        )
      },
      new List<IPostConfigureOptions<ConfigurationSnsTopicOptions>>()
    );
    IEnumerable<IOptionsChangeTokenSource<ConfigurationSnsTopicOptions>> sources = [];
    IOptionsMonitorCache<ConfigurationSnsTopicOptions> cache = new OptionsCache<ConfigurationSnsTopicOptions>();
    OptionsMonitor<ConfigurationSnsTopicOptions> optionsMonitor = new(factory, sources, cache);

    return optionsMonitor;
  }
}
