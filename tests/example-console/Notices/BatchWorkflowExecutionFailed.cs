namespace NiceNotice.Tests.ExampleConsoleApp.Notices;

public record BatchWorkflowExecutionFailed : BatchWorkerEvent
{
  public required TimeSpan Duration { get; init; }
  public required string ExceptionType { get; init; }
  public required string Message { get; init; }
  public required string OutputLocation { get; init; }
  public required string? StackTrace { get; init; }
}
