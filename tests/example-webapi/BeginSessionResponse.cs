namespace NiceNotice.Tests.ExampleWebApi;

/// <summary>
///   API response for starting a session.
/// </summary>
public record BeginSessionResponse
{
  public string? SessionId { get; init; }
}
