namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   An implementation of <see cref="ISnsTopicResolver" /> that uses a compile-time mapping.
/// </summary>
internal class MappedSnsTopicResolver : ISnsTopicResolver
{
  private readonly string? _defaultTopic;
  private readonly Dictionary<EventStreamId, string> _map;

  private MappedSnsTopicResolver(IEnumerable<KeyValuePair<EventStreamId, string>> map, string? defaultTopic)
  {
    _defaultTopic = defaultTopic;
    _map = map
      .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
      .DistinctBy(kvp => kvp.Key)
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

  /// <inheritdoc />
  public string GetTopicArn(EventStreamId stream)
  {
    return _map.TryGetValue(stream, out string? value)
      ? value
      : _defaultTopic ?? throw new SnsStreamRoutingException(stream, innerException: null);
  }

  /// <summary>
  ///   Static constructor for <see cref="MappedSnsTopicResolver" />.
  /// </summary>
  /// <param name="map">A map of event stream IDs to SNS topic ARNs.</param>
  /// <param name="defaultTopic">
  ///   Optional but recommended. A default SNS topic ARN to use when no mapping is found.
  ///   If not specified, an <see cref="SnsStreamRoutingException" /> will be thrown when no mapping is found.
  /// </param>
  /// <returns>Returns the constructed <see cref="MappedSnsTopicResolver" /> instance.</returns>
  public static MappedSnsTopicResolver Create(
    IEnumerable<KeyValuePair<EventStreamId, string>> map,
    string? defaultTopic)
  {
    return new MappedSnsTopicResolver(map, defaultTopic);
  }
}
