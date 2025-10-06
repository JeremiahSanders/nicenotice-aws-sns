using System.Diagnostics.CodeAnalysis;

namespace NiceNotice.Tests.ExampleWebApi;

/// <summary>
///   This is a logging category marker type.
/// </summary>
/// <remarks>
///   This type is not instantiated. It is used as the generic category argument for
///   <see cref="ILogger{TCategoryName}" />.
/// </remarks>
[SuppressMessage(
  category: "ReSharper",
  checkId: "ClassNeverInstantiated.Global",
  Justification = "Logging category marker type"
)]
public class UserSessions;
