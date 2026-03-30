using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.NiceNotice.TypedNotices.Metadata;
using Jds.NiceNotice.TypedNotices.Serialization;
using Jds.TestingUtils.Randomization;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class SnsNoticeBatchingTests
{
  public class BatchNoticesTests
  {
    public class VerifyGrouping
    {
      [ClassDataSource<GroupingArrangement>(Shared = SharedType.PerTestSession)]
      public required GroupingArrangement Arrangement { get; init; }

      [Test]
      public void HaveExpectedTopicArns()
      {
        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.TopicArn == $"{Arrangement.BaseTopic}{snsNoticeBatch.Notices[0].Stream}"
        );
      }

      [Test]
      public void HaveNoMoreThan10EntriesPerBatch()
      {
        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Count <= 10
        );
      }

      [Test]
      public void HaveNoMoreThanMaximumBytesPerBatch()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;

        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Sum(notice => SnsNoticeBatching.GetByteCount(notice.Notice)) <= maximumBytes
        );
      }

      [Test]
      public void ReturnsNoErrors()
      {
        Arrangement.ActResult.Errors.ShouldBeEmpty();
      }
    }

    public class GroupingArrangement : RandomBatchesArrangement
    {
    }

    public class GivenExtremelyLargeEvents
    {
      [ClassDataSource<ExtremelyLargeEventsArrangement>(Shared = SharedType.PerTestSession)]
      public required ExtremelyLargeEventsArrangement Arrangement { get; init; }

      /// <summary>
      ///   Verifies that since we're dispatching events within 90% of the maximum bytes,
      ///   we can only fit one event per batch.
      /// </summary>
      [Test]
      public void HaveNoMoreThan1EntryPerBatch()
      {
        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Count <= 1
        );
      }

      [Test]
      public void HaveNoMoreThanMaximumBytesPerBatch()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;

        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Sum(notice => SnsNoticeBatching.GetByteCount(notice.Notice)) <= maximumBytes
        );
      }

      [Test]
      public void ReturnsNoErrors()
      {
        Arrangement.ActResult.Errors.ShouldBeEmpty();
      }

      /// <summary>
      ///   This is a logical peer of <see cref="HaveNoMoreThan1EntryPerBatch" />,
      ///   but viewed from the batch count, rather than contents of each batch.
      /// </summary>
      [Test]
      public void ReturnsOneBatchPerEvent()
      {
        Arrangement.ActResult.BatchedNotices.Count.ShouldBe(Arrangement.RequestNotices.Count);
      }

      [Test]
      public void Sanity_EventsAreWithin90PercentOfMaximumBytes()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;
        var requiredBytes = (int)Math.Floor(maximumBytes * 0.9);

        Arrangement.ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.All(notice => SnsNoticeBatching.GetByteCount(notice.Notice) >= requiredBytes)
        );
      }
    }

    public class ExtremelyLargeEventsArrangement : RandomBatchesArrangement
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

    public abstract class RandomBatchesArrangement : BaseCaseArrangement
    {
      public string BaseTopic { get; } = Randomizer.Shared.AwsSnsArn();
      public NoticeMetadataProvider MetadataProvider { get; } = MetadataProviders.DefaultMetadataProvider();
      public List<IoRequestNotice> Notices { get; private set; } = [];
      public Dictionary<string, IoRequestNotice> RequestNotices { get; private set; } = [];
      public NoticeSerializer Serializer { get; } = Serializers.Json();
      public List<EventStreamId> Streams { get; private set; } = [];

      internal SnsNoticeBatchingResult ActResult { get; private set; } = new()
      {
        BatchedNotices = [],
        Errors = []
      };

      internal SnsNoticeBatchingResult InvokeSystemUnderTest()
      {
        return SnsNoticeBatching.BatchNotices(TopicResolver, RequestNotices);
      }

      protected override Task ActAsync()
      {
        ActResult = InvokeSystemUnderTest();

        return base.ActAsync();
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

              return new IoRequestNotice(
                Streams.GetRandomItem(),
                serialized,
                MetadataProvider.GetMetadata(ee, serialized, Serializer.ContentType),
                Serializer.ContentType
              );
            },
            Streams.Count,
            Streams.Count * 10
          )
          .ToList();
        RequestNotices = Notices.ToDictionary(_ => Guid.NewGuid().ToString(), notice => notice);

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

      protected virtual string TopicResolver(EventStreamId streamId)
      {
        return $"{BaseTopic}{streamId}";
      }
    }
  }
}
