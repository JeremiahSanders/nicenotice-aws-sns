namespace NiceNotice.Tests.ExampleConsoleApp.Notices;

public record ExtractedImportantInformation : BatchWorkerEvent
{
  public required string DataValueId { get; init; }
}
