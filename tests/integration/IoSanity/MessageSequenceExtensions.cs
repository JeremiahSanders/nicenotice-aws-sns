using Amazon.SQS.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity;

internal static class MessageSequenceExtensions
{
  /// <summary>
  ///   Filters the sequence to those which have a body containing the specified search term.
  /// </summary>
  public static IEnumerable<Message> FilterByBodyContains(this IEnumerable<Message> messages, string searchTerm)
  {
    return messages.Where(m => m.Body.Contains(searchTerm));
  }

  /// <summary>
  ///   Extracts the SnsEnvelope from each message in the sequence, where possible.
  /// </summary>
  public static IEnumerable<SnsEnvelope> FilterBySnsEnvelope(this IEnumerable<Message> messages)
  {
    return messages
      .Select(m =>
        {
          try
          {
            return m.ExtractSnsEnvelope();
          }
          catch (Exception e)
          {
            return null;
          }
        }
      )
      .OfType<SnsEnvelope>();
  }
}
