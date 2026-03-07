using Amazon.SimpleNotificationService;

using Jds.NiceNotice.Aws.Sns.Tests.Unit;

using Microsoft.Extensions.Logging.Testing;

using NiceNotice.Tests.ExampleConsoleApp;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.ConsoleAppTests;

public class ConsoleAppHarness : IDisposable
{
  private readonly FakeLoggerProvider _fakeLogger;

  public ConsoleAppHarness(IReadOnlyDictionary<string, string?>? additionalConfiguration = null)
  {
    Collector = FakeLogCollector.Create(
      new FakeLogCollectorOptions()
    );
    _fakeLogger = new FakeLoggerProvider(Collector);
    Dictionary<string, string?> config = additionalConfiguration?.ToDictionary() ?? [];

    AdditionalConfiguration = config;
    ApplicationHost = CreateHost(config, _fakeLogger);
  }

  public IReadOnlyDictionary<string, string?> AdditionalConfiguration { get; }

  public IHost ApplicationHost { get; }

  public FakeLogCollector Collector { get; }

  public static IReadOnlyDictionary<string, string?> DefaultConfiguration { get; } = CreateDefaultConfiguration();

  public void Dispose()
  {
    _fakeLogger.Dispose();
    ApplicationHost.Dispose();
  }

  public static IHost CreateHost(Dictionary<string, string?> configuration, FakeLoggerProvider fakeLogger)
  {
    IHostBuilder hostBuilder = CreateHostBuilder(configuration, fakeLogger);

    return hostBuilder.Build();
  }

  private static Dictionary<string, string?> CreateDefaultConfiguration()
  {
    return new Dictionary<string, string?>
    {
      {
        "sns:topics:default", "arn:aws:sns:us-east-1:123456789012:example-topic"
      },
      {
        "request:inputFile", "https://not-real.localhost/data"
      },
      {
        "request:output:awsS3OutputPath", "s3://fake-bucket/output-file"
      }
    };
    ;
  }

  private static IHostBuilder CreateHostBuilder(
    IEnumerable<KeyValuePair<string, string?>>? configuration,
    FakeLoggerProvider fakeLogger
  )
  {
    // Create a default host builder. This should load the application's test configuration (Development) from JSON.
    IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

    // Configure the host builder to use the application's configuration types and services.
    hostBuilder.ConfigureServices(static (hostBuilderContext, serviceCollection) =>
      {
        // Apply the normal service configuration (using code in the console application being tested).
        hostBuilderContext.ConfigureApplicationServices(serviceCollection);
        // Now add testing services.
        serviceCollection.AddSingleton<MockSns>();
        serviceCollection.AddSingleton<IAmazonSimpleNotificationService>(static serviceProvider =>
          serviceProvider.GetRequiredService<MockSns>()
        );

        serviceCollection.AddFakeLogging();
      }
    );
    hostBuilder.ConfigureLogging(logging =>
      {
        logging.ClearProviders();
        logging.AddProvider(fakeLogger);
      }
    );

    hostBuilder.ConfigureAppConfiguration((context, builder) => builder
      .AddInMemoryCollection(CreateDefaultConfiguration())
      .AddInMemoryCollection(configuration)
    );

    return hostBuilder;
  }
}
