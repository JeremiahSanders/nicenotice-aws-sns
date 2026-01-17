using System.Collections.Concurrent;

using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

using TUnit.Core.Interfaces;

using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity;

/// <summary>
///   A test harness for testing AWS SNS and SQS integration.
/// </summary>
/// <remarks>
///   Uses concurrent data structures to store messages for testing purposes.
///   This testing dependency instance should be shared across the whole test session.
/// </remarks>
public class AwsTestHarness : IAsyncInitializer
{
  private readonly Lazy<IConfiguration> _configuration = new(IntegrationTestConfiguration.LoadConfiguration);
  private readonly ConcurrentDictionary<string, ConcurrentBag<Message>> _sqsMessages = new();

  public async Task InitializeAsync()
  {
  }

  public static ISnsTopicResolver CreateTopicResolver(IConfiguration configuration)
  {
    return TopicResolvers.Mapped(
      new Dictionary<EventStreamId, string>
      {
        {
          SnsNoticeIoAwsIoTests.ConfiguredEventStreams.Primary,
          configuration.GetMessageStreamArn(ConfiguredSnsTopics.Primary) ?? string.Empty
        },
        {
          SnsNoticeIoAwsIoTests.ConfiguredEventStreams.HighPriority,
          configuration.GetMessageStreamArn(ConfiguredSnsTopics.HighPriority) ?? string.Empty
        },
        {
          SnsNoticeIoAwsIoTests.ConfiguredEventStreams.Errors,
          configuration.GetMessageStreamArn(ConfiguredSnsTopics.Errors) ?? string.Empty
        }
      },
      configuration.GetMessageStreamArn(ConfiguredSnsTopics.Primary)
    );
  }

  /// <summary>
  ///   Retrieves all messages from the specified queue.
  /// </summary>
  public async Task<IEnumerable<Message>> GetMessagesInQueueAsync(string queueUrl)
  {
    await ExtractMessagesInQueueAsync(queueUrl);

    return _sqsMessages.TryGetValue(queueUrl, out ConcurrentBag<Message>? messages)
      ? messages.AsEnumerable()
      : [];
  }

  private async Task<IEnumerable<Message>> ExtractMessagesInQueueAsync(string queueUrl)
  {
    List<Message> messages = await GetSqsClient().ReceiveAllMessagesAsync(queueUrl);
    _sqsMessages.AddOrUpdate(
      queueUrl,
      _ => new ConcurrentBag<Message>(messages),
      (_, bag) =>
      {
        bag.AddRange(messages);

        return bag;
      }
    );

    return messages;
  }

  private IConfiguration GetConfiguration()
  {
    return _configuration.Value;
  }

  public ISnsTopicResolver CreateTopicResolver()
  {
    return CreateTopicResolver(GetConfiguration());
  }

  public IAmazonSQS GetSqsClient()
  {
    return AwsSqsIo.CreateSqsClient(GetConfiguration().GetTestAwsSqsServiceUrl());
  }

  public IAmazonSimpleNotificationService CreateSnsClient()
  {
    return AwsSnsIo.CreateSnsClient(GetConfiguration().GetTestAwsSnsServiceUrl());
  }

  public ISnsNoticeIo GetSnsNoticeIo()
  {
    return new SnsNoticeIo(CreateSnsClient(), CreateTopicResolver());
  }

  public IAmazonSQS CreateSqsClient()
  {
    return AwsSqsIo.CreateSqsClient(GetConfiguration().GetTestAwsSqsServiceUrl());
  }

  public async Task PurgeQueueAsync(string queueArn)
  {
    // "Purge", but still capture
    await ExtractMessagesInQueueAsync(queueArn);
  }

  public string? GetMessageStreamArn(ConfiguredSnsTopics topic)
  {
    return GetConfiguration().GetMessageStreamArn(topic);
  }

  public string? GetQueueArn(ConfiguredSnsTopics topicWhichQueueFollows)
  {
    return GetConfiguration().GetQueueArn(topicWhichQueueFollows);
  }
}
