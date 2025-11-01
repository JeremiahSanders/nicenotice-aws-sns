using System.Collections.Concurrent;

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
  /// <exception cref="SnsIoException">Thrown when SNS throws an exception.</exception>
  public async Task<string> DispatchAsync(
    EventStreamId stream,
    string notice,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(notice);
    try
    {
      string topic = topicResolver.GetTopicArn(stream);
      PublishResponse? publishResponse = await snsClient.PublishAsync(
        new PublishRequest
        {
          TopicArn = topic,
          Message = notice
        },
        cancellationToken
      );

      if (publishResponse == null)
      {
        throw new SnsIoException(
          $"Failed to send notification to SNS. AWS returned `null`. EventStreamId: `{stream}` TopicArn: `{topic}`"
        );
      }

      if ((int)publishResponse.HttpStatusCode > 299)
      {
        throw new SnsIoException(
          $"Failed to send notification to SNS. HTTP response status: `{publishResponse.HttpStatusCode}`. EventStreamId: `{stream}` TopicArn: `{topic}`"
        );
      }

      return notice;
    }
    catch (Exception exception) when (exception is not SnsIoException)
    {
      throw new SnsIoException($"Failed to send notification to SNS. EventStreamId: `{stream}`", exception);
    }
  }

  /// <summary>
  ///   Uses the <see cref="ISnsTopicResolver" /> to determine the SNS topic ARN for each event stream
  ///   and dispatches the notices to the appropriate SNS topic in batches, using
  ///   <see cref="Amazon.SimpleNotificationService.IAmazonSimpleNotificationService.PublishBatchAsync" />.
  ///   Exceptions are caught and returned in the <see cref="BatchIoNoticeDispatchResult" />.
  /// </summary>
  /// <param name="notices">A dictionary of notices to dispatch, where keys are their identities within the batch.</param>
  /// <param name="batchDispatchOptions">Batch dispatch options. Optional.</param>
  /// <param name="cancellationToken">An asynchronous operation cancellation token.</param>
  /// <returns>Returns the batch dispatch result.</returns>
  public async Task<BatchIoNoticeDispatchResult> DispatchNoticesAsync(
    IReadOnlyDictionary<string, BatchedIoRequestNotice> notices,
    BatchDispatchOptions? batchDispatchOptions = null,
    CancellationToken cancellationToken = new())
  {
    ConcurrentBag<(BatchedIoResponseNotice, Exception)> failures = [];
    ConcurrentBag<BatchedIoResponseNotice> successes = [];

    SnsNoticeBatchingResult batchRequests = SnsNoticeBatching.BatchNotices(topicResolver.GetTopicArn, notices);
    failures.AddRange(batchRequests.Errors);

    try
    {
      await Parallel.ForEachAsync(
        batchRequests.BatchedNotices,
        new ParallelOptions
        {
          CancellationToken = cancellationToken,
          MaxDegreeOfParallelism = batchDispatchOptions?.MaxDegreeOfParallelism ?? 1
        },
        async (batch, token) =>
        {
          var batchRequest = batch.ToPublishBatchRequest();
          PublishBatchResponse? awsResponse = await snsClient.PublishBatchAsync(batchRequest, token);

          if (awsResponse == null)
          {
            failures.AddRange(
              batch.Notices.Select((BatchedIoResponseNotice, Exception) (notice) => (notice,
                new SnsIoException(message: "AWS returned `null` for batch request."))
              )
            );

            return;
          }

          List<BatchedIoResponseNotice> succeeded = awsResponse.Successful == null
            ? []
            : awsResponse
              .Successful.Select(batchResultEntry =>
                {
                  return batch.Notices.FirstOrDefault(notice => batchResultEntry.Id == notice.BatchNoticeId);
                }
              )
              .OfType<BatchedIoResponseNotice>()
              .ToList();
          successes.AddRange(succeeded);

          List<(BatchedIoResponseNotice, Exception)> failed =
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
                .Select((BatchedIoResponseNotice, Exception) (failureObject) =>
                  (
                    failureObject.notice!,
                    new SnsIoException(
                      $"SNS reported failure for notice `{failureObject.notice!.BatchNoticeId}`: {failureObject.batchResultErrorEntry.Message}"
                    )
                  )
                )
                .ToList();
          failures.AddRange(failed);

          IEnumerable<(BatchedIoResponseNotice missingNotice, Exception)> missingFromResponse =
            batch
              .Notices.Except(succeeded.Concat(failures.Select(tuple => tuple.Item1)))
              .Select((BatchedIoResponseNotice, Exception) (missingNotice) =>
                (
                  missingNotice,
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
      IEnumerable<(BatchedIoResponseNotice, Exception)> toMarkFailed = notices
        .Where(kvp => successes.All(success => success.BatchNoticeId != kvp.Key) &&
                      failures.All(failure => failure.Item1.BatchNoticeId != kvp.Key)
        )
        .Select((BatchedIoResponseNotice, Exception) (missingNotice) =>
          (
            new BatchedIoResponseNotice(missingNotice.Key, missingNotice.Value.Stream, missingNotice.Value.Notice),
            new SnsIoException(
              message: "SNS publishing aborted due to task cancellation. Notice not published.",
              taskCanceledException
            )
          )
        );

      failures.AddRange(toMarkFailed);
    }

    return new BatchIoNoticeDispatchResult
    {
      Failures = failures.ToList(),
      Successes = successes.ToList()
    };
  }

  /// <inheritdoc />
  public IAmazonSimpleNotificationService GetSnsIo()
  {
    return snsClient;
  }
}
