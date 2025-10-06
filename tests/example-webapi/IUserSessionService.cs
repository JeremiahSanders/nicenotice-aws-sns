namespace NiceNotice.Tests.ExampleWebApi;

public interface IUserSessionService
{
  Task<GetUserSessionResult?> GetUserSessionAsync(string userId, string token);
  Task<BeginSessionResult> BeginSessionAsync(string userId, string token);
  Task<EndUserSessionResult> EndSessionAsync(string userId, string token);
}
