namespace NiceNotice.Tests.ExampleConsoleApp;

public record BatchWorkflowResult
{
  public required TimeSpan Duration { get; init; }
  public Exception? FailureException { get; init; }
  public bool IsSuccessful => FailureException is null;

  public required string OutputLocation { get; init; }

  public static BatchWorkflowResult Failure(Exception failure, string intendedOutputLocation, TimeSpan duration)
  {
    return new BatchWorkflowResult
    {
      Duration = duration,
      FailureException = failure,
      OutputLocation = intendedOutputLocation
    };
  }

  public static BatchWorkflowResult Success(string outputLocation, TimeSpan duration)
  {
    return new BatchWorkflowResult
    {
      Duration = duration,
      OutputLocation = outputLocation
    };
  }
}
