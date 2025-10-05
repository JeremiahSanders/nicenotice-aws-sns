using Amazon.SimpleNotificationService;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jds.NiceNotice.Aws.Sns;

public static class SnsNotificationBuilderExtensions
{
  public static NiceNoticeBuilder WithSnsDispatch(
    this NiceNoticeBuilder builder,
    Action<SnsTopicResolverBuilder> configureTopics,
    ServiceLifetime serviceLifetime
  )
  {
    SnsTopicResolverBuilder topicBuilder = new();
    configureTopics(topicBuilder);
    builder.Services.AddSingleton(topicBuilder.Build());

    return builder.WithSnsDispatch(
      static provider =>
        MissingDependencyException.ThrowIfNull(provider.GetService<IAmazonSimpleNotificationService>()),
      static provider => MissingDependencyException.ThrowIfNull(provider.GetService<ISnsTopicResolver>()),
      serviceLifetime
    );
  }

  /// <summary>
  ///   Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications.
  ///   This method binds configuration from a specified configuration section path, resolves
  ///   dependencies for SNS dispatching, and registers them using the desired service lifetime.
  /// </summary>
  /// <param name="builder">The NiceNoticeBuilder instance to configure.</param>
  /// <param name="configurationSectionPath">
  ///   The configuration section path from which to bind SNS topic options.
  ///   The configuration section should have an API matching <see cref="ConfigurationSnsTopicOptions" />.
  /// </param>
  /// <param name="serviceLifetime">The service lifetime for the registered components.</param>
  /// <returns>The configured NiceNoticeBuilder instance.</returns>
  /// <exception cref="ArgumentException">Thrown if <paramref name="configurationSectionPath" /> is null or whitespace.</exception>
  public static NiceNoticeBuilder WithSnsDispatch(
    this NiceNoticeBuilder builder,
    string configurationSectionPath,
    ServiceLifetime serviceLifetime
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(configurationSectionPath, nameof(configurationSectionPath));

    builder
      .Services.AddOptions<ConfigurationSnsTopicOptions>()
      .BindConfiguration(configurationSectionPath);
    // Singleton registration is safe because we are using the OptionsMonitor<> API, which automatically updates.
    builder.Services.TryAddSingleton<ISnsTopicResolver, ConfigurationSnsTopicResolver<ConfigurationSnsTopicOptions>>();

    return builder.WithSnsDispatch(
      static provider =>
        MissingDependencyException.ThrowIfNull(provider.GetService<IAmazonSimpleNotificationService>()),
      static provider => MissingDependencyException.ThrowIfNull(provider.GetService<ISnsTopicResolver>()),
      serviceLifetime
    );
  }

  public static NiceNoticeBuilder WithSnsDispatch(
    this NiceNoticeBuilder builder,
    Func<IServiceProvider, ISnsTopicResolver> topicResolverProvider,
    ServiceLifetime serviceLifetime
  )
  {
    return builder.WithSnsDispatch(
      static provider =>
        MissingDependencyException.ThrowIfNull(provider.GetService<IAmazonSimpleNotificationService>()),
      topicResolverProvider,
      serviceLifetime
    );
  }


  public static NiceNoticeBuilder WithSnsDispatch(
    this NiceNoticeBuilder builder,
    Func<IServiceProvider, IAmazonSimpleNotificationService> snsIoProvider,
    Func<IServiceProvider, ISnsTopicResolver> topicResolverProvider,
    ServiceLifetime serviceLifetime
  )
  {
    return builder.UseDispatcher<ISnsNoticeIo>(
      serviceProvider => new SnsNoticeIo(snsIoProvider(serviceProvider), topicResolverProvider(serviceProvider)),
      serviceLifetime
    );
  }
}
