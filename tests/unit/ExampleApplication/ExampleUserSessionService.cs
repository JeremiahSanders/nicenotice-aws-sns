namespace Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;

/// <summary>
///   This is an example representing an internal system within an application which needs to emit enterprise events.
///   In this example, it performs the units of work related to user login and logout (abstractly;
///   this implementation doesn't do any real work).
/// </summary>
public class ExampleUserSessionService(ITypedNoticeDispatcher<ExampleBaseEnterpriseEvent> dispatcher)
{
  public async Task<LogoutResult> TryLogoutAsync(string authorizationToken)
  {
    if (await IsAuthorizationTokenValid(authorizationToken))
    {
      string username = await LogoutAsync(authorizationToken);
      await dispatcher.DispatchAsync(
        new ExampleLogoutEvent
        {
          Username = username
        }
      );

      return new LogoutResult
      {
        Username = username
      };
    }

    return new LogoutResult
    {
      ErrorMessage = "Invalid authorization token"
    };

    async Task<bool> IsAuthorizationTokenValid(string token)
    {
      // This local function mimics the interaction patterns of rudimentary async authorization validation.
      await Task.Delay(Random.Shared.Next(minValue: 1, maxValue: 50));

      return !string.IsNullOrWhiteSpace(token);
    }

    async Task<string> LogoutAsync(string token)
    {
      // This local function mimics the interaction patterns of logging out a user.
      await Task.Delay(Random.Shared.Next(minValue: 1, maxValue: 50));

      // NOTE: Since this is a mock implementation, we don't really know what the username would be. This is just a stub.
      return token;
    }
  }

  public async Task<LoginResult> TryLoginAsync(Credentials credentials)
  {
    if (await AreCredentialsValid())
    {
      // We use the notice dispatcher to emit a "login event".
      //   In a real application, that notice might be used by multiple consumers.
      //   Examples:
      //   - A dashboard metric monitor watches login events and filters by distinct username, to get active user counts.
      //   - A "report generator" application watches login events and stores the usernames and timestamps, to create login auditing reports.
      await dispatcher.DispatchAsync(
        new ExampleLoginEvent
        {
          Username = credentials.Username
        }
      );

      return new LoginResult
      {
        AuthorizationToken = await GenerateToken(credentials)
      };
    }

    return new LoginResult
    {
      ErrorMessage = "Invalid credentials"
    };

    async Task<bool> AreCredentialsValid()
    {
      // This local function mimics the interaction patterns of rudimentary async authorization validation.
      await Task.Delay(Random.Shared.Next(minValue: 1, maxValue: 50));

      return !string.IsNullOrWhiteSpace(credentials.Username) && !string.IsNullOrWhiteSpace(credentials.Password);
    }

    async Task<string> GenerateToken(Credentials _)
    {
      // This local function mimics the interaction patterns of getting an authorization token (assumed to have been persisted for later verification in a real implementation).
      await Task.Delay(Random.Shared.Next(minValue: 1, maxValue: 50));

      return Guid
        .NewGuid()
        .ToString();
    }
  }

  public record LogoutResult
  {
    public string? Username { get; init; }
    public string? ErrorMessage { get; init; }
    public bool Success => !string.IsNullOrWhiteSpace(Username);
    public bool Warning => ErrorMessage is not null;
  }

  public record LoginResult
  {
    public string? AuthorizationToken { get; init; }
    public bool Success => !string.IsNullOrWhiteSpace(AuthorizationToken);
    public bool Warning => ErrorMessage is not null;
    public string? ErrorMessage { get; init; }
  }

  public record Credentials
  {
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
  }
}
