using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.TestingUtils.Randomization;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class SnsTopicResolverTests
{
  [Fact]
  public void UsingBuilder_DirectMapping_BuildsExpectedResolver()
  {
    EventStreamId loginStreamId = (EventStreamId)nameof(ExampleLoginEvent);
    EventStreamId logoutStreamId = (EventStreamId)nameof(ExampleLogoutEvent);
    string defaultTopic = Randomizer.Shared.AwsSnsArn();
    string loginTopic = Randomizer.Shared.AwsSnsArn();

    ISnsTopicResolver resolver = new SnsTopicResolverBuilder()
      .Map(loginStreamId, loginTopic) // Map the login stream; don't map logout
      .WithDefaultTopic(defaultTopic)
      .Build();


    resolver.GetTopicArn(loginStreamId).ShouldBe(loginTopic);
    resolver.GetTopicArn(logoutStreamId).ShouldBe(defaultTopic);
  }

  [Fact]
  public void UsingBuilder_DictionaryMapping_BuildsExpectedResolver()
  {
    EventStreamId loginStreamId = (EventStreamId)nameof(ExampleLoginEvent);
    EventStreamId logoutStreamId = (EventStreamId)nameof(ExampleLogoutEvent);
    string defaultTopic = Randomizer.Shared.AwsSnsArn();
    string loginTopic = Randomizer.Shared.AwsSnsArn();

    ISnsTopicResolver resolver = new SnsTopicResolverBuilder()
      .Map(
        new Dictionary<EventStreamId, string>
        {
          // Map the login stream; don't map logout
          {
            loginStreamId, loginTopic
          }
        }
      )
      .WithDefaultTopic(defaultTopic)
      .Build();


    resolver.GetTopicArn(loginStreamId).ShouldBe(loginTopic);
    resolver.GetTopicArn(logoutStreamId).ShouldBe(defaultTopic);
  }
}
