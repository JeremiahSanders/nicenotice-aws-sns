using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.TestingUtils.Randomization;
using Jds.TestingUtils.Xunit2.Extras;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class SnsNoticeIoTests
{
  public class GivenTooLargeEvents : PublishBatchArrangement
  {
    [Fact]
    public void DoesNotDispatchAnyBatches()
    {
      MockSns.CapturedBatchRequests.ShouldBeEmpty();
    }

    [Fact]
    public void ShouldReturnFailures()
    {
      ActResponse.Failures.Count.ShouldBe(Notices.Count);
    }

    [Fact]
    public void AllNoticesShouldBeReturnedAsFailures()
    {
      Notices.ShouldAllBe(expected => ActResponse.Failures.Any(failure => failure.Item1.BatchNoticeId == expected.Key));
    }

    [Fact]
    public void FailuresShouldAllBeSnsIoExceptions()
    {
      ActResponse.Failures.ShouldAllBe(failure =>
        failure.Item2.GetType() == typeof(SnsIoException) &&
        failure.Item2.Message.StartsWith("Notice is too large.")
      );
    }

    [Fact]
    public void ShouldReturnNoSuccesses()
    {
      ActResponse.Successes.ShouldBeEmpty();
    }

    protected override ExampleBaseEnterpriseEvent CreateEnterpriseEvent(int index)
    {
      // ASCII characters are 1 byte each, so if we create a string using only ASCII then
      //   we can predict the byte count of the string.
      int desiredBytes = (int)Math.Floor(SnsNoticeBatching.MaximumBytes * 1.1);
      string asciiString = Randomizer.Shared.RandomStringLatin(desiredBytes);

      return new ExampleLoginEvent
      {
        Username = asciiString
      };
    }
  }

  public class GivenExtremelyLargeEvents : PublishBatchArrangement
  {
    /// <summary>
    ///   This is a logical peer of <see cref="HaveNoMoreThan1EntryPerBatch" />,
    ///   but viewed from the batch count, rather than contents of each batch.
    /// </summary>
    [Fact]
    public void DispatchesOneBatchPerEvent()
    {
      MockSns.CapturedBatchRequests.Count().ShouldBe(Notices.Count);
    }

    /// <summary>
    ///   Verifies that since we're dispatching events within 90% of the maximum bytes,
    ///   we can only fit one event per batch.
    /// </summary>
    [Fact]
    public void HaveNoMoreThan1EntryPerBatch()
    {
      MockSns.CapturedBatchRequests.ShouldAllBe(batch => batch.PublishBatchRequestEntries.Count == 1);
    }

    [Fact]
    public void Sanity_EventsAreWithin90PercentOfMaximumBytes()
    {
      const int maximumBytes = SnsNoticeBatching.MaximumBytes;
      int requiredBytes = (int)Math.Floor(maximumBytes * 0.9);

      ActResponse.Successes.ShouldAllBe(notice => SnsNoticeBatching.GetByteCount(notice.Notice) >= requiredBytes);
    }

    [Fact]
    public void ShouldReturnNoFailures()
    {
      ActResponse.Failures.ShouldBeEmpty();
    }

    [Fact]
    public void DispatchesNoticesUsingBatchIo()
    {
      List<PublishBatchRequest> actualBatchRequests = MockSns.CapturedBatchRequests.ToList();

      int minimumBatches = Notices.Count / SnsNoticeBatching.MaxMessagesPerBatch;
      actualBatchRequests.Count.ShouldBeGreaterThanOrEqualTo(minimumBatches);
    }

    [Fact]
    public void DispatchesExpectedMessages()
    {
      List<PublishBatchRequestEntry> actualBatchRequests =
        MockSns.CapturedBatchRequests.SelectMany(batch => batch.PublishBatchRequestEntries).ToList();


      Notices.ShouldAllBe(kvp =>
        actualBatchRequests.Any(entry => entry.Id == kvp.Key && entry.Message == kvp.Value.Notice)
      );
    }

    [Fact]
    public void ShouldReturnAllSuccesses()
    {
      ActResponse.Successes.Count.ShouldBe(Notices.Count);
      Notices.ShouldAllBe(expected => ActResponse.Successes.Any(actual => actual.BatchNoticeId == expected.Key));
    }

    protected override ExampleBaseEnterpriseEvent CreateEnterpriseEvent(int index)
    {
      // ASCII characters are 1 byte each, so if we create a string using only ASCII then
      //   we can predict the byte count of the string.
      int desiredBytes = (int)Math.Floor(SnsNoticeBatching.MaximumBytes * 0.9);
      string asciiString = Randomizer.Shared.RandomStringLatin(desiredBytes);

      return new ExampleLoginEvent
      {
        Username = asciiString
      };
    }
  }

  public class GivenSingleTopic : PublishBatchArrangement
  {
    [Fact]
    public void ShouldReturnNoFailures()
    {
      ActResponse.Failures.ShouldBeEmpty();
    }

    [Fact]
    public void DispatchesNoticesUsingBatchIo()
    {
      List<PublishBatchRequest> actualBatchRequests = MockSns.CapturedBatchRequests.ToList();

      int minimumBatches = Notices.Count / SnsNoticeBatching.MaxMessagesPerBatch;
      actualBatchRequests.Count.ShouldBeGreaterThanOrEqualTo(minimumBatches);
    }

    [Fact]
    public void DispatchesExpectedMessages()
    {
      List<PublishBatchRequestEntry> actualBatchRequests =
        MockSns.CapturedBatchRequests.SelectMany(batch => batch.PublishBatchRequestEntries).ToList();


      Notices.ShouldAllBe(kvp =>
        actualBatchRequests.Any(entry => entry.Id == kvp.Key && entry.Message == kvp.Value.Notice)
      );
    }

    [Fact]
    public void ShouldReturnAllSuccesses()
    {
      ActResponse.Successes.Count.ShouldBe(Notices.Count);
      Notices.ShouldAllBe(expected => ActResponse.Successes.Any(actual => actual.BatchNoticeId == expected.Key));
    }
  }

  public class PublishBatchArrangement : BaseCaseFixture
  {
    protected PublishBatchArrangement(ISnsTopicResolver? topicResolver = null)
    {
      MockSns = new MockSns();
      DefaultTopic = Randomizer.Shared.AwsSnsArn();
      TopicResolver = topicResolver ?? TopicResolvers.Mapped([], DefaultTopic);
      Sut = new SnsNoticeIo(MockSns, TopicResolver);
      Serializer = Serializers.Json();

      Notices = new Dictionary<string, BatchedIoRequestNotice>();
      Streams = [];
    }

    protected ISnsNoticeIo Sut { get; }

    public NoticeSerializer Serializer { get; }
    public string DefaultTopic { get; }

    public ISnsTopicResolver TopicResolver { get; }

    /// <summary>
    ///   Gets a prearranged mock SNS service. By default, this is used by <see cref="SnsIo" />.
    /// </summary>
    public MockSns MockSns { get; }

    /// <summary>
    ///   Gets the SNS service used for <see cref="SnsNoticeIo" />.
    /// </summary>
    public virtual IAmazonSimpleNotificationService SnsIo => MockSns;

    public BatchIoNoticeDispatchResult ActResponse { get; private set; }

    public virtual BatchDispatchOptions? BatchDispatchOptions { get; }
    public IReadOnlyDictionary<string, BatchedIoRequestNotice> Notices { get; private set; }

    public IReadOnlyList<EventStreamId> Streams { get; set; }

    protected virtual EventStreamId StreamIdFactory(int index)
    {
      return EventStreamId.From($"stream{index}");
    }

    protected override Task ArrangeAsync()
    {
      Streams = Randomizer
        .Shared.Enumerable(StreamIdFactory, inclusiveMinCount: 3, exclusiveMaxCount: 35)
        .ToList();
      Notices = Randomizer
        .Shared.Enumerable(
          index => new BatchedIoRequestNotice(
            Streams.GetRandomItem(),
            Serializer.Serialize(
              CreateEnterpriseEvent(index)
            )
          ),
          Streams.Count,
          Streams.Count * 10
        )
        .ToDictionary(batchEvent => Guid.NewGuid().ToString(), batchEvent => batchEvent);

      return base.ArrangeAsync();
    }

    protected virtual ExampleBaseEnterpriseEvent CreateEnterpriseEvent(int index)
    {
      return new ExampleLoginEvent
      {
        Username = $"user{index}"
      };
    }

    protected override async Task ActAsync()
    {
      ActResponse = await Sut.DispatchNoticesAsync(Notices, BatchDispatchOptions, CancellationToken.None);
    }
  }
}
