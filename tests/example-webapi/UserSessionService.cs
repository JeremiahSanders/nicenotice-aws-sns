using System;
using System.Threading.Tasks;

using Jds.NiceNotice;

using Microsoft.Extensions.Logging;

using NiceNotice.Tests.ExampleWebApi.Notices;

namespace NiceNotice.Tests.ExampleWebApi;

public class UserSessionService(
  IUserSessionIo io,
  ITypedNoticeDispatcher<ExampleWebApiEventNotice> dispatcher,
  ILogger<UserSessions> logger
)
  : IUserSessionService
{
  public async Task<GetUserSessionResult?> GetUserSessionAsync(string userId, string token)
  {
    UserSession? getUserResult = await io.GetUserSessionAsync(userId);
    if (getUserResult == null)
    {
      return null;
    }

    if (!getUserResult.IsTokenValid(token))
    {
      throw new UnauthorizedAccessException();
    }

    if (!getUserResult.IsExpired)
    {
      return new GetUserSessionResult
      {
        SessionId = getUserResult.SessionId
      };
    }

    // The session is already expired, so we need to terminate it.
    await RemoveSessionAndEmitEvent(userId);

    return null;
  }

  public async Task<BeginSessionResult> BeginSessionAsync(string userId, string token)
  {
    GetUserSessionResult? sessionStatus = await GetUserSessionAsync(userId, token);

    if (sessionStatus != null)
    {
      return new BeginSessionResult
      {
        SessionId = sessionStatus.SessionId
      };
    }

    UserSession session = new()
    {
      UserId = userId,
      Token = token,
      CreatedAt = DateTime.UtcNow,
      SessionId = Guid
        .NewGuid()
        .ToString()
    };
    UserSession persistResult = await io.SetUserSessionAsync(session);

    await dispatcher.TryDispatchAsync(
      new UserSessionStarted
      {
        SessionId = persistResult.SessionId,
        UserId = persistResult.UserId
      },
      (notice, ex) => logger.LogError(
        ex,
        message: "Failed to dispatch user session started notice for user {UserId}. Session: {SessionId}",
        notice.UserId,
        notice.SessionId
      )
    );

    return new BeginSessionResult
    {
      SessionId = persistResult.SessionId
    };
  }

  public async Task<EndUserSessionResult> EndSessionAsync(string userId, string token)
  {
    GetUserSessionResult? sessionStatus = await GetUserSessionAsync(userId, token);

    if (sessionStatus == null)
    {
      throw new UnauthorizedAccessException();
    }

    UserSession? removalResult = await RemoveSessionAndEmitEvent(userId);

    if (removalResult == null)
    {
      throw new InvalidOperationException(message: "Failed to end session.");
    }

    return new EndUserSessionResult
    {
      SessionId = removalResult.SessionId,
      EndedAt = removalResult.ExpiresAt ?? DateTimeOffset.UtcNow
    };
  }

  private async Task<UserSession?> RemoveSessionAndEmitEvent(string userId)
  {
    UserSession? removedSession = await io.RemoveUserSessionAsync(userId);

    if (removedSession != null)
    {
      await dispatcher.TryDispatchAsync(
        new UserSessionEnded
        {
          SessionId = removedSession.SessionId,
          UserId = userId,
          Duration = DateTime.UtcNow - removedSession.CreatedAt
        },
        (notice, ex) => logger.LogError(
          ex,
          message:
          "Failed to dispatch user session ended notice for user {UserId}. Session: {SessionId} Duration: {Duration}",
          notice.UserId,
          notice.SessionId,
          notice.Duration
        )
      );
    }

    return removedSession;
  }
}
