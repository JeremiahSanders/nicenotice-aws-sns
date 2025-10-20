using Jds.NiceNotice;
using Jds.NiceNotice.Aws.Sns;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using NiceNotice.Tests.ExampleConsoleApp.Notices;

namespace NiceNotice.Tests.ExampleConsoleApp;

public static class Services
{
  /// <summary>
  ///   Configures application-level services for the host.
  /// </summary>
  /// <remarks>
  ///   This method performs the following operations:
  ///   <list type="bullet">
  ///     <item>
  ///       Loads a <see cref="BatchWorkflowRequest" /> from the application configuration and binds it as an
  ///       <see cref="IOptions{TOptions}" /> service.
  ///     </item>
  ///     <item>
  ///       Registers the application-specific <see cref="BatchWorkflow" /> as a singleton service for batch processing.
  ///     </item>
  ///     <item>
  ///       Configures and integrates the Nice Notice enterprise event infrastructure to enable event processing.
  ///     </item>
  ///   </list>
  ///   The Nice Notice integration includes:
  ///   <list type="bullet">
  ///     <item>
  ///       Using typed enterprise events of type <see cref="BatchWorkerEvent" />.
  ///     </item>
  ///     <item>
  ///       Routing all events to a single logical event stream named <c>batch-worker-events</c>.
  ///     </item>
  ///     <item>Configuring event dispatch to AWS SNS with a topic ARN obtained from the application's configuration.</item>
  ///   </list>
  /// </remarks>
  /// <param name="hostBuilderContext">
  ///   Provides access to the host's configuration and environment.
  /// </param>
  /// <param name="serviceCollection">
  ///   The collection of services to which application services are added.
  /// </param>
  /// <param name="requestConfigurationSectionPath">Configuration section path for the batch workflow request.</param>
  /// <param name="snsTopicsConfigurationSectionPath">Configuration section path for the Nice Notice SNS topics.</param>
  /// <exception cref="InvalidOperationException">
  ///   Thrown if the required SNS topic ARN is not found in the application configuration.
  /// </exception>
  public static HostBuilderContext ConfigureApplicationServices(
    this HostBuilderContext hostBuilderContext,
    IServiceCollection serviceCollection,
    string requestConfigurationSectionPath = "request",
    string snsTopicsConfigurationSectionPath = "sns:topics"
  )
  {
    // Load the batch workflow request directly from the configuration
    serviceCollection
      .AddOptions<BatchWorkflowRequest>()
      .BindConfiguration(requestConfigurationSectionPath);

    // Add the batch workflow processor.
    serviceCollection.AddSingleton<BatchWorkflow>();


    /*****
     *   Add Nice Notice enterprise event infrastructure.
     *****/
    serviceCollection.AddNiceNotice(builder => builder
      // Enable typed enterprise events.
      .UseTypedNotices<BatchWorkerEvent>(
        typedNoticeBuilder =>
          /* Route all events to a single logical stream.
           *
           *   Some console applications may benefit from separating `normal` events from `error` events,
           *   following the idea of a console application's `STDOUT` and `STDERR` output streams.
           *
           *   Remember: Stream identities support logical routing; they are not direct representations of I/O streams.
           */
          typedNoticeBuilder.RouteToConstantStream((EventStreamId)"batch-worker-events"),
        ServiceLifetime.Singleton
      )
      // Dispatch enterprise events to Amazon Web Services SNS.
      .DispatchToSns(
        snsTopicsConfigurationSectionPath,
        ServiceLifetime.Singleton
      )
    );

    return hostBuilderContext;
  }
}
