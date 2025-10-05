using Jds.TestingUtils.Randomization;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

/// <summary>
///   Extensions to <see cref="IRandomizationSource" /> for generating AWS-related values.
/// </summary>
public static class RandomizerAwsExtensions
{
  private static readonly string[] Regions =
  [
    "eu-west-1",
    "us-east-1",
    "ap-southeast-2"
  ];

  private static readonly char[] Numbers =
  [
    '0',
    '1',
    '2',
    '3',
    '4',
    '5',
    '6',
    '7',
    '8',
    '9'
  ];

  public static string AwsAccount(this IRandomizationSource randomizationSource)
  {
    return string.Concat(
      Enumerable
        .Range(start: 0, count: 12)
        .Select(i => Numbers.GetRandomItem(randomizationSource))
    );
  }

  public static string AwsSnsArn(
    this IRandomizationSource randomizationSource,
    string? region = null,
    string? account = null,
    string? topicName = null)
  {
    region ??= Regions.GetRandomItem(randomizationSource);
    account ??= randomizationSource.AwsAccount();
    topicName ??=
      randomizationSource.RandomStringLatin(randomizationSource.IntInRange(minInclusive: 5, maxExclusive: 20));

    return $"arn:aws:sns:{region}:{account}:{topicName}";
  }
}
