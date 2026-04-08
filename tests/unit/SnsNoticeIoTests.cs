using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.NiceNotice.Dispatching;
using Jds.NiceNotice.TypedNotices.Metadata;
using Jds.NiceNotice.TypedNotices.Serialization;
using Jds.TestingUtils.Randomization;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class SnsNoticeIoTests
{
  public class GivenTooLargeEvents
  {
    [ClassDataSource<TooLargeEventsArrangement>(Shared = SharedType.PerTestSession)]
    public required TooLargeEventsArrangement Arrangement { get; init; }

    [Test]
    public void AllNoticesShouldBeReturnedAsFailures()
    {
      Arrangement.Notices.ShouldAllBe(expected =>
        Arrangement.ActResponse.Failures.Any(failure => failure.BatchNoticeId == expected.Key)
      );
    }

    [Test]
    public void DoesNotDispatchAnyBatches()
    {
      Arrangement.MockSns.CapturedBatchRequests.ShouldBeEmpty();
    }

    [Test]
    public void FailuresShouldAllBeSnsIoExceptions()
    {
      Arrangement.ActResponse.Failures.ShouldAllBe(failure =>
        failure.Exception!.GetType() == typeof(SnsIoException) &&
        failure.Exception.Message.StartsWith("Notice is too large.")
      );
    }

    [Test]
    public void ShouldReturnFailures()
    {
      Arrangement.ActResponse.Failures.Count().ShouldBe(Arrangement.Notices.Count);
    }

    [Test]
    public void ShouldReturnNoSuccesses()
    {
      Arrangement.ActResponse.Successes.ShouldBeEmpty();
    }
  }

  public class TooLargeEventsArrangement : PublishBatchArrangement
  {
    protected override ExampleBaseEnterpriseEvent CreateEnterpriseEvent(int index)
    {
      // ASCII characters are 1 byte each, so if we create a string using only ASCII then
      //   we can predict the byte count of the string.
      var desiredBytes = (int)Math.Floor(SnsNoticeBatching.MaximumBytes * 1.1);
      string asciiString = Randomizer.Shared.RandomStringLatin(desiredBytes);

      return new ExampleLoginEvent
      {
        Username = asciiString
      };
    }
  }

  public class GivenExtremelyLargeEvents
  {
    [ClassDataSource<ExtremelyLargeEventsArrangement>(Shared = SharedType.PerTestSession)]
    public required ExtremelyLargeEventsArrangement Arrangement { get; init; }

    [Test]
    public void DispatchesExpectedMessages()
    {
      List<PublishBatchRequestEntry> actualBatchRequests =
        Arrangement.MockSns.CapturedBatchRequests.SelectMany(batch => batch.PublishBatchRequestEntries).ToList();


      Arrangement.Notices.ShouldAllBe(kvp =>
        actualBatchRequests.Any(entry => entry.Id == kvp.Key && entry.Message == kvp.Value.Notice)
      );
    }

    [Test]
    public void DispatchesNoticesUsingBatchIo()
    {
      List<PublishBatchRequest> actualBatchRequests = Arrangement.MockSns.CapturedBatchRequests.ToList();

      int minimumBatches = Arrangement.Notices.Count / SnsNoticeBatching.MaxMessagesPerBatch;
      actualBatchRequests.Count.ShouldBeGreaterThanOrEqualTo(minimumBatches);
    }

    /// <summary>
    ///   This is a logical peer of <see cref="HaveNoMoreThan1EntryPerBatch" />,
    ///   but viewed from the batch count, rather than contents of each batch.
    /// </summary>
    [Test]
    public void DispatchesOneBatchPerEvent()
    {
      Arrangement.MockSns.CapturedBatchRequests.Count().ShouldBe(Arrangement.Notices.Count);
    }

    /// <summary>
    ///   Verifies that since we're dispatching events within 90% of the maximum bytes,
    ///   we can only fit one event per batch.
    /// </summary>
    [Test]
    public void HaveNoMoreThan1EntryPerBatch()
    {
      Arrangement.MockSns.CapturedBatchRequests.ShouldAllBe(batch => batch.PublishBatchRequestEntries.Count == 1);
    }

    [Test]
    public void Sanity_EventsAreWithin90PercentOfMaximumBytes()
    {
      const int maximumBytes = SnsNoticeBatching.MaximumBytes;
      var requiredBytes = (int)Math.Floor(maximumBytes * 0.9);

      Arrangement.ActResponse.Successes.ShouldAllBe(notice =>
        SnsNoticeBatching.GetByteCount(notice.Notice) >= requiredBytes
      );
    }

    [Test]
    public void ShouldReturnAllSuccesses()
    {
      Arrangement.ActResponse.Successes.Count().ShouldBe(Arrangement.Notices.Count);
      Arrangement.Notices.ShouldAllBe(expected =>
        Arrangement.ActResponse.Successes.Any(actual => actual.BatchNoticeId == expected.Key)
      );
    }

    [Test]
    public void ShouldReturnNoFailures()
    {
      Arrangement.ActResponse.Failures.ShouldBeEmpty();
    }
  }

  public class ExtremelyLargeEventsArrangement : PublishBatchArrangement
  {
    protected override ExampleBaseEnterpriseEvent CreateEnterpriseEvent(int index)
    {
      // ASCII characters are 1 byte each, so if we create a string using only ASCII then
      //   we can predict the byte count of the string.
      var desiredBytes = (int)Math.Floor(SnsNoticeBatching.MaximumBytes * 0.9);
      string asciiString = Randomizer.Shared.RandomStringLatin(desiredBytes);

      return new ExampleLoginEvent
      {
        Username = asciiString
      };
    }
  }

  public class GivenSingleTopic
  {
    [ClassDataSource<SingleTopicArrangement>(Shared = SharedType.PerTestSession)]
    public required SingleTopicArrangement Arrangement { get; init; }

    [Test]
    public void DispatchesExpectedMessages()
    {
      List<PublishBatchRequestEntry> actualBatchRequests =
        Arrangement.MockSns.CapturedBatchRequests.SelectMany(batch => batch.PublishBatchRequestEntries).ToList();


      Arrangement.Notices.ShouldAllBe(kvp =>
        actualBatchRequests.Any(entry => entry.Id == kvp.Key && entry.Message == kvp.Value.Notice)
      );
    }

    [Test]
    public void DispatchesNoticesUsingBatchIo()
    {
      List<PublishBatchRequest> actualBatchRequests = Arrangement.MockSns.CapturedBatchRequests.ToList();

      int minimumBatches = Arrangement.Notices.Count / SnsNoticeBatching.MaxMessagesPerBatch;
      actualBatchRequests.Count.ShouldBeGreaterThanOrEqualTo(minimumBatches);
    }

    [Test]
    public void ShouldReturnAllSuccesses()
    {
      Arrangement.ActResponse.Successes.Count().ShouldBe(Arrangement.Notices.Count);
      Arrangement.Notices.ShouldAllBe(expected =>
        Arrangement.ActResponse.Successes.Any(actual => actual.BatchNoticeId == expected.Key)
      );
    }

    [Test]
    public void ShouldReturnNoFailures()
    {
      Arrangement.ActResponse.Failures.ShouldBeEmpty();
    }
  }

  public class SingleTopicArrangement : PublishBatchArrangement
  {
  }

  public abstract class PublishBatchArrangement : BaseCaseArrangement
  {
    protected PublishBatchArrangement(ISnsTopicResolver? topicResolver = null)
    {
      MockSns = new MockSns();
      DefaultTopic = Randomizer.Shared.AwsSnsArn();
      TopicResolver = topicResolver ?? TopicResolvers.Mapped([], DefaultTopic);
      Sut = new SnsNoticeIo(MockSns, TopicResolver);
      Serializer = Serializers.Json();
      MetadataProvider = MetadataProviders.DefaultMetadataProvider();

      Notices = new Dictionary<string, IoNoticeDispatchRequest>();
      Streams = [];
      ActResponse = new IoBatchNoticeDispatchResult([]);
    }

    public IoBatchNoticeDispatchResult ActResponse { get; private set; }

    public virtual BatchDispatchOptions? BatchDispatchOptions { get; }
    public string DefaultTopic { get; }
    public NoticeMetadataProvider MetadataProvider { get; }

    /// <summary>
    ///   Gets a prearranged mock SNS service. By default, this is used by <see cref="SnsIo" />.
    /// </summary>
    public MockSns MockSns { get; }

    public IReadOnlyDictionary<string, IoNoticeDispatchRequest> Notices { get; private set; }

    public NoticeSerializer Serializer { get; }

    /// <summary>
    ///   Gets the SNS service used for <see cref="SnsNoticeIo" />.
    /// </summary>
    public virtual IAmazonSimpleNotificationService SnsIo => MockSns;

    public IReadOnlyList<EventStreamId> Streams { get; set; }

    public ISnsTopicResolver TopicResolver { get; }

    protected ISnsNoticeIo Sut { get; }

    protected override async Task ActAsync()
    {
      ActResponse = await Sut.DispatchNoticesAsync(
        new IoBatchNoticeDispatchRequest (Notices, BatchDispatchOptions),
        CancellationToken.None
      );
    }

    protected override Task ArrangeAsync()
    {
      Streams = Randomizer
        .Shared.Enumerable(StreamIdFactory, inclusiveMinCount: 3, exclusiveMaxCount: 35)
        .ToList();
      Notices = Randomizer
        .Shared.Enumerable(
          index =>
          {
            ExampleBaseEnterpriseEvent ee = CreateEnterpriseEvent(index);
            string serialized = Serializer.Serialize(ee);

            return new IoNoticeDispatchRequest(
              Streams.GetRandomItem(),
              serialized,
              MetadataProvider.GetMetadata(ee, serialized, Serializer.ContentType),
              Serializer.ContentType
            );
          },
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

    protected virtual EventStreamId StreamIdFactory(int index)
    {
      return EventStreamId.From($"stream{index}");
    }
  }
}
