using System;

namespace NiceNotice.Tests.ExampleWebApi.Notices;

public record UserSessionEnded : UserSessionEventNotice
{
  public TimeSpan? Duration { get; init; }
  public required string SessionId { get; init; }
}
