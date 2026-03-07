using System.Threading.Tasks;

namespace NiceNotice.Tests.ExampleWebApi;

public interface IUserSessionIo
{
  Task<UserSession?> GetUserSessionAsync(string userId);
  Task<UserSession?> RemoveUserSessionAsync(string userId);
  Task<UserSession> SetUserSessionAsync(UserSession session);
}
