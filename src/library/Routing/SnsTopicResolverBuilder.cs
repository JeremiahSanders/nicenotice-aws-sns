using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A builder for <see cref="ISnsTopicResolver" /> instances.
/// </summary>
public class SnsTopicResolverBuilder
{
  private readonly Dictionary<EventStreamId, string> _constant = new();
  private string? _defaultTopic;

  /// <summary>
  ///   Defines a mapping between an event stream and an SNS topic.
  /// </summary>
  /// <param name="stream">A logical event stream.</param>
  /// <param name="topicArn">
  ///   An AWS SNS topic ARN.
  ///   This will be used to populate <see cref="PublishRequest" /> <see cref="PublishRequest.TopicArn" /> values
  ///   sent to the SNS API.
  /// </param>
  /// <returns>Returns this instance for further configuration.</returns>
  public SnsTopicResolverBuilder Map(EventStreamId stream, string topicArn)
  {
    _constant[stream] = topicArn;

    return this;
  }

  /// <summary>
  ///   Adds mappings for a sequence of event streams and SNS topics.
  /// </summary>
  /// <remarks>
  ///   This overload allows use of <see cref="Dictionary{TKey,TValue}" /> to define mappings,
  ///   rather than repeatedly invoking <see cref="Map(Jds.NiceNotice.EventStreamId,string)" />.
  /// </remarks>
  /// <param name="map">An event stream to SNS topic ARN mapping.</param>
  /// <returns>Returns this instance for further configuration.</returns>
  public SnsTopicResolverBuilder Map(IEnumerable<KeyValuePair<EventStreamId, string>> map)
  {
    foreach (KeyValuePair<EventStreamId, string> kvp in map)
    {
      Map(kvp.Key, kvp.Value);
    }

    return this;
  }

  /// <summary>
  ///   Defines a default topic to use when no mapping is found for a given event stream.
  /// </summary>
  /// <param name="topicArn">A default AWS SNS topic ARN.</param>
  /// <returns>Returns this instance for further configuration.</returns>
  public SnsTopicResolverBuilder WithDefaultTopic(string topicArn)
  {
    _defaultTopic = topicArn;

    return this;
  }

  /// <summary>
  ///   Builds an <see cref="ISnsTopicResolver" /> instance which uses the logic defined by this builder.
  /// </summary>
  /// <returns>Returns the <see cref="ISnsTopicResolver" />.</returns>
  public ISnsTopicResolver Build()
  {
    SnsTopicResolver resolver = SnsTopicResolver.FromMap(_constant, _defaultTopic);

    return resolver;
  }
}
