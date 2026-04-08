using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns;

internal static class MessageAttributeDerivation
{
  private const string ContentTypeKey = "ContentType";
  private const string NumberDataType = "Number";
  private const string StringDataType = "String";

  public static Dictionary<string, MessageAttributeValue>? GetMessageAttributes(IoNoticeDispatchRequest notice)
  {
    Dictionary<string, MessageAttributeValue>? attributes = notice
      .Metadata?
      .Where(RequireNonNullOrEmptyNameAndValue)
      .Select(MetadataMap)
      .ToDictionary();

    // Set content type, but only if it's not already set
    if (!string.IsNullOrWhiteSpace(notice.ContentType) &&
        (attributes == null || !attributes.ContainsKey(ContentTypeKey)))
    {
      attributes ??= new Dictionary<string, MessageAttributeValue>();
      attributes[ContentTypeKey] = new MessageAttributeValue
      {
        StringValue = notice.ContentType,
        DataType = StringDataType
      };
    }

    return attributes;

    static bool RequireNonNullOrEmptyNameAndValue(KeyValuePair<string, NoticeMetadataValue> kvp)
    {
      // Corresponds to "Name, type, and value must not be empty or null."
      // Source: https://docs.aws.amazon.com/sdkfornet/v4/apidocs/items/SNS/TMessageAttributeValue.html
      return !string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value.ToString());
    }

    static KeyValuePair<string, MessageAttributeValue> MetadataMap(KeyValuePair<string, NoticeMetadataValue> kvp)
    {
      return new KeyValuePair<string, MessageAttributeValue>(
        kvp.Key,
        new MessageAttributeValue
        {
          StringValue = kvp.Value.ToString(),
          DataType = kvp.Value.IsInt || kvp.Value.IsDouble ? NumberDataType : StringDataType
        }
      );
    }
  }
}
