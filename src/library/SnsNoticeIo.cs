using System.Collections.Concurrent;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Dispatching;

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
  /// <param name="notice">The notification which is being dispatched.</param>
  /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
  /// <returns>The notification message that was dispatched.</returns>
  /// <exception cref="ArgumentNullException">
  ///   Thrown when <paramref name="notice" /> is <c>null</c>.
  /// </exception>
  /// <exception cref="SnsIoException">Thrown when SNS throws an exception.</exception>
  public async Task<IoNoticeDispatchResult> DispatchAsync(
    IoNoticeDispatchRequest notice,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(notice);
    try
    {
      string topic = topicResolver.GetTopicArn(notice.Stream);
      PublishResponse? publishResponse = await snsClient.PublishAsync(
        new PublishRequest
        {
          TopicArn = topic,
          Message = notice.Notice,
          MessageAttributes = MessageAttributeDerivation.GetMessageAttributes(notice)
        },
        cancellationToken
      );

      if (publishResponse == null)
      {
        throw new SnsIoException(
          $"Failed to send notification to SNS. AWS returned `null`. EventStreamId: `{notice.Stream}` TopicArn: `{topic}`"
        );
      }

      if ((int)publishResponse.HttpStatusCode > 299)
      {
        throw new SnsIoException(
          $"Failed to send notification to SNS. HTTP response status: `{publishResponse.HttpStatusCode}`. EventStreamId: `{notice.Stream}` TopicArn: `{topic}`"
        );
      }

      return new IoNoticeDispatchResult(
        notice.Stream,
        notice.Notice,
        notice.Metadata,
        notice.ContentType,
        exception: null
      );
    }
    catch (Exception exception) when (exception is not SnsIoException)
    {
      return new IoNoticeDispatchResult(
        notice.Stream,
        notice.Notice,
        notice.Metadata,
        notice.ContentType,
        new SnsIoException($"Failed to send notification to SNS. EventStreamId: `{notice.Stream}`", exception)
      );
    }
  }

  /// <summary>
  ///   Uses the <see cref="ISnsTopicResolver" /> to determine the SNS topic ARN for each event stream
  ///   and dispatches the notices to the appropriate SNS topic in batches, using
  ///   <see cref="Amazon.SimpleNotificationService.IAmazonSimpleNotificationService.PublishBatchAsync" />.
  ///   Exceptions are caught and returned in the <see cref="IoBatchNoticeDispatchResult" />.
  /// </summary>
  /// <param name="request">A batch dispatch request.</param>
  /// <param name="cancellationToken">An asynchronous operation cancellation token.</param>
  /// <returns>Returns the batch dispatch result.</returns>
  public async Task<IoBatchNoticeDispatchResult> DispatchNoticesAsync(
    IoBatchNoticeDispatchRequest request,
    CancellationToken cancellationToken = new())
  {
    ConcurrentBag<IoBatchNoticeDispatchResultItem> failures = [];
    ConcurrentBag<IoBatchNoticeDispatchResultItem> successes = [];

    SnsNoticeBatchingResult batchRequests = SnsNoticeBatching.BatchNotices(topicResolver.GetTopicArn, request.Notices);
    failures.AddRange(batchRequests.Errors);

    try
    {
      await Parallel.ForEachAsync(
        batchRequests.BatchedNotices,
        new ParallelOptions
        {
          CancellationToken = cancellationToken,
          MaxDegreeOfParallelism = request.BatchDispatchOptions?.MaxDegreeOfParallelism ?? 1
        },
        async (batch, token) =>
        {
          var batchRequest = batch.ToPublishBatchRequest();
          PublishBatchResponse? awsResponse = await snsClient.PublishBatchAsync(batchRequest, token);

          if (awsResponse == null)
          {
            failures.AddRange(
              batch.Notices.Select(IoBatchNoticeDispatchResultItem (notice) => new IoBatchNoticeDispatchResultItem(
                  notice.BatchNoticeId,
                  notice.Stream,
                  notice.Notice,
                  notice.Metadata,
                  notice.ContentType,
                  new SnsIoException(message: "AWS returned `null` for batch request.")
                )
              )
            );

            return;
          }

          List<IoBatchNoticeDispatchResultItem> succeeded = awsResponse.Successful == null
            ? []
            : awsResponse
              .Successful.Select(batchResultEntry =>
                {
                  return batch.Notices.FirstOrDefault(notice => batchResultEntry.Id == notice.BatchNoticeId);
                }
              )
              .OfType<IoBatchNoticeDispatchResultItem>()
              .ToList();
          successes.AddRange(succeeded);

          List<IoBatchNoticeDispatchResultItem> failed =
            awsResponse.Failed == null
              ? []
              : awsResponse
                .Failed
                .Select(batchResultErrorEntry => new
                  {
                    batchResultErrorEntry,
                    notice = batch.Notices.FirstOrDefault(notice => batchResultErrorEntry.Id == notice.BatchNoticeId)
                  }
                )
                .Where(obj => obj.notice != null)
                .Select(IoBatchNoticeDispatchResultItem (failureObject) =>
                  new IoBatchNoticeDispatchResultItem(
                    failureObject.notice!.BatchNoticeId,
                    failureObject.notice.Stream,
                    failureObject.notice.Notice,
                    failureObject.notice.Metadata,
                    failureObject.notice.ContentType,
                    new SnsIoException(
                      $"SNS reported failure for notice `{failureObject.notice!.BatchNoticeId}`: {failureObject.batchResultErrorEntry.Message}"
                    )
                  )
                )
                .ToList();
          failures.AddRange(failed);

          IEnumerable<IoBatchNoticeDispatchResultItem> missingFromResponse =
            batch
              .Notices.Except(succeeded.Concat(failures.Select(tuple => tuple)))
              .Select(IoBatchNoticeDispatchResultItem (missingNotice) =>
                new IoBatchNoticeDispatchResultItem(
                  missingNotice.BatchNoticeId,
                  missingNotice.Stream,
                  missingNotice.Notice,
                  missingNotice.Metadata,
                  missingNotice.ContentType,
                  new SnsIoException(
                    $"SNS failed to report success/failure status for notice. HttpStatusCode: {awsResponse.HttpStatusCode}"
                  )
                )
              );
          failures.AddRange(missingFromResponse);
        }
      );
    }
    catch (TaskCanceledException taskCanceledException)
    {
      IEnumerable<IoBatchNoticeDispatchResultItem> toMarkFailed = request
        .Notices
        .Where(kvp => successes.All(success => success.BatchNoticeId != kvp.Key) &&
                      failures.All(failure => failure.BatchNoticeId != kvp.Key)
        )
        .Select(IoBatchNoticeDispatchResultItem (missingNotice) =>
          new IoBatchNoticeDispatchResultItem(
            missingNotice.Key,
            missingNotice.Value.Stream,
            missingNotice.Value.Notice,
            missingNotice.Value.Metadata,
            missingNotice.Value.ContentType,
            new SnsIoException(
              message: "SNS publishing aborted due to task cancellation. Notice not published.",
              taskCanceledException
            )
          )
        );

      failures.AddRange(toMarkFailed);
    }

    return new IoBatchNoticeDispatchResult(failures.Concat(successes));
  }

  /// <inheritdoc />
  public IAmazonSimpleNotificationService GetSnsIo()
  {
    return snsClient;
  }
}
