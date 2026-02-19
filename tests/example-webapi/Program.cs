using Jds.NiceNotice;
using Jds.NiceNotice.Aws.Sns;
using Jds.NiceNotice.TypedNotices.Routing;
using Jds.NiceNotice.TypedNotices.Validation;

using NiceNotice.Tests.ExampleWebApi;
using NiceNotice.Tests.ExampleWebApi.Notices;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder
  .Services
  .AddSingleton<IUserSessionIo, UserSessionIo>()
  .AddTransient<IUserSessionService, UserSessionService>();


/*****
 *   Add Nice Notice enterprise event infrastructure.
 *****/
builder.Services.AddNiceNotice(niceNoticeBuilder => niceNoticeBuilder
  // Enable typed enterprise events.
  .UseTypedNotices<ExampleWebApiEventNotice>(
    typedNoticeBuilder =>
      /* Configure two streams: a default (for most events) and a second stream for user session events.
       *
       *   Microservices and similarly narrowly scoped applications may prefer to use a single stream for all events.
       *   However, applications which offer multiple REST endpoints
       *     or which have multiple subdomains or categories of events may benefit from using multiple streams.
       *
       *   Remember: Stream identities support logical routing; they are not direct representations of I/O streams.
       */
      typedNoticeBuilder
        .UseStreamSelector(
          Routers.TypeMap<ExampleWebApiEventNotice>(
            defaultStream: EventStreams.Default,
            map: new Dictionary<Type, EventStreamId>
            {
              {
                typeof(UserSessionEnded), EventStreams.UserSessions
              },
              {
                typeof(UserSessionStarted), EventStreams.UserSessions
              }
            }
          )
        )
        .ValidateWithDataAnnotations(),
    ServiceLifetime.Singleton
  )
  // Dispatch enterprise events to Amazon Web Services SNS.
  .DispatchToSns(
    configurationSectionPath: "sns:topics",
    ServiceLifetime.Singleton
  )
);


// Build the application.
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map the user session HTTP APIs.
UserSessionEndpoints.Map(app);

app.Run();

namespace NiceNotice.Tests.ExampleWebApi
{
  /// <summary>
  ///   Entry point for the application.
  /// </summary>
  /// <remarks>
  ///   This type declaration is required to access this type from integration tests.
  ///   See
  ///   <a
  ///     href="https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0&pivots=xunit#basic-tests-with-the-default-webapplicationfactory">
  ///     ASP.NET Core integration test guidance.
  ///   </a>
  /// </remarks>
  public class Program
  {
  }
}
