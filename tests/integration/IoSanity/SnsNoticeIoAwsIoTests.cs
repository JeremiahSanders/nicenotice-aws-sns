using Amazon.SQS.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;
using Jds.NiceNotice.Dispatching;
using Jds.NiceNotice.TypedNotices;
using Jds.NiceNotice.TypedNotices.Routing;
using Jds.TestingUtils.Randomization;

using NiceNotice.Tests.ExampleWebApi.Notices;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity;

/// <summary>
///   Tests verifying the <see cref="SnsNoticeIo" /> class functionality
///   when using AWS services (as compared to in-process mocks).
/// </summary>
/// <remarks>
///   Tests are expected to execute against LocalStack, providing a strong SNS mock.
///   SNS messages cannot be verified directly. Instead, we have to have an SQS queue subscribed to the topic
///   and verify that the queue receives the expected message.
/// </remarks>
[ClassDataSource<AwsTestHarness>(Shared = SharedType.PerTestSession)]
public class SnsNoticeIoAwsIoTests(AwsTestHarness awsTestHarness)
{
  [Test]
  public async Task CanDispatchASingleNotice()
  {
    ISnsNoticeIo snsNoticeIo = awsTestHarness.GetSnsNoticeIo();

    // Act
    const string soloNoticeText = "Test notice raw text";
    string rawTextResponse = await snsNoticeIo.DispatchAsync(
      ConfiguredEventStreams.HighPriority,
      soloNoticeText
    );

    // Obtain verification values
    List<Message> queueMessages =
      (await awsTestHarness.GetMessagesInQueueAsync(
        awsTestHarness.GetQueueArn(ConfiguredSnsTopics.HighPriority) ?? string.Empty
      )).ToList();

    // Assert
    await Assert
      .That((IEnumerable<Message>)queueMessages)
      .Contains(message => message.ExtractSnsMessage() == soloNoticeText);
  }

  [Test]
  public async Task CanDispatchBatchedNotices()
  {
    ISnsNoticeIo snsNoticeIo = awsTestHarness.GetSnsNoticeIo();

    // Act
    const string batchNoticeText = "Test notice text in a batch";
    BatchIoNoticeDispatchResult batchResponse = await snsNoticeIo.DispatchNoticesAsync(
      new Dictionary<string, BatchedIoRequestNotice>
      {
        {
          "message-1", new BatchedIoRequestNotice(ConfiguredEventStreams.Errors, batchNoticeText)
        }
      }
    );

    // Obtain verification values
    List<Message> queueMessages =
      (await awsTestHarness.GetMessagesInQueueAsync(
        awsTestHarness.GetQueueArn(ConfiguredSnsTopics.Errors) ?? string.Empty
      )).ToList();

    // Assert
    await Assert
      .That((IEnumerable<Message>)queueMessages)
      .Contains(message => message.ExtractSnsMessage() == batchNoticeText);
  }

  [Test]
  public async Task CanDispatchUsableSerializedSqsMessages()
  {
    string topic = awsTestHarness.GetMessageStreamArn(ConfiguredSnsTopics.Primary) ?? string.Empty;
    string queue = awsTestHarness.GetQueueArn(ConfiguredSnsTopics.Primary) ?? string.Empty;

    var topicOptions = new ConfigurationSnsTopicOptions
    {
      Default = topic
    };

    ServiceProvider services = new ServiceCollection()
      .AddSingleton(serviceProvider => awsTestHarness.CreateSnsClient())
      .AddNiceNotice(nnBuilder =>
        nnBuilder
          .UseTypedNotices<EnterpriseEvent>(
            tnBuilder => tnBuilder.RouteToConstantStream(EventStreamId.From(value: "primary")),
            ServiceLifetime.Singleton
          )
          .DispatchToSns(topicOptions, ServiceLifetime.Singleton)
      )
      .BuildServiceProvider();

    ITypedNoticeDispatcher<EnterpriseEvent> dispatcher =
      services.GetRequiredService<ITypedNoticeDispatcher<EnterpriseEvent>>();

    // Act
    UserSessionStarted soloEvent = new()
    {
      SessionId = Randomizer.Shared.HashSha256(),
      UserId = Randomizer.Shared.RandomStringLatin(length: 32)
    };
    UserSessionEnded batchEvent1 = new()
    {
      SessionId = Randomizer.Shared.HashSha256(),
      UserId = Randomizer.Shared.RandomStringLatin(length: 32)
    };
    UserSessionEnded batchEvent2 = new()
    {
      SessionId = Randomizer.Shared.HashSha256(),
      UserId = Randomizer.Shared.RandomStringLatin(length: 32)
    };

    TypedNoticeDispatchResult<UserSessionStarted> soloDispatchResult = await dispatcher.DispatchAsync(soloEvent);
    BatchTypedNoticeDispatchResult batchDispatchResult =
      await dispatcher.DispatchBatchAsync(
        DispatchBatchRequest<EnterpriseEvent>.CreateFromTypedNotices([batchEvent1, batchEvent2])
      );

    // Obtain verification values
    List<Message> queueMessages = (await awsTestHarness.GetMessagesInQueueAsync(queue)).ToList();

    Message? soloMessageInQueue = queueMessages.FilterByBodyContains(soloEvent.Id.ToString()).SingleOrDefault();
    var soloMessageDeserialized = soloMessageInQueue?.DeserializeBodyAsSnsEnvelopeOverJson<UserSessionStarted>();
    Message? batch1InQueue = queueMessages.FilterByBodyContains(batchEvent1.Id.ToString()).SingleOrDefault();
    Message? batch2InQueue = queueMessages.FilterByBodyContains(batchEvent2.Id.ToString()).SingleOrDefault();
    var batch1Deserialized = batch1InQueue?.DeserializeBodyAsSnsEnvelopeOverJson<UserSessionEnded>();
    var batch2Deserialized = batch2InQueue?.DeserializeBodyAsSnsEnvelopeOverJson<UserSessionEnded>();

    // Assert
    soloMessageDeserialized.ShouldBeEquivalentTo(soloEvent);
    batch1Deserialized.ShouldBeEquivalentTo(batchEvent1);
    batch2Deserialized.ShouldBeEquivalentTo(batchEvent2);
  }

  public static class ConfiguredEventStreams
  {
    public static readonly EventStreamId Primary = EventStreamId.From(value: "Primary");
    public static readonly EventStreamId HighPriority = EventStreamId.From(value: "HighPriority");
    public static readonly EventStreamId Errors = EventStreamId.From(value: "Errors");
  }
}
