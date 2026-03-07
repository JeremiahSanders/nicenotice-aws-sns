using System.ComponentModel.DataAnnotations;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;

/// <summary>
///   An example, base, typed notice (a.k.a., an enterprise event).
/// </summary>
/// <remarks>
///   <para>
///     A base enterprise event represents a required/core collection of notice properties.
///   </para>
///   <para>
///     In this example, an event <see cref="Name" /> and <see cref="Timestamp" /> are required for all
///     derived notices.
///   </para>
///   <para>
///     In other applications, a unique event identifier (possibly implemented as a <see cref="Guid" />)
///     might be necessary.
///     Inclusion of such an identifier could allow downstream processors to deduplicate events
///     or otherwise identify specific notices.
///   </para>
///   <para>
///     Implementers should consider strongly the required/shared properties on a typed notice.
///   </para>
/// </remarks>
public record ExampleBaseEnterpriseEvent
{
  /// <summary>
  ///   Gets the name of this schema/type of enterprise event.
  ///   This is not a message; interpret as an enumeration value shared by all notices of the same &quot;type&quot;.
  /// </summary>
  /// <remarks>Use <see cref="CreateEventName" />to create a name in the preferred format.</remarks>
  [Required(AllowEmptyStrings = false)]
  public string Name { get; init; } = string.Empty;

  /// <summary>
  ///   Gets the timestamp associated with this enterprise event
  ///   (in general, understood to mean &quot;when&quot; this event occurred).
  /// </summary>
  public DateTime Timestamp { get; init; } = DateTime.UtcNow;

  protected static string CreateEventName(string eventTitle, int eventSchemaRevision)
  {
    return $"{eventTitle}@{eventSchemaRevision}";
  }
}
