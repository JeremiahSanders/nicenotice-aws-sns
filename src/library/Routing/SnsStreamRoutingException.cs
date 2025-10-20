namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   Represents an exception which is thrown when unable to route an event stream to an SNS topic.
/// </summary>
/// <param name="streamName">The event stream name. (I.e., the <see cref="EventStreamId" />)</param>
/// <param name="message">An exception message.</param>
/// <param name="innerException">Optional. An inner exception.</param>
public class SnsStreamRoutingException(string streamName, string message, Exception? innerException)
  : Exception(message, innerException)
{
  /// <summary>
  ///   Represents an exception which is thrown when unable to route an event stream to an SNS topic.
  /// </summary>
  /// <param name="streamName">The event stream name. (I.e., the <see cref="EventStreamId" />)</param>
  /// <param name="innerException">Optional. An inner exception.</param>
  public SnsStreamRoutingException(string streamName, Exception? innerException)
    : this(streamName, $"No topic defined for stream '{streamName}'.", innerException)
  {
  }

  /// <summary>
  ///   Gets the event stream which failed to be routed to an SNS topic.
  /// </summary>
  public EventStreamId EventStream { get; } = EventStreamId.From(streamName ?? string.Empty);
}
