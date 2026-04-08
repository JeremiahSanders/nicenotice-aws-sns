using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

using Jds.NiceNotice;
using Jds.NiceNotice.TypedNotices;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NiceNotice.Tests.ExampleConsoleApp.Notices;

namespace NiceNotice.Tests.ExampleConsoleApp;

/// <summary>
///   <para>This class encapsulates the core work of an example &quot;batch&quot; console application.</para>
/// </summary>
/// <remarks>
///   <para>
///     A batch-oriented application may load data from a file or database,
///     perform some processing,
///     and then write the results to another file or database.
///   </para>
///   <para>
///     Such an application might run on a schedule,
///     might be triggered by an external event,
///     or might be invoked manually.
///   </para>
///   <para>
///     This implementation follows a unit-of-work pattern,
///     in an effort to clarify how and when enterprise events are dispatched.
///     Real-world batch applications often also follow a unit-of-work pattern.
///   </para>
/// </remarks>
public class BatchWorkflow(
  ILogger<BatchWorkflow> logger,
  ITypedNoticeDispatcher<BatchWorkerEvent> dispatcher,
  IOptionsMonitor<BatchWorkflowRequest> defaultOptions
)
{
  public Task<BatchWorkflowResult> RunAsync()
  {
    return RunAsync(defaultOptions.CurrentValue);
  }

  public async Task<BatchWorkflowResult> RunAsync(BatchWorkflowRequest request)
  {
    string inputData;
    Dictionary<string, List<string>> parsedData;
    List<string[]> outputCsvLines;

    var timer = Stopwatch.StartNew();

    try
    {
      Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
      inputData = await LoadInputDataAsync(new Uri(request.InputFile, UriKind.Absolute));
    }
    catch (Exception e)
    {
      timer.Stop();

      return await CreateFailureResponse(e, timer.Elapsed, phase: "Load input data");
    }

    try
    {
      parsedData = ParseData(inputData);
    }
    catch (Exception e)
    {
      timer.Stop();

      return await CreateFailureResponse(e, timer.Elapsed, phase: "Parse data");
    }

    try
    {
      outputCsvLines = GenerateOutputCsvLines(parsedData);
    }
    catch (Exception e)
    {
      timer.Stop();

      return await CreateFailureResponse(e, timer.Elapsed, phase: "Generate output CSV lines");
    }

    try
    {
      await SaveCsvToAwsS3Async(outputCsvLines, request.Output);
    }
    catch (Exception e)
    {
      timer.Stop();

      return await CreateFailureResponse(e, timer.Elapsed, phase: "Save CSV to AWS S3");
    }

    timer.Stop();

    // Dispatch an enterprise event to notify the system that the batch workflow has completed.
    await dispatcher.TryDispatchAsync(
      new BatchExtractionComplete
      {
        OutputLocation = request.Output.AwsS3OutputPath,
        Duration = timer.Elapsed
      },
      (notice, exception) => logger.LogError(
        exception,
        message: "Failed to dispatch batch extraction complete notice. {Notice}",
        notice
      )
    );

    // Dispatch enterprise events to notify other applications of the extracted data.
    //   Parallelization requires that the dispatcher be thread-safe.
    BatchDispatchRequest<BatchWorkerEvent> events = new()
    {
      Notices = parsedData
        .Select(dataEntry => new ExtractedImportantInformation
          {
            DataValueId = dataEntry.Key
          }
        )
        .ToDictionary(notice => Guid.NewGuid().ToString(), BatchWorkerEvent (notice) => notice)
    };
    BatchTypedNoticeDispatchResult batchDispatchResults = await dispatcher.DispatchBatchAsync(events);
    foreach (BatchRoutedTypedNoticeResponse response in batchDispatchResults.Failures)
    {
      logger.LogError(
        response.Exception,
        message: "Failed to dispatch extracted important information notice. {Notice}",
        (BatchWorkerEvent)response.TypedNotice
      );
    }

    logger.LogInformation(message: "Batch workflow completed successfully. Duration: {Duration}", timer.Elapsed);

    return BatchWorkflowResult.Success(request.Output.AwsS3OutputPath, timer.Elapsed);

    // Local function to handle workflow failures uniformly and gracefully.
    async Task<BatchWorkflowResult> CreateFailureResponse(Exception exception, TimeSpan duration, string phase)
    {
      return await HandleFailure(request, exception, duration, phase);
    }
  }

  private List<string[]> GenerateOutputCsvLines(Dictionary<string, List<string>> parsedData)
  {
    // Simulate some translation logic, converting the parsed data into a predefined CSV format.
    // This implementation is not intended to do anything useful.
    IEnumerable<string[]> header =
    [
      [
        "id",
        "data-count"
      ]
    ];

    IEnumerable<string[]> allLines = header.Concat(
      parsedData.Select(kvp => new[]
        {
          kvp.Key,
          kvp.Value.Count.ToString()
        }
      )
    );

    return allLines.ToList();
  }

  /// <summary>
  ///   Provides a uniform way to handle workflow failures.
  /// </summary>
  /// <param name="request">The request that was being processed when the failure occurred.</param>
  /// <param name="exception">The exception that caused the failure.</param>
  /// <param name="duration">The duration of the workflow execution.</param>
  /// <param name="phase">The logical phase of the workflow process.</param>
  /// <returns>Returns a failure response after dispatching a failure notification.</returns>
  private async Task<BatchWorkflowResult> HandleFailure(
    BatchWorkflowRequest request,
    Exception exception,
    TimeSpan duration,
    string phase
  )
  {
    logger.LogError(exception, message: "An batch workflow error occured during '{Phase}'.", phase);

    await dispatcher.TryDispatchAsync(
      new BatchWorkflowExecutionFailed
      {
        OutputLocation = request.Output.AwsS3OutputPath,
        Duration = duration,
        ExceptionType = exception.GetType()
          .FullName ?? string.Empty,
        Message = exception.Message,
        StackTrace = exception.StackTrace is {Length: <= 10_000}
          ? exception.StackTrace
          : exception.StackTrace?[..10_000]
      },
      (notice, dispatchFailureException) => logger.LogError(
        dispatchFailureException,
        message: "Failed to dispatch batch workflow execution failed notice. {Notice}",
        notice
      )
    );

    return BatchWorkflowResult.Failure(exception, request.Output.AwsS3OutputPath, duration);
  }

  private async Task<string> LoadInputDataAsync(Uri requestInputFile)
  {
    // Simulate an I/O delay while reading the input file.
    await Task.Delay(TimeSpan.FromMilliseconds(value: 1));

    // Generate some random data to simulate some data being loaded from a web page, file, or database.
    return string.Join(
      separator: "\n",
      Enumerable
        .Range(start: 0, Random.Shared.Next(minValue: 14, maxValue: 91))
        .Select(_ => Guid.NewGuid())
    );
  }

  private Dictionary<string, List<string>> ParseData(string inputData)
  {
    // Simulate some data parsing logic.
    // This implementation is not intended to do anything useful.
    string[] byLine = inputData.Split(
      [
        "\n"
      ],
      StringSplitOptions.RemoveEmptyEntries
    );
    KeyValuePair<string, List<string>>[] dataSegments = byLine
      .Select(line =>
        new KeyValuePair<string, List<string>>(
          line,
          line
            .Split(
              [
                ",",
                "-",
                "{",
                "}"
              ],
              StringSplitOptions.RemoveEmptyEntries
            )
            .Order()
            .ToList()
        )
      )
      .ToArray();

    return dataSegments
      .DistinctBy(kvp => kvp.Key)
      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
  }

  private async Task SaveCsvToAwsS3Async(
    List<string[]> outputCsvLines,
    BatchWorkflowRequest.OutputDestination outputDestination
  )
  {
    // Simulate an I/O delay while writing the output file.
    await Task.Delay(TimeSpan.FromMilliseconds(value: 1));
  }
}
