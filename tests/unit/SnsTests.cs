using System.Text.Json;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;
using Jds.NiceNotice.TypedNotices;
using Jds.NiceNotice.TypedNotices.Routing;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

/// <summary>
///   Tests verifying <see cref="MockSns" /> behavior and its ability to serve test needs.
/// </summary>
public class SnsTests
{
  /// <summary>
  ///   This test verifies that
  /// </summary>
  [Test]
  public async Task CanArrangeAwsIo()
  {
    var defaultTopic = "arn:aws:sns:us-east-1:123456789012:test-topic";
    IServiceCollection services = CreateServices(defaultTopic);
    ServiceProvider provider = services.BuildServiceProvider();
    var mockSns = provider.GetRequiredService<MockSns>();

    ITypedNoticeDispatcher<ExampleBaseEnterpriseEvent> dispatch =
      provider.GetRequiredService<ITypedNoticeDispatcher<ExampleBaseEnterpriseEvent>>();

    ExampleLoginEvent exampleEvent = new()
    {
      Username = "user",
      Name = "Bobby"
    };
    TypedNoticeDispatchResult<ExampleLoginEvent> result = await dispatch.DispatchAsync(exampleEvent);

    mockSns.CapturedRequests.ShouldContain(item => !string.IsNullOrWhiteSpace(item.TopicArn) &&
                                                   item.Message == JsonSerializer.Serialize(
                                                     exampleEvent,
                                                     JsonDefaults.DefaultJsonSerializerOptions
                                                   )
    );
  }

  /// <summary>
  ///   This test verifies that our <see cref="MockSns" /> captures messages as expected.
  /// </summary>
  [Test]
  public async Task Sanity_CanSendSns()
  {
    var topicArn = "arn:aws:sns:us-east-1:123456789012:test-topic";
    var message = "Hello World!";

    IServiceCollection services = CreateServices(topicArn);

    ServiceProvider provider = services.BuildServiceProvider();

    var mockSns = provider.GetRequiredService<MockSns>();
    IAmazonSimpleNotificationService fakeSns = mockSns;


    // Act
    PublishResponse? result = await fakeSns.PublishAsync(topicArn, message);

    // Assert
    result.ShouldNotBeNull();
    result.MessageId.ShouldNotBeNullOrWhiteSpace();
    mockSns.CapturedRequests.ShouldContain(item => item.TopicArn == topicArn && item.Message == message);
  }

  /// <summary>
  ///   Creates a service collection which uses AWS SNS dispatch.
  ///   AWS SNS <see cref="IAmazonSimpleNotificationService" /> is mocked using <see cref="MockSns" />,
  ///   which captures published requests.
  ///   Sends all notification streams to a single SNS topic: <paramref name="defaultTopic" />.
  /// </summary>
  /// <param name="defaultTopic"></param>
  /// <param name="constantStream"></param>
  /// <returns></returns>
  private static IServiceCollection CreateServices(string defaultTopic, string constantStream = "singleton-stream")
  {
    ServiceCollection services = new();

    // Configure AWS SNS dependencies
    services.AddSingleton<MockSns>();
    services.AddSingleton<IAmazonSimpleNotificationService>(static serviceProvider =>
      serviceProvider.GetRequiredService<MockSns>()
    );

    // Configure NiceNotice - and set it up to use AWS SNS
    services.AddNiceNotice(builder =>
      builder
        .UseTypedNotices<ExampleBaseEnterpriseEvent>(
          eeBuilder => eeBuilder.RouteToConstantStream((EventStreamId)constantStream),
          ServiceLifetime.Singleton
        )
        .DispatchToSns(
          new ConfigurationSnsTopicOptions
          {
            Default = defaultTopic
          },
          ServiceLifetime.Singleton
        )
    );

    return services;
  }
}
