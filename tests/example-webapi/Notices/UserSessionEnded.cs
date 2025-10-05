namespace NiceNotice.Tests.ExampleWebApi.Notices;

public record UserSessionEnded : UserSessionEventNotice
{
  public required string SessionId { get; init; }
  public TimeSpan? Duration { get; init; }
}
