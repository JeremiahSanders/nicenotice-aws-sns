using Jds.NiceNotice.TypedNotices;

namespace NiceNotice.Tests.ExampleWebApi.Notices;

/// <summary>
///   A base class for user session event notices.
/// </summary>
/// <remarks>
///   This 'subdomain'/category of event notices (those which inherit from this type)
///   will be routed to the user session event stream, due to this <see cref="NoticeStreamAttribute" />,
///   which overrides the default event stream (<see cref="EventStreams.Default" />)
///   which is specified on the base type (<see cref="ExampleWebApiEventNotice" />).
/// </remarks>
[NoticeStream(EventStreams.UserSessions)]
public abstract record UserSessionEventNotice : ExampleWebApiEventNotice
{
  /// <summary>
  ///   Gets the user ID associated with this notice.
  /// </summary>
  public required string UserId { get; init; }
}
