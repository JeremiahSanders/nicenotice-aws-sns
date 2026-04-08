using Amazon.SimpleNotificationService;

using Jds.NiceNotice.Aws.Sns.Tests.Unit;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging.Testing;

using NiceNotice.Tests.ExampleWebApi;

using TUnit.Core.Interfaces;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.WebApiTests;

public class ExampleApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
  private readonly FakeLoggerProvider _fakeLogger;

  public ExampleApiWebApplicationFactory()
  {
    Collector = FakeLogCollector.Create(
      new FakeLogCollectorOptions()
    );
    _fakeLogger = new FakeLoggerProvider(Collector);
  }

  public FakeLogCollector Collector { get; }

  public IReadOnlyDictionary<string, string?> DefaultConfiguration { get; } = GenerateInitialConfiguration();

  public IReadOnlyDictionary<string, string?> SupplementalConfiguration { get; init; } =
    new Dictionary<string, string?>();

  public Task InitializeAsync()
  {
    _ = Server;

    return Task.CompletedTask;
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder
      .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
        configurationBuilder
          .AddInMemoryCollection(DefaultConfiguration)
          .AddJsonFile(path: "appsettings.Development.json", optional: true)
          .AddEnvironmentVariables()
          .AddInMemoryCollection(SupplementalConfiguration)
      )
      .ConfigureServices(serviceCollection =>
        {
          serviceCollection.AddSingleton<MockSns>();
          serviceCollection.AddSingleton<IAmazonSimpleNotificationService>(static serviceProvider =>
            serviceProvider.GetRequiredService<MockSns>()
          );
        }
      )
      .ConfigureLogging(logging =>
        {
          logging.ClearProviders();
          logging.AddProvider(_fakeLogger);
        }
      );
  }

  private static IReadOnlyDictionary<string, string?> GenerateInitialConfiguration()
  {
    return new Dictionary<string, string?>
    {
      {
        "sns:topics:default", "arn:aws:sns:eu-west-1:123456789012:example-topic"
      }
    };
  }
}
