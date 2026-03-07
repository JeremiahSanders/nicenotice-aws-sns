using System.Collections.Concurrent;
using System.Net;

using Amazon.Runtime;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using Endpoint = Amazon.Runtime.Endpoints.Endpoint;

namespace Jds.NiceNotice.Aws.Sns.Tests.Unit;

public class MockSns : IAmazonSimpleNotificationService
{
  private readonly ConcurrentBag<PublishBatchRequest> _capturedBatchRequests = [];
  private readonly ConcurrentBag<PublishRequest> _capturedRequests = [];
  public IEnumerable<PublishBatchRequest> CapturedBatchRequests => _capturedBatchRequests;

  public IEnumerable<PublishRequest> CapturedRequests => _capturedRequests;

  void IDisposable.Dispose()
  {
  }

  IClientConfig IAmazonService.Config => throw new NotImplementedException();

  Task<PublishResponse> IAmazonSimpleNotificationService.PublishAsync(
    PublishRequest request,
    CancellationToken cancellationToken)
  {
    _capturedRequests.Add(request);

    return Task.FromResult(
      new PublishResponse
      {
        HttpStatusCode = HttpStatusCode.OK,
        MessageId = Guid
          .NewGuid()
          .ToString()
      }
    );
  }

  Task<PublishResponse> IAmazonSimpleNotificationService.PublishAsync(
    string topicArn,
    string message,
    CancellationToken cancellationToken)
  {
    _capturedRequests.Add(new PublishRequest(topicArn, message));

    return Task.FromResult(
      new PublishResponse
      {
        HttpStatusCode = HttpStatusCode.OK,
        MessageId = Guid
          .NewGuid()
          .ToString()
      }
    );
  }

  /// <inheritdoc />
  Task<PublishResponse> IAmazonSimpleNotificationService.PublishAsync(
    string topicArn,
    string message,
    string subject,
    CancellationToken cancellationToken)
  {
    _capturedRequests.Add(new PublishRequest(topicArn, message, subject));

    return Task.FromResult(
      new PublishResponse
      {
        HttpStatusCode = HttpStatusCode.OK,
        MessageId = Guid
          .NewGuid()
          .ToString()
      }
    );
  }

  /// <inheritdoc />
  Task<PublishBatchResponse> IAmazonSimpleNotificationService.PublishBatchAsync(
    PublishBatchRequest request,
    CancellationToken cancellationToken)
  {
    _capturedBatchRequests.Add(request);

    return Task.FromResult(
      new PublishBatchResponse
      {
        Successful = request
          .PublishBatchRequestEntries.Select(request => new PublishBatchResultEntry
            {
              Id = request.Id,
              MessageId = Guid.NewGuid().ToString()
            }
          )
          .ToList(),
        HttpStatusCode = HttpStatusCode.OK
      }
    );
  }


  public void ClearCapturedRequests()
  {
    _capturedRequests.Clear();
    _capturedBatchRequests.Clear();
  }

  #region Stubs

  Task<string> IAmazonSimpleNotificationService.SubscribeQueueAsync(
    string topicArn,
    ICoreAmazonSQS sqsClient,
    string sqsQueueUrl)
  {
    throw new NotImplementedException();
  }

  Task<IDictionary<string, string>> IAmazonSimpleNotificationService.SubscribeQueueToTopicsAsync(
    IList<string> topicArns,
    ICoreAmazonSQS sqsClient,
    string sqsQueueUrl)
  {
    throw new NotImplementedException();
  }

  Task<Topic> IAmazonSimpleNotificationService.FindTopicAsync(string topicName)
  {
    throw new NotImplementedException();
  }

  Task IAmazonSimpleNotificationService.AuthorizeS3ToPublishAsync(string topicArn, string bucket)
  {
    throw new NotImplementedException();
  }

