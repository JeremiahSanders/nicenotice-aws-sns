using System.Text;

using Jds.NiceNotice.Dispatching;

namespace Jds.NiceNotice.Aws.Sns;

internal static class SnsNoticeBatching
{
  /// <summary>
  ///   The maximum allowed messages per SNS batch.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Source:
  ///     <a
  ///       href="https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/SNS/MSNSPublishBatchAsyncPublishBatchRequestCancellationToken.html">
  ///       AWS SDK for .NET v4 documentation
  ///     </a>
  ///   </para>
  /// </remarks>
  internal const int MaxMessagesPerBatch = 10;

  /// <summary>
  ///   The maximum allowed size in bytes of both a single SNS message and the sum of all batched messages.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     Source:
  ///     <a
  ///       href="https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/SNS/MSNSPublishBatchAsyncPublishBatchRequestCancellationToken.html">
  ///       AWS SDK for .NET v4 documentation
  ///     </a>
  ///   </para>
  /// </remarks>
  internal const int MaximumBytes = 262_144;

  internal static int GetByteCount(string message)
  {
    return Encoding.UTF8.GetByteCount(message);
  }

  /// <summary>
  ///   Groups and preprocesses notices into batches ready for dispatch to AWS SNS.
  ///   Applies logic to avoid sending notices that are too large, and to group notices
  ///   into batches which do not exceed the maximum allowed size.
  /// </summary>
  /// <param name="topicResolver">A topic resolver.</param>
  /// <param name="notices">A dictionary of notices being dispatched in batches.</param>
  /// <returns>Returns the batching result.</returns>
  public static SnsNoticeBatchingResult BatchNotices(
    Func<EventStreamId, string> topicResolver,
    IReadOnlyDictionary<string, BatchedIoRequestNotice> notices
  )
  {
    List<SnsNoticeBatch> batchedNotices = [];
    List<(BatchedIoResponseNotice, Exception)> errors = [];

    var responseNotices = notices
      .Select(kvp => new BatchedIoResponseNotice(kvp.Key, kvp.Value.Stream, kvp.Value.Notice))
      .GroupBy(notice => notice.Stream)
      .Select(streamGrouping =>
        {
          string? topic = TryGetTopic(streamGrouping.Key, topicResolver);

          return new
          {
            topic,
            eventStream = streamGrouping.Key,
            notices = streamGrouping
          };
        }
      )
      .ToList();
    foreach (var noticesWithoutTopic in responseNotices.Where(obj => obj.topic == null))
    {
      errors.AddRange(
        noticesWithoutTopic.notices.Select((BatchedIoResponseNotice, Exception) (obj) =>
          (obj, new SnsIoException($"Cannot determine AWS SNS topic ARN for event stream `{obj.Stream}`."))
        )
      );
    }

    var withTopicsAndSizes = responseNotices
      .Where(obj => obj.topic != null)
      .Select(obj =>
        {
          var noticesWithStatuses = obj
            .notices.Select(notice =>
              {
                int byteCount = GetByteCount(notice.Notice);

                return new
                {
                  notice,
                  byteCount,
                  isTooLarge = byteCount > MaximumBytes
                };
              }
            )
            .ToList();

          return new
          {
            topic = obj.topic!,
            obj.eventStream,
            notices = noticesWithStatuses
          };
        }
      )
      .ToList();


    foreach (var topicGrouping in withTopicsAndSizes)
    {
      // Handle the case where the notice is too large and just cannot be sent.
      foreach (var noticeWithTooLargeMessage in topicGrouping.notices.Where(noticeStatus => noticeStatus.isTooLarge))
      {
        errors.Add(
          (noticeWithTooLargeMessage.notice,
            new SnsIoException(
              $"Notice is too large. AWS SNS limits messages to {MaximumBytes} bytes. Size: {noticeWithTooLargeMessage.byteCount} bytes."
            ))
        );
      }

      // Now now handle the notices we might be able to batch
      List<List<BatchedIoResponseNotice>> resultGroups = [];
      var currentByteSum = 0;
      List<BatchedIoResponseNotice> currentBatch = [];

      foreach (var noticeStatus in topicGrouping.notices.Where(noticeStatus => !noticeStatus.isTooLarge))
      {
        // If adding this item would exceed either limit, finalize the current group
        if (currentBatch.Count >= MaxMessagesPerBatch || currentByteSum + noticeStatus.byteCount > MaximumBytes)
        {
          // Add the current group to the result aggregate
          resultGroups.Add(currentBatch);
          // Reset the grouping status
          currentBatch = [];
          currentByteSum = 0;
        }

        currentBatch.Add(noticeStatus.notice);
        currentByteSum += noticeStatus.byteCount;
      }

      // Add the last group if it has any items
      if (currentBatch.Count > 0)
      {
        resultGroups.Add(currentBatch);
      }

      // Now add the batches to our result aggregate
      batchedNotices.AddRange(
        resultGroups.Select(obj => new SnsNoticeBatch
          {
            Notices = obj,
            TopicArn = topicGrouping.topic
          }
        )
      );
    }


    return new SnsNoticeBatchingResult
    {
      BatchedNotices = batchedNotices,
      Errors = errors
    };
  }

  private static string? TryGetTopic(EventStreamId streamGroupingKey, Func<EventStreamId, string> topicResolver)
  {
    try
    {
      return topicResolver(streamGroupingKey);
    }
    catch (Exception _)
    {
      return null;
    }
  }
}
