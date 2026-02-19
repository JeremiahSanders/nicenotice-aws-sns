using Amazon.SimpleNotificationService;

using Jds.NiceNotice.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   Methods extending the <see cref="NiceNoticeBuilder" /> to configure SNS dispatching.
/// </summary>
public static class SnsNotificationBuilderExtensions
{
  /// <summary>
  ///   <para>
  ///     Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications
  ///     via a custom <see cref="INoticeIo" /> interface, <see cref="ISnsNoticeIo" />.
  ///     (The <see cref="ISnsNoticeIo" /> interface is also registered by this method,
  ///     in addition to the registration of <see cref="INoticeIo" />.)
  ///   </para>
  ///   <para>
  ///     This overload accepts a <see cref="ISnsTopicOptions" /> configuration object
  ///     to configure how notifications are routed to SNS topics, supporting test scenarios.
  ///     Note that configuring topic ARNs in code is not recommended for production applications.
  ///   </para>
  ///   <para>
  ///     Runtime applications should prefer the <see cref="DispatchToSns(NiceNoticeBuilder, string, ServiceLifetime)" />
  ///     overload.
  ///     That prevents secrets from being embedded in code,
  ///     promotes separation of concerns,
  ///     and allows for dynamic configuration (via use of <see cref="IOptionsMonitor{TOptions}" /> and the
  ///     configuration API).
  ///   </para>
  /// </summary>
  /// <param name="builder">The NiceNoticeBuilder instance to configure.</param>
  /// <param name="topicOptions">
  ///   <para>Topic routing configuration.</para>
  ///   <para>The <see cref="ConfigurationSnsTopicOptions" /> type is useful for in-memory/test configurations.</para>
  /// </param>
  /// <param name="serviceLifetime">
  ///   <para>The service lifetime for the registered components.</para>
  ///   <para>
  ///     Be mindful of
  ///     <a
  ///       href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#captive-dependency">
  ///       captive dependencies,
  ///     </a>
  ///     relating to your registered <see cref="IAmazonSimpleNotificationService" /> implemenentation,
  ///     when selecting the service lifetime.
  ///   </para>
  /// </param>
  /// <returns></returns>
  public static NiceNoticeBuilder DispatchToSns(
    this NiceNoticeBuilder builder,
    ISnsTopicOptions topicOptions,
    ServiceLifetime serviceLifetime
  )
  {
    builder.Services.AddSingleton<ISnsTopicResolver>(
      MappedSnsTopicResolver.Create(
        topicOptions.Map.Select(kvp =>
          new KeyValuePair<EventStreamId, string>(EventStreamId.From(kvp.Key), kvp.Value)
        ),
        topicOptions.Default
      )
    );

    return builder.DispatchToSns(
      static provider => provider.GetServiceOrThrowMissingDependency<IAmazonSimpleNotificationService>(),
      static provider => provider.GetServiceOrThrowMissingDependency<ISnsTopicResolver>(),
      serviceLifetime
    );
  }

