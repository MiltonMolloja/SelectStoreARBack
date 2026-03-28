using System.Security.Claims;
using MediatR;
using SelectStoreAR.Application.Commands.Orders;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Orders;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/orders").WithTags("Orders");

        // Public — crear pedido
        group.MapPost("/", PlaceOrder)
            .WithName("PlaceOrder")
            .AllowAnonymous()
            .Produces<PlaceOrderResult>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        // Admin
        group.MapGet("/", GetOrders)
            .WithName("GetOrders")
            .RequireAuthorization("admin")
            .Produces<PagedResult<OrderDto>>();

        group.MapGet("/{id:guid}", GetOrderById)
            .WithName("GetOrderById")
            .RequireAuthorization()
            .Produces<OrderDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/status", UpdateOrderStatus)
            .WithName("UpdateOrderStatus")
            .RequireAuthorization("admin")
            .Produces<OrderDto>()
            .Produces(StatusCodes.Status404NotFound);

        // Registrar precio al enviar cotización por WhatsApp
        group.MapPost("/{orderId:guid}/quote-sent", RecordQuoteSent)
            .WithName("RecordQuoteSent")
            .RequireAuthorization("admin");
    }

    private static async Task<IResult> RecordQuoteSent(
        Guid orderId,
        RecordQuoteRequest body,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        string quotedBy = httpContext.User.Identity?.Name ?? "admin";

        RecordWhatsAppQuoteResult result = await sender.Send(
            new RecordWhatsAppQuoteCommand(body.ProductId, orderId, quotedBy),
            cancellationToken);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> PlaceOrder(
        PlaceOrderCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        PlaceOrderResult result = await sender.Send(command, cancellationToken);
        return Results.Created($"/api/orders/{result.Order.Id}", result);
    }

    private static async Task<IResult> GetOrders(
        ISender sender,
        int page = 1,
        int pageSize = 20,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        PagedResult<OrderDto> result = await sender.Send(
            new GetOrdersQuery(page, pageSize, status),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetOrderById(
        Guid id,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        string? role = httpContext.User.FindFirstValue("role");
        Guid? requestingUserId = null;

        if (role != "admin")
        {
            string? userIdStr = httpContext.User.FindFirstValue("sub")
                ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userIdStr, out Guid userId))
            {
                requestingUserId = userId;
            }
        }

        OrderDto? order = await sender.Send(new GetOrderByIdQuery(id, requestingUserId), cancellationToken);
        return order is null ? Results.NotFound() : Results.Ok(order);
    }

    private static async Task<IResult> UpdateOrderStatus(
        Guid id,
        UpdateOrderStatusRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        OrderDto? order = await sender.Send(
            new UpdateOrderStatusCommand(id, request.Status, request.DepositType, request.Notes),
            cancellationToken);

        return order is null ? Results.NotFound() : Results.Ok(order);
    }
}

public sealed record UpdateOrderStatusRequest(string Status, string? DepositType, string? Notes);

public sealed record RecordQuoteRequest(Guid ProductId);
