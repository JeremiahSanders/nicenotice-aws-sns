using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

public class ConfigurationSnsTopicResolver<TOptions>(IOptionsMonitor<TOptions> options) : ISnsTopicResolver
  where TOptions : ISnsTopicOptions
{
  public string GetTopicArn(EventStreamId stream)
  {
    return options.CurrentValue.Map.TryGetValue(stream.ToString(), out string? topicArnTemp)
      ? topicArnTemp
      : options.CurrentValue.Default
        ?? throw new EventRoutingException(stream, innerException: null);
  }
}
