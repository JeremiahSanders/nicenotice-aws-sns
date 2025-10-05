namespace Jds.NiceNotice.Aws.Sns.Tests.Unit.ExampleApplication;

public record ExampleLogoutEvent : ExampleBaseEnterpriseEvent
{
  public ExampleLogoutEvent()
  {
    Name = CreateEventName(eventTitle: "Logout", eventSchemaRevision: 0);
  }

  public required string Username { get; init; }
}
