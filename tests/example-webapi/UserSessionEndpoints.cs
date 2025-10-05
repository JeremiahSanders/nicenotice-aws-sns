using Microsoft.AspNetCore.Mvc;

namespace NiceNotice.Tests.ExampleWebApi;

public static class UserSessionEndpoints
{
  public static void Map(WebApplication app)
  {
    app
      .MapPost(
        pattern: "/api/sessions/begin",
        BeginSessionHandler
      )
      .WithName(endpointName: "BeginSession")
      .WithOpenApi();

    app
      .MapPost(
        pattern: "/api/sessions/end",
        EndSessionHandler
      )
      .WithName(endpointName: "EndSession")
      .WithOpenApi();
  }

  private static async Task<IResult> EndSessionHandler(
    [FromHeader(Name = "Authorization")]
    string authorizationHeader,
    [FromBody]
    BeginSessionRequest request,
    IUserSessionService service)
  {
    if (string.IsNullOrWhiteSpace(request.UserId))
    {
      return Results.BadRequest($"{nameof(request.UserId)} is required.");
    }

    if (string.IsNullOrWhiteSpace(authorizationHeader))
    {
      return Results.Unauthorized();
    }

    string token = authorizationHeader
      .Split(separator: ' ')
      .Last();

    EndUserSessionResult endSessionResult = await service.EndSessionAsync(request.UserId, token);

    return endSessionResult.SessionId != null ? Results.Ok() : Results.Forbid();
  }

  private static async Task<IResult> BeginSessionHandler(
    [FromHeader(Name = "Authorization")]
    string authorizationHeader,
    [FromBody]
    BeginSessionRequest request,
    IUserSessionService service)
  {
    if (string.IsNullOrWhiteSpace(request.UserId))
    {
      return Results.BadRequest($"{nameof(request.UserId)} is required.");
    }

    if (string.IsNullOrWhiteSpace(authorizationHeader))
    {
      return Results.Unauthorized();
    }

    string token = authorizationHeader
      .Split(separator: ' ')
      .Last();

    BeginSessionResult result = await service.BeginSessionAsync(request.UserId, token);

    return Results.Ok(
      new BeginSessionResponse
      {
        SessionId = result.SessionId
      }
    );
  }
}