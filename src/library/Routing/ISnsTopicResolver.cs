namespace Jds.NiceNotice.Aws.Sns;

public interface ISnsTopicResolver
{
  string GetTopicArn(EventStreamId stream);
}
