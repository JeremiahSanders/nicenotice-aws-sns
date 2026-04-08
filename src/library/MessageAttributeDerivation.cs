using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns;

internal static class MessageAttributeDerivation
{
  private const string NumberDataType = "Number";
  private const string StringDataType = "String";

  public static Dictionary<string, MessageAttributeValue>? GetMessageAttributes(IoNoticeDispatchRequest notice)
  {
    Dictionary<string, MessageAttributeValue>? attributes = notice
      .Metadata?
      .Select(GetValues)
      .Where(RequireNonNullOrEmptyNameAndValue)
      .Select(MetadataMap)
      .ToDictionary();

    return attributes is {Count: > 0} ? attributes : null;

    static (string key, string value, string dataType) GetValues(KeyValuePair<string, NoticeMetadataValue> kvp)
    {
      return (kvp.Key, kvp.Value.ToString(), kvp.Value.IsInt || kvp.Value.IsDouble ? NumberDataType : StringDataType);
    }

    static bool RequireNonNullOrEmptyNameAndValue((string key, string value, string dataType) tuple)
    {
      // Corresponds to "Name, type, and value must not be empty or null."
      // Source: https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/SNS/TMessageAttributeValue.html
      return !string.IsNullOrEmpty(tuple.key) && !string.IsNullOrEmpty(tuple.value);
    }

    static KeyValuePair<string, MessageAttributeValue> MetadataMap((string key, string value, string dataType) tuple)
    {
      return new KeyValuePair<string, MessageAttributeValue>(
        tuple.key,
        new MessageAttributeValue
        {
          StringValue = tuple.value,
          DataType = tuple.dataType
        }
      );
    }
  }
}
