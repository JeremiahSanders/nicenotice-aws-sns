using System;

namespace NiceNotice.Tests.ExampleWebApi;

public class EndUserSessionResult
{
  public DateTimeOffset? EndedAt { get; init; }
  public string? SessionId { get; init; }
}
