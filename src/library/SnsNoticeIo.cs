using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A notification dispatcher that sends messages to Amazon SNS topics.
/// </summary>
/// <remarks>
///   The <c>SnsNotificationDispatcher</c> class implements <see cref="INoticeIo" /> to dispatch
///   enterprise event notifications to designated Amazon SNS topics (identified by <see cref="EventStreamId" />).
///   It uses the AWS SDK for .NET to interact with the Simple Notification Service (SNS).
/// </remarks>
public class SnsNoticeIo(IAmazonSimpleNotificationService snsClient, ISnsTopicResolver topicResolver) : ISnsNoticeIo
{
  /// <summary>
  ///   Sends a notification message to a specified event stream using Amazon SNS.
  /// </summary>
  /// <param name="stream">The event stream to which the notification will be dispatched.</param>
  /// <param name="notice">The notification message to be sent.</param>
  /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
  /// <returns>The notification message that was dispatched.</returns>
  /// <exception cref="ArgumentNullException">
  ///   Thrown when <paramref name="notice" /> is <c>null</c>.
  /// </exception>
  /// <exception cref="IOException">Thrown when SNS throws an exception.</exception>
  public async Task<string> DispatchAsync(
    EventStreamId stream,
    string notice,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(notice);
    try
    {
      PublishResponse? publishResponse = await snsClient.PublishAsync(
        new PublishRequest
        {
          TopicArn = topicResolver.GetTopicArn(stream),
          Message = notice
        },
        cancellationToken
      );

      return notice;
    }
    catch (Exception exception)
    {
      throw new IOException($"Failed to send notification to SNS. TopicArn: {stream}", exception);
    }
  }

  /// <inheritdoc />
  public IAmazonSimpleNotificationService GetSnsIo()
  {
    return snsClient;
  }
}
