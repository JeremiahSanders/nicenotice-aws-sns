using Amazon.Runtime;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.IoSanity.ImplementationDetails;

public static class AwsCredentialsHelpers
{
  public static AWSCredentials CreateTestAwsCredentials()
  {
    return new SessionAWSCredentials(
      awsAccessKeyId: "access-key-id",
      awsSecretAccessKey: "secret-access-key",
      token: "session-token",
      accountId: "000000000000"
    );
  }
}
