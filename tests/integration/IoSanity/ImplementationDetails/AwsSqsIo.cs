using Amazon.SQS;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

public static class AwsSqsIo
{
  public static IAmazonSQS CreateSqsClient(string? serviceUrl = null)
  {
    string? url = serviceUrl ?? IntegrationTestConfiguration
      .LoadConfiguration()
      .GetTestAwsSqsServiceUrl();

    return new AmazonSQSClient(
      new AmazonSQSConfig
      {
        ServiceURL = url,
        DefaultAWSCredentials = AwsCredentialsHelpers.CreateTestAwsCredentials()
      }
    );
  }

  internal static string? GetQueueArn(this IConfiguration configuration, ConfiguredSnsTopics topic)
  {
    return topic switch
    {
      ConfiguredSnsTopics.Primary => configuration.GetQueueArn(key: "Primary"),
      ConfiguredSnsTopics.HighPriority => configuration.GetQueueArn(key: "HighPriority"),
      ConfiguredSnsTopics.Errors => configuration.GetQueueArn(key: "Errors"),
      _ => throw new ArgumentOutOfRangeException(nameof(topic), topic, message: null)
    };
  }

  internal static string? GetTestAwsSqsServiceUrl(this IConfiguration configuration)
  {
    return configuration
      .GetSection(key: "Tests:Aws:Sqs")
      .GetValue<string>(key: "ServiceUrl");
  }

  private static string? GetQueueArn(this IConfiguration configuration, string key)
  {
    return configuration
      .GetSection(key: "Tests:Queues")
      .GetValue<string>(key);
  }
}
