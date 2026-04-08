using System.Collections.Concurrent;

using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   An implementation of <see cref="ISnsTopicResolver" /> that uses application configuration
///   (as accessed via a type which implements <see cref="ISnsTopicOptions" />)
///   to resolve topic ARNs.
/// </summary>
/// <typeparam name="TOptions">A configuration object which implements <see cref="ISnsTopicOptions" />.</typeparam>
internal class ConfigurationSnsTopicResolver<TOptions> : ISnsTopicResolver
  where TOptions : ISnsTopicOptions
{
  private readonly ConcurrentDictionary<string, string> _caseInsensitiveMap = [];
  private readonly IOptionsMonitor<TOptions> _optionsMonitor;

  /// <summary>
  ///   Constructs an implementation of <see cref="ISnsTopicResolver" /> that uses application configuration
  ///   (as accessed via a type which implements <see cref="ISnsTopicOptions" />)
  ///   to resolve topic ARNs.
  /// </summary>
  /// <param name="optionsMonitor">
  ///   A monitor providing access to <typeparamref name="TOptions" /> configuration.
  ///   Use of <see cref="IOptionsMonitor{TOptions}" /> allows this type to be registered as a singleton,
  ///   as the <see cref="IOptionsMonitor{TOptions}" /> interface provides a mechanism for observing changes
  ///   to the configuration throughout the application's lifetime.
  /// </param>
  public ConfigurationSnsTopicResolver(IOptionsMonitor<TOptions> optionsMonitor)
  {
    _optionsMonitor = optionsMonitor;
  }

  /// <inheritdoc />
  public string GetTopicArn(EventStreamId stream)
  {
    var streamValue = stream.ToString();
    TOptions options = _optionsMonitor.CurrentValue;
    IReadOnlyDictionary<string, string> map = options.Map;

    return map.TryGetValue(streamValue, out string? topicArn)
      ? topicArn
      : CaseInsensitiveMatch()
        ?? options.Default
        ?? throw new SnsStreamRoutingException(streamValue, innerException: null);

    string? CaseInsensitiveMatch()
    {
      // Check to see if we've already looked up this stream name in a case-insensitive manner.
      if (_caseInsensitiveMap.TryGetValue(streamValue, out string? insensitiveCacheStreamName))
      {
        // If so, try to find a match in the map.
        if (map.TryGetValue(insensitiveCacheStreamName, out string? value))
        {
          return value;
        }
      }

      // We don't have a cached case-insensitive match, so we'll iterate and look for a case-insensitive match.
      foreach ((string key, string value) in map)
      {
        if (!key.Equals(streamValue, StringComparison.OrdinalIgnoreCase))
        {
          continue;
        }

        _caseInsensitiveMap.TryAdd(streamValue, key);

        return value;
      }

      return null;
    }
  }
}
