namespace Jds.NiceNotice.Aws.Sns;

public class SnsTopicResolverBuilder
{
  private readonly Dictionary<EventStreamId, string> _constant = new();
  private string? _defaultTopic;

  public SnsTopicResolverBuilder Map(EventStreamId stream, string topicArn)
  {
    _constant[stream] = topicArn;

    return this;
  }

  public SnsTopicResolverBuilder WithDefaultTopic(string topicArn)
  {
    _defaultTopic = topicArn;

    return this;
  }

  public ISnsTopicResolver Build()
  {
    SnsTopicResolver resolver = SnsTopicResolver.FromMap(_constant, _defaultTopic);

    return resolver;
  }
}
