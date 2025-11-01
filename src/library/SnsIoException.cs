namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   An exception related to notification dispatch using AWS Simple Notification Service (SNS).
/// </summary>
public class SnsIoException : IOException
{
  /// <summary>
  ///   Constructs a new instance of the <see cref="SnsIoException" /> class.
  /// </summary>
  /// <param name="message">A message describing the exception.</param>
  public SnsIoException(string message)
    : base(message)
  {
  }

  /// <summary>
  ///   Constructs a new instance of the <see cref="SnsIoException" /> class.
  /// </summary>
  /// <param name="message">A message describing the exception.</param>
  /// <param name="innerException">An inner (generally triggering) exception.</param>
  public SnsIoException(string message, Exception? innerException)
    : base(message, innerException)
  {
  }
}
