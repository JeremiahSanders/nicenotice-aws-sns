using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NiceNotice.Tests.ExampleWebApi;

public class UserSessionIo : IUserSessionIo
{
  private readonly ConcurrentDictionary<string, UserSession> _sessions = [];

  public Task<UserSession?> GetUserSessionAsync(string userId)
  {
    return Task.FromResult(_sessions.GetValueOrDefault(userId));
  }

  public Task<UserSession> SetUserSessionAsync(UserSession session)
  {
    _sessions[session.UserId] = session;

    return Task.FromResult(session);
  }

  public Task<UserSession?> RemoveUserSessionAsync(string userId)
  {
    _sessions.Remove(userId, out UserSession? session);

    return Task.FromResult(session);
  }
}
