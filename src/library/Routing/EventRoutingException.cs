namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   Represents an exception which is thrown when unable to route an event stream
/// </summary>
/// <param name="message"></param>
/// <param name="innerException"></param>
public class EventRoutingException(string streamName, string message, Exception? innerException)
  : Exception(message, innerException)
{
  public EventRoutingException(string streamName, Exception? innerException)
    : this(streamName, $"No topic defined for stream '{streamName}'.", innerException)
  {
  }
}
