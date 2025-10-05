namespace NiceNotice.Tests.ExampleWebApi;

/// <summary>
///   API request for starting a session.
/// </summary>
public record BeginSessionRequest
{
  public string UserId { get; init; } = string.Empty;
}
