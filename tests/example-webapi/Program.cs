using Jds.NiceNotice;
using Jds.NiceNotice.Aws.Sns;
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
  //   We're providing a custom base notice type, ExampleWebApiEventNotice.
  //   Additionally, we're configuring NiceNotice to validate notices using .NET component model data annotations.
  .UseTypedNotices<ExampleWebApiEventNotice>(
    typedNoticeBuilder =>
      typedNoticeBuilder.ValidateWithDataAnnotations(),
    ServiceLifetime.Singleton
  )
  // Dispatch enterprise events to Amazon Web Services SNS.
  //   The `sns:topics` configuration section path is used to resolve the SNS topic ARNs.
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
