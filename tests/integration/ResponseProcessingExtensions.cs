using System.Text.Json;

namespace Jds.NiceNotice.Aws.Sns.Tests.Integration;

internal static class ResponseProcessingExtensions
{
  public static async Task<TBody> DeserializeContentAsync<TBody>(
    this HttpResponseMessage response,
    JsonSerializerOptions? options = null)
    where TBody : notnull
  {
    string stringContent = await response.Content.ReadAsStringAsync();

    try
    {
      return JsonSerializer.Deserialize<TBody>(stringContent, options ?? JsonDefaults.DefaultJsonSerializerOptions)
             ?? throw new Exception("Response not deserialized." + Environment.NewLine + stringContent);
    }
    catch (Exception e)
    {
      throw new Exception("Response not deserialized." + Environment.NewLine + stringContent, e);
    }
  }
}
