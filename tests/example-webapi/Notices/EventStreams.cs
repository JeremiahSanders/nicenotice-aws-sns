using Jds.NiceNotice;

namespace NiceNotice.Tests.ExampleWebApi.Notices;

public static class EventStreams
{
  public static EventStreamId UserSessions { get; } = EventStreamId.From(value: "user-sessions");

  public static EventStreamId Default { get; } = EventStreamId.From(value: "default");
}
