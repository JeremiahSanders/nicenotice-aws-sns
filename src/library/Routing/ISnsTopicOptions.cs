namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   An interface for configuring SNS topic mappings.
/// </summary>
public interface ISnsTopicOptions
{
  /// <summary>
  ///   Gets a read-only dictionary that maps event stream identifiers to SNS topic ARNs.
  /// </summary>
  IReadOnlyDictionary<string, string> Map { get; }

  /// <summary>
  ///   Gets or sets the default SNS topic to use if no specific mapping is found for a stream.
  /// </summary>
  string? Default { get; }
}
