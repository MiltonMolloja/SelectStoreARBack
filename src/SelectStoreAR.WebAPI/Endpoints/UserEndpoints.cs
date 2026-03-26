using System.Security.Claims;
using MediatR;
using SelectStoreAR.Application.Commands.Auth;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Auth;
using SelectStoreAR.Application.Queries.Orders;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/user")
            .WithTags("User")
            .RequireAuthorization();

        group.MapGet("/profile", GetProfile)
            .WithName("GetUserProfile")
            .Produces<UserDto>()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/profile", UpdateProfile)
            .WithName("UpdateUserProfile")
            .Produces<UserDto>()
            .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/orders", GetUserOrders)
            .WithName("GetUserOrders")
            .Produces<PagedResult<OrderDto>>();

        group.MapGet("/orders/{id:guid}", GetUserOrderById)
            .WithName("GetUserOrderById")
            .Produces<OrderDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static Guid? GetUserId(HttpContext httpContext)
    {
        string? userIdStr = httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdStr, out Guid userId) ? userId : null;
    }

    private static async Task<IResult> GetProfile(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid? userId = GetUserId(httpContext);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        UserDto? user = await sender.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid? userId = GetUserId(httpContext);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        UserDto user = await sender.Send(
            new UpdateUserProfileCommand(userId.Value, request.Name, request.Phone),
            cancellationToken);

        return Results.Ok(user);
    }

    private static async Task<IResult> GetUserOrders(
        HttpContext httpContext,
        ISender sender,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        Guid? userId = GetUserId(httpContext);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        PagedResult<OrderDto> result = await sender.Send(
            new GetOrdersQuery(page, Math.Min(pageSize, 20), UserId: userId.Value),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetUserOrderById(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        Guid? userId = GetUserId(httpContext);
        if (!userId.HasValue)
        {
            return Results.Unauthorized();
        }

        OrderDto? order = await sender.Send(
            new GetOrderByIdQuery(id, RequestingUserId: userId.Value),
            cancellationToken);

        return order is null ? Results.NotFound() : Results.Ok(order);
    }
}

public sealed record UpdateProfileRequest(string Name, string? Phone);
