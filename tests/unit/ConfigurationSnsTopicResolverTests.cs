using Jds.TestingUtils.Randomization;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class ConfigurationSnsTopicResolverTests
{
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
