using Jds.NiceNotice.Aws.Sns.Tests.Unit;

using Microsoft.Extensions.Logging.Testing;

using NiceNotice.Tests.ExampleConsoleApp;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration.ConsoleAppTests;

public class BatchWorkflowTests
{
  [Test]
  public async Task BatchWorkflowExecutesSuccessfully()
  {
    // Arrange
    using ConsoleAppHarness consoleAppHarness = new();

    // Act
    BatchWorkflowResult result = await ActAsync(consoleAppHarness);

    // Assert
    await Assert
      .That(result.FailureException)
      .IsNull();

    await Assert
      .That(result.IsSuccessful)
      .IsTrue();
  }

  private async Task<BatchWorkflowResult> ActAsync(ConsoleAppHarness harness)
  {
    BatchWorkflow workflow = harness.ApplicationHost.Services.GetRequiredService<BatchWorkflow>();

    // Act
    BatchWorkflowResult result = await workflow.RunAsync();

    return result;
  }

  [Test]
  public async Task BatchWorkflowDoesNotLogAnyErrors()
  {
    // Arrange
    using ConsoleAppHarness consoleAppHarness = new();

    // Act
    BatchWorkflowResult _ = await ActAsync(consoleAppHarness);

    // Assert
    IReadOnlyList<FakeLogRecord> logs = consoleAppHarness
      .Collector
      .GetSnapshot();
    await Assert
      .That(logs)
      .DoesNotContain(logRecord => logRecord.Level is LogLevel.Error or LogLevel.Critical or LogLevel.Warning);
  }

  [Test]
  public async Task BatchWorkflowLogsCompletion()
  {
    // Arrange
    using ConsoleAppHarness consoleAppHarness = new();

    // Act
    BatchWorkflowResult _ = await ActAsync(consoleAppHarness);

    // Assert
    IReadOnlyList<FakeLogRecord> logs = consoleAppHarness
      .Collector
      .GetSnapshot();
    await Assert
      .That(logs)
      .Contains(logRecord => logRecord.Level is LogLevel.Information &&
                             logRecord.Message.Contains(value: "Batch workflow completed successfully.")
      );
  }

  [Test]
  [Arguments("BatchExtractionComplete")]
  [Arguments("ExtractedImportantInformation")]
  public async Task EmitsEventToSns(string enterpriseEventName)
  {
    // Arrange
    using ConsoleAppHarness consoleAppHarness = new();
    string expectedTopic = ConsoleAppHarness.DefaultConfiguration.Single(kvp => kvp.Key.Equals(
                                 value: "sns:topics:default",
                                 StringComparison.OrdinalIgnoreCase
                               )
                             )
                             .Value
                           ?? throw new InvalidOperationException(message: "Default topic not configured");

    // Act
    BatchWorkflowResult result = await ActAsync(consoleAppHarness);

    // Assert
    MockSns sns = consoleAppHarness.ApplicationHost.Services.GetRequiredService<MockSns>();
    await Assert
      .That(sns.CapturedRequests)
      .Contains(snsMessage =>
        SnsJsonAssertions.MatchesMessageJsonPropertyExact(
          snsMessage,
          rootObjectPropertyName: "schema",
          enterpriseEventName
        )
        && SnsJsonAssertions.MatchesMessageJsonPropertyElement(
          snsMessage,
          rootObjectPropertyName: "timestamp",
          jsonElement => jsonElement.GetDateTimeOffset() != default(DateTimeOffset)
        )
        && snsMessage.TopicArn == expectedTopic
      );
  }
}
