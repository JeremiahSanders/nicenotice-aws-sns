# SnsNotificationBuilderExtensions.DispatchToSns method (1 of 3)

Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications via a custom INoticeIo interface, [`ISnsNoticeIo`](../ISnsNoticeIo.md). (The [`ISnsNoticeIo`](../ISnsNoticeIo.md) interface is also registered by this method, in addition to the registration of INoticeIo.)

This overload accepts a [`ISnsTopicOptions`](../ISnsTopicOptions.md) configuration object to configure how notifications are routed to SNS topics, supporting test scenarios. Note that configuring topic ARNs in code is not recommended for production applications.

Runtime applications should prefer the [`DispatchToSns`](./DispatchToSns.md) overload. That prevents secrets from being embedded in code, promotes separation of concerns, and allows for dynamic configuration (via use of IOptionsMonitor and the configuration API).

```csharp
public static NiceNoticeBuilder DispatchToSns(this NiceNoticeBuilder builder, 
    ISnsTopicOptions topicOptions, ServiceLifetime serviceLifetime)
```

| parameter | description |
| --- | --- |
| builder | The NiceNoticeBuilder instance to configure. |
| topicOptions | Topic routing configuration. |
| serviceLifetime | The service lifetime for the registered components. |

## Remarks

Registers [`SnsNoticeIo`](../SnsNoticeIo.md) as [`ISnsNoticeIo`](../ISnsNoticeIo.md), as INoticeIo, and as INoticeBatchIo in the Services service collection.

## See Also

* interface [ISnsTopicOptions](../ISnsTopicOptions.md)
* class [SnsNotificationBuilderExtensions](../SnsNotificationBuilderExtensions.md)
* namespace [Jds.NiceNotice.Aws.Sns](../../NiceNotice.Aws.Sns.md)

---

# SnsNotificationBuilderExtensions.DispatchToSns method (2 of 3)

Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications via a custom INoticeIo interface, [`ISnsNoticeIo`](../ISnsNoticeIo.md). (The [`ISnsNoticeIo`](../ISnsNoticeIo.md) interface is also registered by this method, in addition to the registration of INoticeIo.)

This method binds configuration from a specified configuration section path, resolves dependencies for SNS dispatching, and registers them using the desired service lifetime.

```csharp
public static NiceNoticeBuilder DispatchToSns(this NiceNoticeBuilder builder, 
    string configurationSectionPath, ServiceLifetime serviceLifetime)
```

| parameter | description |
| --- | --- |
| builder | The NiceNoticeBuilder instance to configure. |
| configurationSectionPath | The configuration section path from which to bind SNS topic options. The configuration section should have an API matching [`ConfigurationSnsTopicOptions`](../ConfigurationSnsTopicOptions.md). (Specifically, [`Default`](../ConfigurationSnsTopicOptions/Default.md) and [`Streams`](../ConfigurationSnsTopicOptions/Streams.md).) |
| serviceLifetime | The service lifetime for the registered components. |

## Return Value

The configured NiceNoticeBuilder instance.

## Exceptions

| exception | condition |
| --- | --- |
| ArgumentException | Thrown if *configurationSectionPath* is null or whitespace. |

## Remarks

Registers [`SnsNoticeIo`](../SnsNoticeIo.md) as [`ISnsNoticeIo`](../ISnsNoticeIo.md), as INoticeIo, and as INoticeBatchIo in the Services service collection.

Registers IOptions services for [`ConfigurationSnsTopicOptions`](../ConfigurationSnsTopicOptions.md). Binds its configuration to the values specified in the *configurationSectionPath* configuration section.

## See Also

* class [SnsNotificationBuilderExtensions](../SnsNotificationBuilderExtensions.md)
* namespace [Jds.NiceNotice.Aws.Sns](../../NiceNotice.Aws.Sns.md)

---

# SnsNotificationBuilderExtensions.DispatchToSns method (3 of 3)

Configures the NiceNoticeBuilder to use Amazon SNS for dispatching notifications via a custom INoticeIo interface, [`ISnsNoticeIo`](../ISnsNoticeIo.md). (The [`ISnsNoticeIo`](../ISnsNoticeIo.md) interface is also registered by this method, in addition to the registration of INoticeIo.)

This overload accepts service resolvers which provide [`ISnsTopicResolver`](../ISnsTopicResolver.md) and IAmazonSimpleNotificationService. These dependencies are used to construct the implementation, [`SnsNoticeIo`](../SnsNoticeIo.md).

```csharp
public static NiceNoticeBuilder DispatchToSns(this NiceNoticeBuilder builder, 
    Func<IServiceProvider, IAmazonSimpleNotificationService> snsIoProvider, 
    Func<IServiceProvider, ISnsTopicResolver> topicResolverProvider, 
    ServiceLifetime serviceLifetime)
```

| parameter | description |
| --- | --- |
| builder | The NiceNoticeBuilder instance to configure. |
| snsIoProvider |  |
| topicResolverProvider |  |
| serviceLifetime | The service lifetime for the registered services. |

## Remarks

Registers [`SnsNoticeIo`](../SnsNoticeIo.md) as [`ISnsNoticeIo`](../ISnsNoticeIo.md), as INoticeIo, and as INoticeBatchIo in the Services service collection.

## See Also

* interface [ISnsTopicResolver](../ISnsTopicResolver.md)
* class [SnsNotificationBuilderExtensions](../SnsNotificationBuilderExtensions.md)
* namespace [Jds.NiceNotice.Aws.Sns](../../NiceNotice.Aws.Sns.md)

<!-- DO NOT EDIT: generated by xmldocmd for NiceNotice.Aws.Sns.dll -->
