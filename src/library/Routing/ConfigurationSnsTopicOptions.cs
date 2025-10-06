using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jds.NiceNotice.Aws.Sns;

/// <summary>
///   A configuration class for SNS topic mappings.
/// </summary>
/// <remarks>
///   <para>
///     It is expected that this class is configured using the <see cref="IConfiguration" /> API,
///     e.g., using an <c>appsettings.json</c> file, environment variables, or runtime arguments.
///   </para>
///   <para>
///     This class is registered as an <see cref="IOptions{TOptions}" /> service when using the
///     <see
///       cref="SnsNotificationBuilderExtensions.WithSnsDispatch" />
///     extension method during application startup.
///   </para>
/// </remarks>
public class ConfigurationSnsTopicOptions : ISnsTopicOptions
{
  /// <summary>
  ///   Gets or sets the map of event streams to SNS topics.
  ///   Keys are exact values matching <see cref="EventStreamId" />.
  ///   Values are SNS topic ARNs.
  /// </summary>
  public Dictionary<string, string> Streams { get; set; } = [];

  /// <inheritdoc />
  IReadOnlyDictionary<string, string> ISnsTopicOptions.Map => Streams;

  /// <summary>
  ///   Gets or sets the default SNS topic used when a specific mapping for a stream is not found.
  /// </summary>
  public string? Default { get; set; }
}
