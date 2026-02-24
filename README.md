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
     * This is a perfect time to think about the _logical_ "streams" of notifications you'll send. For example, logically sending account creation notifications to a different stream than request timing metrics, which is potentially different again to error notifications. By default, NiceNotice sends each notice to a _logical_ stream matching the notification type name. E.g., `public class SignInEvent : MyBaseType {}` would route to a `SignInEvent` stream. To override this and specify a custom stream name, [add a `[NoticeStream()]` attribute to your notification type][nicenotice-noticestream].
   * Use [`.DispatchToSns()`][DispatchToSns] in the NiceNotice configuration, to enable SNS dispatch.
   * Update application configuration to specify SNS topic routing.
     * Make sure to set the `Default` topic ARN in application configuration (see below).
4. **_Start using NiceNotice!_**
   * **To send notifications:**
     * Add `Jds.NiceNotice.ITypedNoticeDispatcher<YourApplicationBaseEventType>` to your application services' constructor (e.g., ASP.NET Core controllers and other registered services).
     * Use its [`DispatchAsync()`][DispatchAsync] method to send individual notifications.
     * Or use its [`DispatchBatchAsync<YourApplicationBaseEventType>`][DispatchBatchAsync] method to send batches of notifications.

### Simple Example Service Registration

The snippet below shows how NiceNotice can be arranged to send notices to AWS SNS.
This code would exist in `Program.cs`, `Startup.cs`, or similar location where the root application `IServiceCollection` is arranged.

In this example, the developer created a new class `MyApplicationEvent` which is the **base "value object" type** for their application's notices.

Additionally, they've chosen to store their configuration mapping their notifications' logical "streams" to AWS SNS topics in a configuration section named `sns:topics`. (See configuration section below for more details.)

```csharp
services
  .AddNiceNotice(builder => builder
    .UseTypedNotices<MyApplicationEvent>(
      // By default, notices will be serialized to JSON and routed to logical streams based on their type name.
      //   Here, we're enabling data attributes validation, for extra data
      new TypedNoticesBuilderOptions
      {
        ValidationType = TypedNoticesBuilderOptions.ValidationTypes.DataAttributes
      },
      ServiceLifetime.Scoped
    )
    .DispatchToSns( // Dispatch enterprise events to AWS SNS.
      "sns:topics", // IConfiguration section key; should match your application configuration
      ServiceLifetime.Scoped
    )
  );
```

Given the above configuration, the **minimum** configuration required to successfully send a notice to SNS would be to set the _default_ SNS topic.

In the configuration above, the _developer_ decided to store their SNS routing in the `sns:topics` configuration section. **Configuring the default SNS topic is done by setting the `default` property in the configured section** to the desired SNS topic ARN.

For example, provide a `SNS__TOPICS__DEFAULT="arn:aws:sns:eu-west-1:123456789012:example-topic"` environment variable, or set the `$.sns.topics.default` property in the application's `appsettings.json` file.

## Configuring NiceNotice.Aws.Sns (Routing Notifications to SNS Topics)

Routing serialized notifications to AWS SNS **topics** is a key implementation concern when adding SNS I/O to NiceNotice (via [`.DispatchToSns()`][DispatchToSns]).

The **recommended** way to configure SNS topic routing is by using the [`.DispatchToSns()`][DispatchToSns] overload which accepts a `string configurationSectionPath` (as shown in the example above). At runtime, the configuration of NiceNotice routing to SNS topics will be bound to the `IConfiguration` section key in application configuration. That configuration section will be parsed as a [ConfigurationSnsTopicOptions][] object. There is no default configuration section assumed by NiceNotice; it **must** be chosen by the developer. Note that when loading SNS topic routing via configuration, all configured sources will be used (e.g., environment variables, `appsettings.json` files).

Fortunately, the configuration is very simple. As seen in [ConfigurationSnsTopicOptions][], there are two configuration properties. Both are optional, but incomplete configuration can result in runtime exceptions.

* `Default`: **Recommended** A default AWS SNS topic to which notifications will be sent if no topic _for its NiceNotice stream_ is configured in `Streams`.
* `Streams`: A dictionary of `string niceNoticeEventStream` (key) to `string awsSnsTopicArn` (value).

### Example Configuration in a `appsettings.json` File

In the file below, the **_developer_** has chosen to use the `InterAppNotifications:SnsRouting` configuration path.

In this hypothetical application there is a _default_ SNS topic specified for all notifications _not_ mapped in `Streams`.

Notice that the `Streams` shows three _type name_ mappings. From that we can infer that NiceNotice _typed notices_ are configured with `RoutingType = TypedNoticeConfigurationExtensions.RoutingTypes.TypeFullName`. (See the example service registration above for that setting in context.)

```json
{
  "InterAppNotifications": {
    "SnsRouting": {
      "Default": "arn:aws:sns:eu-west-1:123456789012:example-topic",
      "Streams": {
        "MyApp.XappNotices.SignIn": "arn:aws:sns:us-east-1:123456789012:user-sessions",
        "MyApp.XappNotices.SignOut": "arn:aws:sns:us-east-1:123456789012:user-sessions",
        "MyApp.XappNotices.RevokeSession": "arn:aws:sns:us-east-1:123456789012:user-sessions"
      }
    }
  }
}
```

#### Equivalent Configuration in Environment Variables

Below is an equivalent configuration to the `appsettings.json` shown above.

```bash
InterAppNotifications__SnsRouting__Default="arn:aws:sns:eu-west-1:123456789012:example-topic"
InterAppNotifications__SnsRouting__Streams__MyApp.XappNotices.SignIn="arn:aws:sns:us-east-1:123456789012:user-sessions"
InterAppNotifications__SnsRouting__Streams__MyApp.XappNotices.SignOut="arn:aws:sns:us-east-1:123456789012:user-sessions"
InterAppNotifications__SnsRouting__Streams__MyApp.XappNotices.RevokeSession="arn:aws:sns:us-east-1:123456789012:user-sessions"
```

[aws-configure-docs]: https://docs.aws.amazon.com/sdk-for-net/v4/developer-guide/net-dg-config-netcore.html
[ConfigurationSnsTopicOptions]: https://github.com/JeremiahSanders/nicenotice-aws-sns/blob/dev/docs/api/Jds.NiceNotice.Aws.Sns/ConfigurationSnsTopicOptions.md
[DispatchAsync]: https://github.com/JeremiahSanders/nicenotice/blob/dev/docs/api/Jds.NiceNotice.TypedNotices/ITypedNoticeDispatcher-1/DispatchAsync.md
[DispatchBatchAsync]: https://github.com/JeremiahSanders/nicenotice/blob/dev/docs/api/Jds.NiceNotice.TypedNotices/TypedNoticeDispatcherBatchExtensions/DispatchBatchAsync.md
[DispatchToSns]: https://github.com/JeremiahSanders/nicenotice-aws-sns/blob/dev/docs/api/Jds.NiceNotice.Aws.Sns/SnsNotificationBuilderExtensions/DispatchToSns.md
[nicenotice-noticestream]: https://github.com/JeremiahSanders/nicenotice/tree/dev/docs/api/Jds.NiceNotice.TypedNotices/NoticeStreamAttribute.md
[NiceNotice]: https://github.com/JeremiahSanders/nicenotice/blob/dev/README.md
