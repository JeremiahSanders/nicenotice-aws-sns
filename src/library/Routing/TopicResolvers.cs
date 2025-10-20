using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   Static constructors for implementations of <see cref="ISnsTopicResolver" />.
/// </summary>
public static class TopicResolvers
{
  /// <summary>
  ///   Creates a new <see cref="ISnsTopicResolver" /> that uses a mapping of event stream identifiers to SNS topic ARNs.
  ///   A default topic can be specified, which will be used if no mapping is found for a given event stream.
  ///   If no mapping is found and no default topic is specified,
  ///   an <see cref="SnsStreamRoutingException" /> exception will be thrown.
  /// </summary>
  /// <param name="map">A map of event streams to SNS topic ARNs.</param>
  /// <param name="defaultTopic">A default SNS topic ARN, which will be used if no mapping is found for a given event stream.</param>
  /// <returns>Returns a new <see cref="ISnsTopicResolver" /> instance.</returns>
  public static ISnsTopicResolver Mapped(IEnumerable<KeyValuePair<EventStreamId, string>> map, string? defaultTopic)
  {
    return MappedSnsTopicResolver.Create(map, defaultTopic);
  }

  /// <summary>
  ///   Creates a new <see cref="ISnsTopicResolver" /> which uses a configuration object to resolve SNS topic ARNs.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     The use of the <see cref="IOptionsMonitor{T}" /> interface allows the resolver to dynamically update its
  ///     operation based on changes to the application configuration,
  ///     generally from changes in <see cref="IConfiguration" />.
  ///   </para>
  /// </remarks>
  /// <param name="optionsMonitor">An options monitor for the configuration object.</param>
  /// <returns>Returns a new <see cref="ISnsTopicResolver" /> instance.</returns>
  public static ISnsTopicResolver Configuration<TSnsTopicOptions>(IOptionsMonitor<TSnsTopicOptions> optionsMonitor)
    where TSnsTopicOptions : ISnsTopicOptions
  {
    return new ConfigurationSnsTopicResolver<TSnsTopicOptions>(optionsMonitor);
  }
}
