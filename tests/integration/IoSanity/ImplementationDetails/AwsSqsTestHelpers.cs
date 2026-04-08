using System.Text.Json;

using Amazon.SQS;
using Amazon.SQS.Model;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

public static class AwsSqsTestHelpers
{
  public static SnsEnvelope? ExtractSnsEnvelope(this Message sqsMessage)
  {
    return JsonSerializer.Deserialize<SnsEnvelope>(sqsMessage.Body);
  }

  public static string? ExtractSnsMessage(this Message sqsMessage)
  {
    return sqsMessage.ExtractSnsEnvelope()?.Message;
  }

  public static T? ParseSnsMessage<T>(this Message sqsMessage) where T : class
  {
    string? message = sqsMessage.ExtractSnsMessage();

    return message == null ? null : JsonSerializer.Deserialize<T>(message);
  }

  /// <summary>
  ///   Empties the queue using the Purge operation.
  ///   Note: AWS limits purge operations to once every 60 seconds per queue.
  /// </summary>
  public static async Task<PurgeQueueResponse> PurgeQueueAsync(this IAmazonSQS sqsClient, string queueUrl)
  {
    return await sqsClient.PurgeQueueAsync(
      new PurgeQueueRequest
      {
        QueueUrl = queueUrl
      }
    );
  }


  /// <summary>
  ///   Pulls all available messages from the queue until it is empty.
  ///   This is useful for clearing a queue in tests without hitting the Purge 60-second limit.
  /// </summary>
  public static async Task<List<Message>> ReceiveAllMessagesAsync(this IAmazonSQS sqsClient, string queueUrl)
  {
    List<Message> allMessages = new();
    var hasMoreMessages = true;

    while (hasMoreMessages)
    {
      ReceiveMessageResponse? receiveResponse = await sqsClient.ReceiveMessageAsync(
        new ReceiveMessageRequest
        {
          QueueUrl = queueUrl,
          MaxNumberOfMessages = 10,
          WaitTimeSeconds = 1 // Short wait to ensure we've drained current flight
        }
      );

      if (receiveResponse.Messages?.Count > 0)
      {
        allMessages.AddRange(receiveResponse.Messages);

        // Optional: Delete messages so they don't reappear
        List<DeleteMessageBatchRequestEntry> deleteEntries = receiveResponse
          .Messages
          .Select(m => new DeleteMessageBatchRequestEntry(m.MessageId, m.ReceiptHandle))
          .ToList();

        await sqsClient.DeleteMessageBatchAsync(queueUrl, deleteEntries);
      }
      else
      {
        hasMoreMessages = false;
      }
    }

    return allMessages;
  }
}
