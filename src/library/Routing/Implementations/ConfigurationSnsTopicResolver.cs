using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   An implementation of <see cref="ISnsTopicResolver" /> that uses application configuration
///   (as accessed via a type which implements <see cref="ISnsTopicOptions" />)
///   to resolve topic ARNs.
/// </summary>
/// <param name="optionsMonitor">
///   A monitor providing access to <typeparamref name="TOptions" /> configuration.
///   Use of <see cref="IOptionsMonitor{TOptions}" /> allows this type to be registered as a singleton,
///   as the <see cref="IOptionsMonitor{TOptions}" /> interface provides a mechanism for observing changes
///   to the configuration throughout the application's lifetime.
/// </param>
/// <typeparam name="TOptions">A configuration object which implements <see cref="ISnsTopicOptions" />.</typeparam>
internal class ConfigurationSnsTopicResolver<TOptions>(IOptionsMonitor<TOptions> optionsMonitor) : ISnsTopicResolver
  where TOptions : ISnsTopicOptions
{
  /// <inheritdoc />
  public string GetTopicArn(EventStreamId stream)
  {
    return optionsMonitor.CurrentValue.Map.TryGetValue(stream.ToString(), out string? topicArnTemp)
      ? topicArnTemp
      : optionsMonitor.CurrentValue.Default
        ?? throw new SnsStreamRoutingException(stream, innerException: null);
  }
}
