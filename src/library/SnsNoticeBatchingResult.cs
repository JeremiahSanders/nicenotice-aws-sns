namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A result of notice batching.
/// </summary>
internal class SnsNoticeBatchingResult
{
  /// <summary>
  ///   Gets the notices that were batched together, ready for dispatch to AWS SNS.
  /// </summary>
  public required List<SnsNoticeBatch> BatchedNotices { get; init; }

  /// <summary>
  ///   Gets the errors that occurred while attempting to batch the notices.
  ///   Generally this will be empty, but failures arise when messages are too large for SNS.
  /// </summary>
  public required List<(BatchedIoResponseNotice, Exception)> Errors { get; init; }
}
