using System.ComponentModel.DataAnnotations;

namespace NiceNotice.Tests.ExampleConsoleApp;

public record BatchWorkflowRequest : IValidatableObject
{
  public string InputFile { get; init; } = string.Empty;
  public OutputDestination Output { get; init; } = new();

  public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
  {
    if (string.IsNullOrWhiteSpace(InputFile))
    {
      yield return new ValidationResult(
        errorMessage: "Input file is required",
        [
          nameof(InputFile)
        ]
      );
    }

    if (!Uri.IsWellFormedUriString(InputFile, UriKind.Absolute))
    {
      yield return new ValidationResult(errorMessage: "Input file must be a valid absolute URI", [nameof(InputFile)]);
    }

    if (string.IsNullOrWhiteSpace(Output.AwsS3OutputPath))
    {
      yield return new ValidationResult(
        errorMessage: "AWS S3 output path cannot be null or whitespace.",
        [nameof(Output)]
      );
    }
  }

  /// <summary>
  ///   Settings for the output destination, understood to be an Amazon Web Services S3 bucket.
  ///   In this example, the output file is written to an S3 bucket.
  /// </summary>
  public record OutputDestination
  {
    /// <summary>
    ///   Gets the Amazon Web Services authorization token to use for S3 operations.
    /// </summary>
    public string AwsAuthToken { get; init; } = string.Empty;

    /// <summary>
    ///   Gets the intended output file path, understood to be a full Amazon Web Services S3 path,
    ///   including the bucket name, prefix, and file name,
    ///   e.g., <c>s3://my-bucket/my-prefix/my-file.txt</c>.
    /// </summary>
    public string AwsS3OutputPath { get; init; } = string.Empty;
  }
}
