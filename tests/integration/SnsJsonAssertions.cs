using System.Linq.Expressions;
using System.Text.Json;

using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration;

public static class SnsJsonAssertions
{
  public static JsonDocument AssertJsonMatches(string json, Expression<Func<JsonDocument, bool>> assertion)
  {
    JsonDocument document = JsonDocument.Parse(json);

    Func<JsonDocument, bool> predicate = assertion.Compile();

    return !predicate(document)
      ? throw new Exception($"JSON did not match predicate: {assertion}")
      : document;
  }

  public static JsonDocument AssertMessageJsonMatches(
    PublishRequest publishRequest,
    Expression<Func<JsonDocument, bool>> assertion
  )
  {
    return AssertJsonMatches(publishRequest.Message, assertion);
  }

  public static bool MatchesJson(
    string json,
    Expression<Func<JsonDocument, bool>> assertion,
    JsonDocumentOptions? options = null
  )
  {
    try
    {
      JsonDocument document = options.HasValue ? JsonDocument.Parse(json, options.Value) : JsonDocument.Parse(json);
      Func<JsonDocument, bool> predicate = assertion.Compile();

      return predicate(document);
    }
    catch (Exception exception)
    {
      throw new Exception($"Failed to assert JSON matches predicate: {assertion}", exception);
    }
  }

  public static bool MatchesMessageJson(
    PublishRequest publishRequest,
    Expression<Func<JsonDocument, bool>> assertion,
    JsonDocumentOptions? options = null
  )
  {
    return MatchesJson(publishRequest.Message, assertion, options);
  }

  public static bool MatchesMessageJsonPropertyExact(
    PublishRequest publishRequest,
    string rootObjectPropertyName,
    string rootObjectPropertyValue,
    JsonDocumentOptions? options = null
  )
  {
    try
    {
      return MatchesMessageJson(
        publishRequest,
        doc => doc
          .RootElement.GetProperty(rootObjectPropertyName)
          .GetString() == rootObjectPropertyValue,
        options
      );
    }
    catch
    {
      return false;
    }
  }
}
