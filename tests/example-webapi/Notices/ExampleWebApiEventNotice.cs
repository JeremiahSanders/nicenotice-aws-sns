using Jds.NiceNotice;
using Jds.NiceNotice.TypedNotices;

namespace NiceNotice.Tests.ExampleWebApi.Notices;

/// <summary>
///   A base class for this web API's event notices.
/// </summary>
/// <remarks>
///   Because we're specifying a <see cref="NoticeStreamAttribute" /> here, the base notice type,
///   all notices derived from this type will be routed to the default event stream (<see cref="EventStreams.Default" />),
///   unless they override it with their own <see cref="NoticeStreamAttribute" />.
/// </remarks>
[NoticeStream(EventStreams.Default)]
public abstract record ExampleWebApiEventNotice : EnterpriseEvent;
