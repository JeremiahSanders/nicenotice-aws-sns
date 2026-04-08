namespace NiceNotice.Tests.ExampleWebApi.Notices;

/// <summary>
///   Constants for the defined, logical event streams in this application.
/// </summary>
/// <remarks>
///   <para>
///     This example application configures two streams:
///     a default (for most events) and a second stream for user session events.
///     Microservices and similarly narrowly scoped applications may prefer to use a single stream for all events.
///   </para>
///   <para>
///     However, applications which offer multiple REST endpoints
///     or which have multiple subdomains or categories of events may benefit from using multiple streams.
///   </para>
///   <para>Remember: Stream identities support logical routing; they are not direct representations of I/O streams.</para>
/// </remarks>
public static class EventStreams
{
  /// <summary>
  ///   Default stream for events that do not belong to any other stream.
  /// </summary>
  public const string Default = "default";

  /// <summary>
  ///   User sessions stream. (E.g., sign in, sign out)
  /// </summary>
  public const string UserSessions = "user-sessions";
}
