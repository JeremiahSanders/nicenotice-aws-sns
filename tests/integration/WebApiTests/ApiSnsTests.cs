using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Jds.NiceNotice.Aws.Sns.Tests.Unit;
using Jds.TestingUtils.Randomization;

using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.WebApiTests;

/// <summary>
///   Tests verifying that the example web API can send messages to SNS
///   (as arranged by <see cref="ExampleApiWebApplicationFactory" />).
/// </summary>
public class ApiSnsTests
{
  [ClassDataSource<ExampleApiWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required ExampleApiWebApplicationFactory ExampleApiWebApplicationFactory { get; init; }

  [Test]
  public async Task Sanity_SnsIsConfigured()
  {
    IAmazonSimpleNotificationService service =
      ExampleApiWebApplicationFactory.Services.GetRequiredService<IAmazonSimpleNotificationService>();

    await Assert
      .That(service)
      .IsNotNull();
    await Assert
      .That(service)
      .IsAssignableTo<MockSns>();
  }

  [Test]
  public async Task Sanity_TopicsAreConfigured()
  {
    IOptions<ConfigurationSnsTopicOptions> options = ExampleApiWebApplicationFactory.Services
      .GetRequiredService<IOptions<ConfigurationSnsTopicOptions>>();
    await Assert
      .That(options.Value.Default)
      .IsNotNullOrWhitespace();
    await Assert
      .That(options.Value.Streams)
      .IsNotEmpty();
  }

  [Test]
  public async Task CanPublishToTopic()
  {
    using IServiceScope dependencyScope = ExampleApiWebApplicationFactory.Services.CreateScope();
    MockSns mockSns = dependencyScope.ServiceProvider.GetRequiredService<MockSns>();
    IAmazonSimpleNotificationService asSns = mockSns;

    PublishRequest publishRequest = new()
    {
      Message = Guid.NewGuid().ToString(),
      TopicArn = Randomizer.Shared.AwsSnsArn()
    };

    PublishResponse? _ = await asSns.PublishAsync(publishRequest);

    await Assert
      .That(mockSns.CapturedRequests)
      .Contains(captured => captured.TopicArn == publishRequest.TopicArn && captured.Message == publishRequest.Message);
  }
}
