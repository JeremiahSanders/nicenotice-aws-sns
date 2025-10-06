namespace NiceNotice.Tests.ExampleWebApi;

public record UserSession
{
  public string SessionId { get; init; } = string.Empty;
  public string UserId { get; init; } = string.Empty;
  public string Token { get; init; } = string.Empty;
  public DateTime CreatedAt { get; init; }
  public DateTime? ExpiresAt { get; init; }

  public bool IsExpired => ExpiresAt != null && ExpiresAt < DateTime.UtcNow;

  public bool IsTokenValid(string token)
  {
    return Token == token;
  }
}
