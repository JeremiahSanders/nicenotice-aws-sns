namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

/// <summary>
///   An SNS message envelope from an SQS message <see cref="Amazon.SQS.Model.Message.Body" />.
/// </summary>
/// <remarks>
///   <para>
///     The SNS bridge subscription to SQS yields SQS messages containing a body which is formatted as JSON
///     by AWS infrastructure. This object conveys the shape of that JSON.
///     The <see cref="Message" /> property will contain the actual SNS message payload.
///   </para>
///   <para>This type was inferred from JSON responses. It may not be accurate or complete.</para>
/// </remarks>
public class SnsEnvelope
{
  public string? Message { get; set; }

  public Dictionary<string, MessageAttribute>? MessageAttributes { get; set; }

  public string? MessageId { get; set; }

  public string? TopicArn { get; set; }
  public string? Type { get; set; }

  public record MessageAttribute
  {
    public string? Type { get; set; }
    public string? Value { get; set; }
  }
}
