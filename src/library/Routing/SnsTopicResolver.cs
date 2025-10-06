namespace Jds.NiceNotice.Aws.Sns;

internal class SnsTopicResolver : ISnsTopicResolver
{
  private readonly string? _defaultTopic;
  private readonly Dictionary<EventStreamId, string> _map;

  private SnsTopicResolver(IEnumerable<KeyValuePair<EventStreamId, string>> map, string? defaultTopic)
  {
    _defaultTopic = defaultTopic;
    _map = map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

  public string GetTopicArn(EventStreamId stream)
  {
    return _map.TryGetValue(stream, out string? value)
      ? value
      : _defaultTopic ?? throw new EventRoutingException(stream, innerException: null);
  }

  public static SnsTopicResolver FromMap(IEnumerable<KeyValuePair<EventStreamId, string>> map, string? defaultTopic)
  {
    return new SnsTopicResolver(map, defaultTopic);
  }
}
