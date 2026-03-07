using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Dispatching;

namespace Jds.NiceNotice.Aws.Sns;

internal class SnsNoticeBatch
{
  public required List<BatchedIoResponseNotice> Notices { get; init; }
  public required string TopicArn { get; init; }

  public PublishBatchRequest ToPublishBatchRequest()
  {
    return new PublishBatchRequest
    {
      TopicArn = TopicArn,
      PublishBatchRequestEntries = Notices.Select(FromNotice).ToList()
    };
  }

  private static PublishBatchRequestEntry FromNotice(BatchedIoResponseNotice notice)
  {
    return new PublishBatchRequestEntry
    {
      Id = notice.BatchNoticeId,
      Message = notice.Notice
    };
  }
}
