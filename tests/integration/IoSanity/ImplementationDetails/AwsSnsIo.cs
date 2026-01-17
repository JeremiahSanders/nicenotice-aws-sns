using Amazon.SimpleNotificationService;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

public static class AwsSnsIo
{
  private static string? GetMessageStreamArn(this IConfiguration configuration, string key)
  {
    return configuration
      .GetSection(key: "Tests:Topics")
      .GetValue<string>(key);
  }

  internal static string? GetMessageStreamArn(this IConfiguration configuration, ConfiguredSnsTopics topic)
  {
    return topic switch
    {
      ConfiguredSnsTopics.Primary => configuration.GetMessageStreamArn(key: "Primary"),
      ConfiguredSnsTopics.HighPriority => configuration.GetMessageStreamArn(key: "HighPriority"),
      ConfiguredSnsTopics.Errors => configuration.GetMessageStreamArn(key: "Errors"),
      _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, message: null)
    };
  }


  internal static string? GetTestAwsSnsServiceUrl(this IConfiguration configuration)
  {
    return configuration
      .GetSection(key: "Tests:Aws:Sns")
      .GetValue<string>(key: "ServiceUrl");
  }


  public static IAmazonSimpleNotificationService CreateSnsClient(string? serviceUrl = null)
  {
    string? url = serviceUrl ?? IntegrationTestConfiguration
      .LoadConfiguration()
      .GetTestAwsSnsServiceUrl();

    return new AmazonSimpleNotificationServiceClient(
      new AmazonSimpleNotificationServiceConfig
      {
        ServiceURL = url,
        DefaultAWSCredentials = AwsCredentialsHelpers.CreateTestAwsCredentials()
      }
    );
  }
}
