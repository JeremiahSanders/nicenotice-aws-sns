namespace NiceNotice.Tests.ExampleConsoleApp.Notices;

public record BatchExtractionComplete : BatchWorkerEvent
{
  public TimeSpan Duration { get; init; }
  public required string OutputLocation { get; init; }
}
