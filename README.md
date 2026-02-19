# NiceNotice.Aws.Sns: SNS I/O adapters for NiceNotice

> NiceNotice.Aws.Sns provides Amazon Web Services (AWS) I/O adapters for NiceNotice cross-application notifications
> which dispatches notifications to Amazon Simple Notification Service (SNS).
>
> _This project is not affiliated with Amazon or Amazon Web Services._

## How to use

1. Install the `NiceNotice.Aws.Sns` NuGet package in your application.
   * Consider adding the `NiceNotice` package as well, so you can update core NiceNotice features.
2. [Configure AWS SNS][aws-configure-docs] in your application.
   * Make sure that your application's service registrations include `Amazon.SimpleNotificationService.IAmazonSimpleNotificationService`. That is the AWS SNS abstraction upon which this library depends.
3. Add NiceNotice service registrations to your application.
   * Configure NiceNotice, including choosing a "base" event notification type. [See examples in NiceNotice documentation.][NiceNotice]
   * Use [`.DispatchToSns()`][DispatchToSns] in the NiceNotice configuration, to enable SNS dispatch.
   * Recommended: Update application configuration to specify SNS topic routing.
4. _Start using NiceNotice!_
   * To send notifications:
     * Add `Jds.NiceNotice.ITypedNoticeDispatcher<YourApplicationBaseEventType>` to your application services' constructor (e.g., ASP.NET Core controllers and other registered services).
     * Use its [`DispatchAsync()`][DispatchAsync] method to send individual notifications.
     * Or use its [`DispatchBatchAsync<YourApplicationBaseEventType>`][DispatchBatchAsync] method to send batches of notifications.

### Example Service Registration

```csharp
services
  .AddNiceNotice(builder => builder
    .UseTypedNotices<MyApplicationEvent>(
      new TypedNoticeConfigurationExtensions.TypedNoticeConfiguration
      {
        RoutingType = TypedNoticeConfigurationExtensions.RoutingTypes.TypeFullName,
        SerializationType = TypedNoticeConfigurationExtensions.SerializationTypes.Json,
        ValidationType = TypedNoticeConfigurationExtensions.ValidationTypes.DataAttributes
      },
      ServiceLifetime.Scoped
    )
    .DispatchToSns( // Dispatch enterprise events to AWS SNS.
      "sns:topics", // IConfiguration section key; should match your application configuration
      ServiceLifetime.Scoped
    )
  );
```

[aws-configure-docs]: https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-config-netcore.html
[NiceNotice]: https://github.com/JeremiahSanders/nicenotice/blob/dev/README.md
[DispatchAsync]: https://github.com/JeremiahSanders/nicenotice/blob/dev/docs/api/Jds.NiceNotice.TypedNotices/ITypedNoticeDispatcher-1/DispatchAsync.md
[DispatchBatchAsync]: https://github.com/JeremiahSanders/nicenotice/blob/dev/docs/api/Jds.NiceNotice.TypedNotices/TypedNoticeDispatcherBatchExtensions/DispatchBatchAsync.md
[DispatchToSns]: https://github.com/JeremiahSanders/nicenotice-aws-sns/blob/dev/docs/api/Jds.NiceNotice.Aws.Sns/SnsNotificationBuilderExtensions/DispatchToSns.md
