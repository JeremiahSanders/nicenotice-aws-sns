using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

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
      .IsNotNullOrWhitespace();
  }

  [Test]
  public async Task BeginningSessionLogsNoErrors()
  {
    (HttpResponseMessage response, BeginSessionResult deserializedContent) = await Act_BeginSessionAsync();

    // Assert - Verify logging (side effect)
    IReadOnlyList<FakeLogRecord> recentLogs = ExampleApiWebApplicationFactory.Collector.GetSnapshot();
    await Assert
      .That(recentLogs)
      .DoesNotContain(fakeLogRecord => fakeLogRecord.Level is LogLevel.Error or LogLevel.Critical);
  }

  [Test]
  public async Task BeginningSessionEmitsExpectedEnterpriseEventToSns()
  {
    (HttpResponseMessage response, BeginSessionResult deserializedContent) = await Act_BeginSessionAsync();

    // Assert - Verify SNS emission (side effect)
    using IServiceScope dependencyScope = ExampleApiWebApplicationFactory.Services.CreateScope();
    MockSns sns = dependencyScope.ServiceProvider.GetRequiredService<MockSns>();
    await Assert
      .That(sns.CapturedRequests)
      .Contains(snsMessage =>
        {
          BeginSessionEvent? eventDto = JsonSerializer.Deserialize<BeginSessionEvent>(
            snsMessage.Message,
            JsonDefaults.DefaultJsonSerializerOptions
          );

          return eventDto?.SessionId == deserializedContent.SessionId;
        }
      );
  }

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
      .DoesNotContain(fakeLogRecord => fakeLogRecord.Level is LogLevel.Error or LogLevel.Critical);
  }

  [Test]
  public async Task EndingSessionEmitsExpectedEnterpriseEventToSns()
  {
    (BeginSessionResult beginSessionResponseBody, HttpResponseMessage endSessionResponse) =
      await Act_BeginAndEndSession();

    // Assert -  Verify SNS emission (side effect)
    using IServiceScope dependencyScope = ExampleApiWebApplicationFactory.Services.CreateScope();
    MockSns sns = dependencyScope.ServiceProvider.GetRequiredService<MockSns>();
    await Assert
      .That(sns.CapturedRequests)
      .Contains(snsMessage =>
        {
          EndSessionEvent? eventDto = JsonSerializer.Deserialize<EndSessionEvent>(
            snsMessage.Message,
            JsonDefaults.DefaultJsonSerializerOptions
          );

          return eventDto?.SessionId == beginSessionResponseBody.SessionId
                 && eventDto?.Duration.HasValue == true;
        }
      );
  }


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
    BeginSessionResult deserializedContent = await response.DeserializeContentAsync<BeginSessionResult>();

    return (response, deserializedContent);
  }

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
    BeginSessionResult beginSessionResponseBody = await beginApiResponse.DeserializeContentAsync<BeginSessionResult>();

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


  /// <summary>
  ///   A record expressing the expected schema of the &quot;user session started&quot; enterprise event.
  /// </summary>
  public record BeginSessionEvent
  {
    public string Name { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string SessionId { get; init; } = string.Empty;
  }

  /// <summary>
  ///   A record expressing the expected schema of the &quot;user session ended&quot; enterprise event.
  /// </summary>
  public record EndSessionEvent
  {
    public string Name { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
  }

  /// <summary>
  ///   A record expressing the expected API response from the &quot;begin session&quot; example web API.
  /// </summary>
  public record BeginSessionResult
  {
    public string? SessionId { get; init; }
  }
}
