using Amazon.SimpleNotificationService.Model;

namespace Jds.NiceNotice.Aws.Sns;

internal static class MessageAttributeDerivation
{
  private const string ContentTypeKey = "ContentType";
  private const string StringDataType = "String";

  public static Dictionary<string, MessageAttributeValue>? GetMessageAttributes(IoRequestNotice notice)
  {
    Dictionary<string, MessageAttributeValue>? attributes = notice.Metadata?.Select(MetadataMap).ToDictionary();

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

    static KeyValuePair<string, MessageAttributeValue> MetadataMap(KeyValuePair<string, string> kvp)
    {
      return new KeyValuePair<string, MessageAttributeValue>(
        kvp.Key,
        new MessageAttributeValue
        {
          StringValue = kvp.Value,
          DataType = StringDataType
        }
      );
    }
  }
}
