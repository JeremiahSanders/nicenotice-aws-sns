using Amazon.SimpleNotificationService;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A notification dispatcher that sends messages to Amazon SNS topics.
/// </summary>
public interface ISnsNoticeIo : INoticeIo
{
  /// <summary>
  ///   Gets the AWS SNS client used for this I/O implementation.
  /// </summary>
  IAmazonSimpleNotificationService GetSnsIo();
}
