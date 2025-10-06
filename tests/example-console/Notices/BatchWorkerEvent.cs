using Jds.NiceNotice;

namespace NiceNotice.Tests.ExampleConsoleApp.Notices;

/// <summary>
///   A base type for this batch worker console app's events.
/// </summary>
/// <remarks>
///   <para>
///     In a real application, you can use these base types to enforce a set of schema properties.
///     Additionally, when resolving a notice dispatcher in services, you can use the generic
///     <see cref="ITypedNoticeDispatcher{TEnterpriseEventBaseType}" /> with your custom type
///     to easily enforce those common requirements and expectations.
///   </para>
///   <para>
///     See <see cref="BatchWorkflow" /> for an example of how this is enforced.
///   </para>
/// </remarks>
public abstract record BatchWorkerEvent : EnterpriseEvent;
