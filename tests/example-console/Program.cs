using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NiceNotice.Tests.ExampleConsoleApp;

// Create a default host builder: supports configuration, logging, and dependency injection.
IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

// Configure the host builder to use the application's configuration types and services.
hostBuilder.ConfigureServices(static (hostBuilderContext, serviceCollection) =>
  hostBuilderContext.ConfigureApplicationServices(serviceCollection)
);

// Build the host.
IHost host = hostBuilder.Build();

// Resolve the application's batch workflow processor.
BatchWorkflow workflowProcessor = host.Services.GetService<BatchWorkflow>()
                                  ?? throw new InvalidOperationException(message: "Unable to resolve BatchWorkflow.");

// Run the batch workflow using the default options (which are loaded from configuration).
BatchWorkflowResult result = await workflowProcessor.RunAsync();

// Exit with a non-zero exit code if the batch workflow failed.
return result.IsSuccessful ? 0 : 1;
