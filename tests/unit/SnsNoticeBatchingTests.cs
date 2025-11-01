using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.TestingUtils.Randomization;
using Jds.TestingUtils.Xunit2.Extras;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class SnsNoticeBatchingTests
{
  public class BatchNoticesTests
  {
    public class GroupingTests : RandomBatchesArrangement
    {
      [Fact]
      public void ReturnsNoErrors()
      {
        ActResult.Errors.ShouldBeEmpty();
      }

      [Fact]
      public void HaveExpectedTopicArns()
      {
        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.TopicArn == $"{BaseTopic}{snsNoticeBatch.Notices[0].Stream}"
        );
      }

      [Fact]
      public void HaveNoMoreThan10EntriesPerBatch()
      {
        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Count <= 10
        );
      }

      [Fact]
      public void HaveNoMoreThanMaximumBytesPerBatch()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;

        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Sum(notice => SnsNoticeBatching.GetByteCount(notice.Notice)) <= maximumBytes
        );
      }
    }

    public class GivenExtremelyLargeEvents : RandomBatchesArrangement
    {
      /// <summary>
      ///   Verifies that since we're dispatching events within 90% of the maximum bytes,
      ///   we can only fit one event per batch.
      /// </summary>
      [Fact]
      public void HaveNoMoreThan1EntryPerBatch()
      {
        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Count <= 1
        );
      }

      /// <summary>
      ///   This is a logical peer of <see cref="HaveNoMoreThan1EntryPerBatch" />,
      ///   but viewed from the batch count, rather than contents of each batch.
      /// </summary>
      [Fact]
      public void ReturnsOneBatchPerEvent()
      {
        ActResult.BatchedNotices.Count.ShouldBe(RequestNotices.Count);
      }

      [Fact]
      public void ReturnsNoErrors()
      {
        ActResult.Errors.ShouldBeEmpty();
      }

      [Fact]
      public void HaveNoMoreThanMaximumBytesPerBatch()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;

        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.Sum(notice => SnsNoticeBatching.GetByteCount(notice.Notice)) <= maximumBytes
        );
      }

      [Fact]
      public void Sanity_EventsAreWithin90PercentOfMaximumBytes()
      {
        const int maximumBytes = SnsNoticeBatching.MaximumBytes;
        int requiredBytes = (int)Math.Floor(maximumBytes * 0.9);

        ActResult.BatchedNotices.ShouldAllBe(snsNoticeBatch =>
          snsNoticeBatch.Notices.All(notice => SnsNoticeBatching.GetByteCount(notice.Notice) >= requiredBytes)
        );
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

    public class RandomBatchesArrangement : BaseCaseFixture
    {
      public string BaseTopic { get; } = Randomizer.Shared.AwsSnsArn();
      public List<BatchedIoRequestNotice> Notices { get; private set; } = [];
      public Dictionary<string, BatchedIoRequestNotice> RequestNotices { get; private set; } = [];
      public NoticeSerializer Serializer { get; } = Serializers.Json();
      public List<EventStreamId> Streams { get; private set; } = [];

      internal SnsNoticeBatchingResult ActResult { get; private set; } = new()
      {
        BatchedNotices = [],
        Errors = []
      };

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

      internal SnsNoticeBatchingResult InvokeSystemUnderTest()
      {
        return SnsNoticeBatching.BatchNotices(TopicResolver, RequestNotices);
      }

      protected virtual string TopicResolver(EventStreamId streamId)
      {
        return $"{BaseTopic}{streamId}";
      }

      protected override Task ActAsync()
      {
        ActResult = InvokeSystemUnderTest();

        return base.ActAsync();
      }
    }
  }
}
