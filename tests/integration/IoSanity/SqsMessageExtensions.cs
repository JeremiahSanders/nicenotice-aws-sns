using System.Text.Json;

using Amazon.SQS.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity;

public static class SqsMessageExtensions
{
  public static TBody DeserializeBodyAsSnsEnvelopeOverJson<TBody>(
    this Message message,
    JsonSerializerOptions? serializerOptions = null
  )
    where TBody : class
  {
    SnsEnvelope? snsEnvelope = null;
    try
    {
      snsEnvelope = message.ExtractSnsEnvelope() ??
                    throw new InvalidOperationException(message: "Failed to extract SNS envelope from message body.");

      return snsEnvelope.DeserializeMessageJson<TBody>(serializerOptions);
    }
    catch (Exception e)
    {
      if (snsEnvelope == null)
      {
        throw new InvalidOperationException(message: "Failed to extract SNS envelope from message body.", e);
      }

      throw new InvalidOperationException($"Failed to deserialize SNS message. SNS Message: {snsEnvelope.Message}", e);
    }
  }

  public static TTypedNotice DeserializeMessageJson<TTypedNotice>(
    this SnsEnvelope snsEnvelope,
    JsonSerializerOptions? serializerOptions = null)
  {
    return snsEnvelope.Message == null
      ? throw new InvalidOperationException(message: "SNS envelope's message is null.")
      : JsonSerializer.Deserialize<TTypedNotice>(
          snsEnvelope.Message,
          serializerOptions ?? JsonDefaults.DefaultJsonSerializerOptions
        )
        ?? throw new InvalidOperationException(message: "Failed to deserialize message body. Received null.");
  }

  public static TBody? TryDeserializeBodyAsSnsEnvelopeOverJson<TBody>(
    this Message message,
    JsonSerializerOptions? serializerOptions = null)
    where TBody : class
  {
    try
    {
      return message.DeserializeBodyAsSnsEnvelopeOverJson<TBody>(serializerOptions);
    }
    catch (Exception e)
    {
      return null;
    }
  }
}
