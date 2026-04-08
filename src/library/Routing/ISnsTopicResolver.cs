namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A service which resolves SNS topic ARNs for a given event stream.
///   I.e., given an event stream identifier, it returns the SNS topic ARN to which the event stream should be dispatched.
/// </summary>
public interface ISnsTopicResolver
{
  /// <summary>
  ///   Gets the SNS topic ARN for a given event stream.
  /// </summary>
  /// <param name="stream">An event stream identifier.</param>
  /// <returns>Returns the SNS topic ARN to which the event stream should be dispatched.</returns>
  string GetTopicArn(EventStreamId stream);
}
