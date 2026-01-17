namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

/// <summary>
///   An SNS message envelope from an SQS message <see cref="Amazon.SQS.Model.Message.Body" />.
/// </summary>
/// <remarks>
///   The SNS bridge subscription to SQS yields SQS messages containing a body which is formatted as JSON
///   by AWS infrastructure. This object conveys the shape of that JSON.
///   The <see cref="Message" /> property will contain the actual SNS message payload.
/// </remarks>
public class SnsEnvelope
{
  public string? Type { get; set; }

  public string? MessageId { get; set; }

  public string? TopicArn { get; set; }

  public string? Message { get; set; }

  public Dictionary<string, object>? MessageAttributes { get; set; }
}
