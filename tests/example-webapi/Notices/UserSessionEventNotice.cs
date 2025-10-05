namespace NiceNotice.Tests.ExampleWebApi.Notices;

/// <summary>
///   A base class for user session event notices.
/// </summary>
public abstract record UserSessionEventNotice : ExampleWebApiEventNotice
{
  /// <summary>
  ///   Gets the user ID associated with this notice.
  /// </summary>
  public required string UserId { get; init; }
}
