namespace NiceNotice.Tests.ExampleWebApi.Notices;

public record UserSessionStarted : UserSessionEventNotice
{
  public required string SessionId { get; init; }
}
