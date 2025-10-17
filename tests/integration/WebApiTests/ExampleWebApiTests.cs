using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

using Jds.NiceNotice.Aws.Sns.Tests.Unit;
using Jds.TestingUtils.Randomization;

using Microsoft.Extensions.Logging.Testing;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.WebApiTests;

/// <summary>
///   Tests verifying the example web API functions as expected, in relation to implementing enterprise events.
///   These tests focus on the enterprise events side effects of the API.
///   I.e., verify that it successfully dispatches enterprise events to the configured SNS topics.
/// </summary>
public class ExampleWebApiTests
{
  [ClassDataSource<ExampleApiWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required ExampleApiWebApplicationFactory ExampleApiWebApplicationFactory { get; init; }

  /// <summary>
  ///   Invokes the <c>begin session</c> HTTP API and returns the results.
  /// </summary>
  /// <returns></returns>
  private async Task<(HttpResponseMessage response, BeginSessionResult deserializedContent)> Act_BeginSessionAsync()
  {
    using HttpClient client = ExampleApiWebApplicationFactory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(scheme: "Bearer", parameter: "test-token");

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync(
      requestUri: "/api/sessions/begin",
      new
      {
        userId = $"user-{Randomizer.Shared.IntPositive()}"
      }
    );
    var deserializedContent = await response.DeserializeContentAsync<BeginSessionResult>();

    return (response, deserializedContent);
  }


  /// <summary>
  ///   Invokes the <c>begin session</c> HTTP API and subsequently the <c>end session</c> HTTP API,
  ///   returning the responses of each.
  /// </summary>
  /// <returns></returns>
  private async Task<(BeginSessionResult beginSessionResponseBody, HttpResponseMessage response)>
    Act_BeginAndEndSession()
  {
    using HttpClient client = ExampleApiWebApplicationFactory.CreateClient();
    string userId = $"user-{Randomizer.Shared.IntPositive()}";
    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue(scheme: "Bearer", parameter: "test-token");
    HttpResponseMessage beginApiResponse = await client.PostAsJsonAsync(
      requestUri: "/api/sessions/begin",
      new
      {
        userId
      }
    );
    var beginSessionResponseBody = await beginApiResponse.DeserializeContentAsync<BeginSessionResult>();

    // Act
    HttpResponseMessage response = await client.PostAsJsonAsync(
      requestUri: "/api/sessions/end",
      new
      {
        userId
      }
    );

    return (beginSessionResponseBody, response);
  }

  #region API DTOs

  /// <summary>
  ///   A record expressing the expected schema of the &quot;user session started&quot; enterprise event.
  /// </summary>
  public record BeginSessionEvent
  {
    [JsonPropertyName(name: "schema")]
    public string Schema { get; init; } = string.Empty;

    [JsonPropertyName(name: "timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName(name: "sessionId")]
    public string SessionId { get; init; } = string.Empty;
  }

  /// <summary>
  ///   A record expressing the expected schema of the &quot;user session ended&quot; enterprise event.
  /// </summary>
  public record EndSessionEvent
  {
    [JsonPropertyName(name: "schema")]
    public string Schema { get; init; } = string.Empty;

    [JsonPropertyName(name: "timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName(name: "sessionId")]
    public string SessionId { get; init; } = string.Empty;

    [JsonPropertyName(name: "duration")]
    public TimeSpan? Duration { get; init; }
  }

  /// <summary>
  ///   A record expressing the expected HTTP API response from the &quot;begin session&quot; example web API.
  /// </summary>
  public record BeginSessionResult
  {
    [JsonPropertyName(name: "sessionId")]
    public string? SessionId { get; init; }
  }

  #endregion

  #region Beginning Session

  [Test]
  public async Task BeginningSessionSucceedsWithExpectedResponse()
  {
    (HttpResponseMessage response, BeginSessionResult deserializedContent) = await Act_BeginSessionAsync();

    // Assert
    await Assert
      .That(response.StatusCode)
      .IsEqualTo(HttpStatusCode.OK);

    await Assert
      .That(deserializedContent.SessionId)
      .IsNotNullOrEmpty();
  }

  [Test]
  public async Task BeginningSessionLogsNoErrors()
  {
    (HttpResponseMessage response, BeginSessionResult deserializedContent) = await Act_BeginSessionAsync();

    // Assert - Verify logging (side effect)
    IReadOnlyList<FakeLogRecord> recentLogs = ExampleApiWebApplicationFactory.Collector.GetSnapshot();
    await Assert
      .That(recentLogs)
      .DoesNotContain((FakeLogRecord fakeLogRecord) => fakeLogRecord.Level is LogLevel.Error or LogLevel.Critical);
  }

  /// <summary>
  ///   This test verifies that beginning a session emits a <see cref="BeginSessionEvent" /> to the configured
  ///   user session AWS SNS topic.
  /// </summary>
  [Test]
  public async Task BeginningSessionEmitsExpectedEnterpriseEventToSns()
  {
    const string expectedTopic = "arn:aws:sns:us-east-1:123456789012:user-sessions";
    const string expectedSchema = "UserSessionStarted";
    (HttpResponseMessage response, BeginSessionResult deserializedContent) = await Act_BeginSessionAsync();

    // Assert - Verify SNS emission (side effect)
    using IServiceScope dependencyScope = ExampleApiWebApplicationFactory.Services.CreateScope();
    var sns = dependencyScope.ServiceProvider.GetRequiredService<MockSns>();
    await Assert
      .That(sns.CapturedRequests)
      .Contains(snsMessage =>
        {
          var eventDto = JsonSerializer.Deserialize<BeginSessionEvent>(
            snsMessage.Message,
            JsonDefaults.DefaultJsonSerializerOptions
          );

          bool doesDataMatch = eventDto?.SessionId == deserializedContent.SessionId
                               && eventDto?.Timestamp != DateTime.MinValue
                               && eventDto?.Schema == expectedSchema;
          bool doesTopicMatch = snsMessage.TopicArn == expectedTopic;

          return doesDataMatch && doesTopicMatch;
        }
      );
  }

  #endregion

  #region Ending Session

  [Test]
  public async Task EndingSessionSucceedsWithExpectedResponse()
  {
    (BeginSessionResult beginSessionResponseBody, HttpResponseMessage endSessionResponse) =
      await Act_BeginAndEndSession();

    // Assert
    await Assert
      .That(endSessionResponse.StatusCode)
      .IsEqualTo(HttpStatusCode.OK);
  }

  [Test]
  public async Task EndingSessionLogsNoErrors()
  {
    (BeginSessionResult beginSessionResponseBody, HttpResponseMessage endSessionResponse) =
      await Act_BeginAndEndSession();

    // Assert - Verify logging (side effect)
    IReadOnlyList<FakeLogRecord> recentLogs = ExampleApiWebApplicationFactory.Collector.GetSnapshot();

    await Assert
      .That(recentLogs)
      .DoesNotContain((Func<FakeLogRecord, bool>)(record => record.Level is LogLevel.Error or LogLevel.Critical));
  }

  /// <summary>
  ///   This test verifies that ending a session emits a <see cref="EndSessionEvent" /> to the configured
  ///   user session AWS SNS topic.
  /// </summary>
  [Test]
  public async Task EndingSessionEmitsExpectedEnterpriseEventToSns()
  {
    const string expectedTopic = "arn:aws:sns:us-east-1:123456789012:user-sessions";
    const string expectedSchema = "UserSessionEnded";
    (BeginSessionResult beginSessionResponseBody, HttpResponseMessage endSessionResponse) =
      await Act_BeginAndEndSession();

    // Assert -  Verify SNS emission (side effect)
    using IServiceScope dependencyScope = ExampleApiWebApplicationFactory.Services.CreateScope();
    var sns = dependencyScope.ServiceProvider.GetRequiredService<MockSns>();
    await Assert
      .That(sns.CapturedRequests)
      .Contains(snsMessage =>
        {
          // Note that EndSessionEvent is NOT the type used in the web API (UserSessionEnded). It is defined in this test class.
          //   This shows how tests, like real runtime notification consumers in an organization,
          //   can define their own DTOs based upon a documented notification schema.  
          var eventDto = JsonSerializer.Deserialize<EndSessionEvent>(
            snsMessage.Message,
            JsonDefaults.DefaultJsonSerializerOptions
          );

          bool doesTheDataMatch = eventDto?.SessionId == beginSessionResponseBody.SessionId
                                  && eventDto?.Duration.HasValue == true
                                  && eventDto.Timestamp != DateTime.MinValue
                                  && eventDto.Schema == expectedSchema;
          bool doesTheSnsTopicMatch = snsMessage.TopicArn == expectedTopic;

          return doesTheDataMatch && doesTheSnsTopicMatch;
        }
      );
  }

  #endregion
}