  /// <summary>
  ///   <para>
  ///     Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications
  ///     via a custom <see cref="INoticeIo" /> interface, <see cref="ISnsNoticeIo" />.
  ///     (The <see cref="ISnsNoticeIo" /> interface is also registered by this method,
  ///     in addition to the registration of <see cref="INoticeIo" />.)
  ///   </para>
  ///   <para>
  ///     This method binds configuration from a specified configuration section path, resolves
  ///     dependencies for SNS dispatching, and registers them using the desired service lifetime.
  ///   </para>
  /// </summary>
  /// <param name="builder">The NiceNoticeBuilder instance to configure.</param>
  /// <param name="configurationSectionPath">
  ///   The configuration section path from which to bind SNS topic options.
  ///   The configuration section should have an API matching <see cref="ConfigurationSnsTopicOptions" />.
  ///   (Specifically, <see cref="ConfigurationSnsTopicOptions.Default" /> and
  ///   <see cref="ConfigurationSnsTopicOptions.Streams" />.)
  /// </param>
  /// <param name="serviceLifetime">
  ///   <para>The service lifetime for the registered components.</para>
  ///   <para>
  ///     Be mindful of
  ///     <a
  ///       href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#captive-dependency">
  ///       captive dependencies,
  ///     </a>
  ///     relating to your registered <see cref="IAmazonSimpleNotificationService" /> implemenentation,
  ///     when selecting the service lifetime.
  ///   </para>
  /// </param>
  /// <returns>The configured NiceNoticeBuilder instance.</returns>
  /// <exception cref="ArgumentException">Thrown if <paramref name="configurationSectionPath" /> is null or whitespace.</exception>
  public static NiceNoticeBuilder DispatchToSns(
    this NiceNoticeBuilder builder,
    string configurationSectionPath,
    ServiceLifetime serviceLifetime
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(configurationSectionPath);

    builder
      .Services.AddOptions<ConfigurationSnsTopicOptions>()
      .BindConfiguration(configurationSectionPath);

    // Singleton registration is safe because we are using the OptionsMonitor<> API, which automatically updates.
    builder.Services.AddSingleton<ISnsTopicResolver>(static provider =>
      TopicResolvers.Configuration(
        provider.GetServiceOrThrowMissingDependency<IOptionsMonitor<ConfigurationSnsTopicOptions>>()
      )
    );

    return builder.DispatchToSns(
      static provider =>
        MissingDependencyException.ThrowIfNull(provider.GetService<IAmazonSimpleNotificationService>()),
      static provider => provider.GetServiceOrThrowMissingDependency<ISnsTopicResolver>(),
      serviceLifetime
    );
  }

  /// <summary>
  ///   <para>
  ///     Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications
  ///     via a custom <see cref="INoticeIo" /> interface, <see cref="ISnsNoticeIo" />.
  ///     (The <see cref="ISnsNoticeIo" /> interface is also registered by this method,
  ///     in addition to the registration of <see cref="INoticeIo" />.)
  ///   </para>
  ///   <para>
  ///     This overload accepts service resolvers which provide <see cref="ISnsTopicResolver" />
  ///     and  <see cref="IAmazonSimpleNotificationService" />.
  ///     These dependencies are used to construct the implementation, <see cref="SnsNoticeIo" />.
  ///   </para>
  /// </summary>
  /// <param name="builder">The NiceNoticeBuilder instance to configure.</param>
  /// <param name="snsIoProvider"></param>
  /// <param name="topicResolverProvider"></param>
  /// <param name="serviceLifetime">
  ///   <para>The service lifetime for the registered components.</para>
  ///   <para>
  ///     Be mindful of
  ///     <a
  ///       href="https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines#captive-dependency">
  ///       captive dependencies,
  ///     </a>
  ///     relating to <paramref name="snsIoProvider" /> and <paramref name="topicResolverProvider" />,
  ///     when selecting the service lifetime.
  ///   </para>
  /// </param>
  /// <returns></returns>
  public static NiceNoticeBuilder DispatchToSns(
    this NiceNoticeBuilder builder,
    Func<IServiceProvider, IAmazonSimpleNotificationService> snsIoProvider,
    Func<IServiceProvider, ISnsTopicResolver> topicResolverProvider,
    ServiceLifetime serviceLifetime
  )
  {
    builder.Services.Add(
      new ServiceDescriptor(
        typeof(ISnsNoticeIo),
        serviceProvider => new SnsNoticeIo(snsIoProvider(serviceProvider), topicResolverProvider(serviceProvider)),
        serviceLifetime
      )
    );

    // We're declaring `ISnsNoticeIo` as the dispatcher, not the concrete `SnsNoticeIo`, because
    // that is the service interface which was registered above.
    return builder.UseDispatcher<ISnsNoticeIo>(
      serviceProvider => serviceProvider.GetServiceOrThrowMissingDependency<ISnsNoticeIo>(),
      serviceLifetime
    );
  }

  /// <summary>
  ///   A helper method to throw an <see cref="MissingDependencyException" /> if a service is not found.
  /// </summary>
  /// <param name="serviceProvider">This service provider.</param>
  /// <typeparam name="TService">A required service.</typeparam>
  /// <returns>Returns the service, or throws a <see cref="MissingDependencyException" />.</returns>
  /// <exception cref="MissingDependencyException">Thrown if the service is not found.</exception>
  private static TService GetServiceOrThrowMissingDependency<TService>(this IServiceProvider serviceProvider)
    where TService : class
  {
    return MissingDependencyException.ThrowIfNull(serviceProvider.GetService<TService>());
  }
}