  Task<AddPermissionResponse> IAmazonSimpleNotificationService.AddPermissionAsync(
    string topicArn,
    string label,
    List<string> awsAccountId,
    List<string> actionName,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<AddPermissionResponse> IAmazonSimpleNotificationService.AddPermissionAsync(
    AddPermissionRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CheckIfPhoneNumberIsOptedOutResponse> IAmazonSimpleNotificationService.CheckIfPhoneNumberIsOptedOutAsync(
    CheckIfPhoneNumberIsOptedOutRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ConfirmSubscriptionResponse> IAmazonSimpleNotificationService.ConfirmSubscriptionAsync(
    string topicArn,
    string token,
    string authenticateOnUnsubscribe,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ConfirmSubscriptionResponse> IAmazonSimpleNotificationService.ConfirmSubscriptionAsync(
    string topicArn,
    string token,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ConfirmSubscriptionResponse> IAmazonSimpleNotificationService.ConfirmSubscriptionAsync(
    ConfirmSubscriptionRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CreatePlatformApplicationResponse> IAmazonSimpleNotificationService.CreatePlatformApplicationAsync(
    CreatePlatformApplicationRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CreatePlatformEndpointResponse> IAmazonSimpleNotificationService.CreatePlatformEndpointAsync(
    CreatePlatformEndpointRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CreateSMSSandboxPhoneNumberResponse> IAmazonSimpleNotificationService.CreateSMSSandboxPhoneNumberAsync(
    CreateSMSSandboxPhoneNumberRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CreateTopicResponse> IAmazonSimpleNotificationService.CreateTopicAsync(
    string name,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<CreateTopicResponse> IAmazonSimpleNotificationService.CreateTopicAsync(
    CreateTopicRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<DeleteEndpointResponse> IAmazonSimpleNotificationService.DeleteEndpointAsync(
    DeleteEndpointRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<DeletePlatformApplicationResponse> IAmazonSimpleNotificationService.DeletePlatformApplicationAsync(
    DeletePlatformApplicationRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<DeleteSMSSandboxPhoneNumberResponse> IAmazonSimpleNotificationService.DeleteSMSSandboxPhoneNumberAsync(
    DeleteSMSSandboxPhoneNumberRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<DeleteTopicResponse> IAmazonSimpleNotificationService.DeleteTopicAsync(
    string topicArn,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<DeleteTopicResponse> IAmazonSimpleNotificationService.DeleteTopicAsync(
    DeleteTopicRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetDataProtectionPolicyResponse> IAmazonSimpleNotificationService.GetDataProtectionPolicyAsync(
    GetDataProtectionPolicyRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetEndpointAttributesResponse> IAmazonSimpleNotificationService.GetEndpointAttributesAsync(
    GetEndpointAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetPlatformApplicationAttributesResponse> IAmazonSimpleNotificationService.GetPlatformApplicationAttributesAsync(
    GetPlatformApplicationAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetSMSAttributesResponse> IAmazonSimpleNotificationService.GetSMSAttributesAsync(
    GetSMSAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetSMSSandboxAccountStatusResponse> IAmazonSimpleNotificationService.GetSMSSandboxAccountStatusAsync(
    GetSMSSandboxAccountStatusRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetSubscriptionAttributesResponse> IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync(
    string subscriptionArn,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetSubscriptionAttributesResponse> IAmazonSimpleNotificationService.GetSubscriptionAttributesAsync(
    GetSubscriptionAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetTopicAttributesResponse> IAmazonSimpleNotificationService.GetTopicAttributesAsync(
    string topicArn,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<GetTopicAttributesResponse> IAmazonSimpleNotificationService.GetTopicAttributesAsync(
    GetTopicAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListEndpointsByPlatformApplicationResponse> IAmazonSimpleNotificationService.
    ListEndpointsByPlatformApplicationAsync(
      ListEndpointsByPlatformApplicationRequest request,
      CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListOriginationNumbersResponse> IAmazonSimpleNotificationService.ListOriginationNumbersAsync(
    ListOriginationNumbersRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListPhoneNumbersOptedOutResponse> IAmazonSimpleNotificationService.ListPhoneNumbersOptedOutAsync(
    ListPhoneNumbersOptedOutRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListPlatformApplicationsResponse> IAmazonSimpleNotificationService.ListPlatformApplicationsAsync(
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListPlatformApplicationsResponse> IAmazonSimpleNotificationService.ListPlatformApplicationsAsync(
    ListPlatformApplicationsRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSMSSandboxPhoneNumbersResponse> IAmazonSimpleNotificationService.ListSMSSandboxPhoneNumbersAsync(
    ListSMSSandboxPhoneNumbersRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsResponse> IAmazonSimpleNotificationService.ListSubscriptionsAsync(
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsResponse> IAmazonSimpleNotificationService.ListSubscriptionsAsync(
    string nextToken,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsResponse> IAmazonSimpleNotificationService.ListSubscriptionsAsync(
    ListSubscriptionsRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsByTopicResponse> IAmazonSimpleNotificationService.ListSubscriptionsByTopicAsync(
    string topicArn,
    string nextToken,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsByTopicResponse> IAmazonSimpleNotificationService.ListSubscriptionsByTopicAsync(
    string topicArn,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListSubscriptionsByTopicResponse> IAmazonSimpleNotificationService.ListSubscriptionsByTopicAsync(
    ListSubscriptionsByTopicRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListTagsForResourceResponse> IAmazonSimpleNotificationService.ListTagsForResourceAsync(
    ListTagsForResourceRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListTopicsResponse> IAmazonSimpleNotificationService.ListTopicsAsync(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListTopicsResponse> IAmazonSimpleNotificationService.ListTopicsAsync(
    string nextToken,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<ListTopicsResponse> IAmazonSimpleNotificationService.ListTopicsAsync(
    ListTopicsRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<OptInPhoneNumberResponse> IAmazonSimpleNotificationService.OptInPhoneNumberAsync(
    OptInPhoneNumberRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<PutDataProtectionPolicyResponse> IAmazonSimpleNotificationService.PutDataProtectionPolicyAsync(
    PutDataProtectionPolicyRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<RemovePermissionResponse> IAmazonSimpleNotificationService.RemovePermissionAsync(
    string topicArn,
    string label,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<RemovePermissionResponse> IAmazonSimpleNotificationService.RemovePermissionAsync(
    RemovePermissionRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetEndpointAttributesResponse> IAmazonSimpleNotificationService.SetEndpointAttributesAsync(
    SetEndpointAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetPlatformApplicationAttributesResponse> IAmazonSimpleNotificationService.SetPlatformApplicationAttributesAsync(
    SetPlatformApplicationAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetSMSAttributesResponse> IAmazonSimpleNotificationService.SetSMSAttributesAsync(
    SetSMSAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetSubscriptionAttributesResponse> IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync(
    string subscriptionArn,
    string attributeName,
    string attributeValue,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetSubscriptionAttributesResponse> IAmazonSimpleNotificationService.SetSubscriptionAttributesAsync(
    SetSubscriptionAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetTopicAttributesResponse> IAmazonSimpleNotificationService.SetTopicAttributesAsync(
    string topicArn,
    string attributeName,
    string attributeValue,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SetTopicAttributesResponse> IAmazonSimpleNotificationService.SetTopicAttributesAsync(
    SetTopicAttributesRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SubscribeResponse> IAmazonSimpleNotificationService.SubscribeAsync(
    string topicArn,
    string protocol,
    string endpoint,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<SubscribeResponse> IAmazonSimpleNotificationService.SubscribeAsync(
    SubscribeRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<TagResourceResponse> IAmazonSimpleNotificationService.TagResourceAsync(
    TagResourceRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<UnsubscribeResponse> IAmazonSimpleNotificationService.UnsubscribeAsync(
    string subscriptionArn,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<UnsubscribeResponse> IAmazonSimpleNotificationService.UnsubscribeAsync(
    UnsubscribeRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<UntagResourceResponse> IAmazonSimpleNotificationService.UntagResourceAsync(
    UntagResourceRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Task<VerifySMSSandboxPhoneNumberResponse> IAmazonSimpleNotificationService.VerifySMSSandboxPhoneNumberAsync(
    VerifySMSSandboxPhoneNumberRequest request,
    CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }

  Endpoint IAmazonSimpleNotificationService.DetermineServiceOperationEndpoint(AmazonWebServiceRequest request)
  {
    throw new NotImplementedException();
  }

  ISimpleNotificationServicePaginatorFactory IAmazonSimpleNotificationService.Paginators =>
    throw new NotImplementedException();

  #endregion
}
