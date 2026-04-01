using MediatR;
using SelectStoreAR.Application.Commands.Admin;
using SelectStoreAR.Application.Queries.Admin;
using SelectStoreAR.Application.Queries.Products;
using SelectStoreAR.Domain.Enums;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class PendingChangesEndpoints
{
    public static void MapPendingChangesEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/pending-changes")
            .WithTags("Admin - Pending Changes")
            .RequireAuthorization("admin");

        group.MapGet("/", GetPendingChanges);
        group.MapGet("/batch/{batchId:guid}", GetByBatch);
        group.MapPost("/{id:guid}/approve", ApproveOne);
        group.MapPost("/{id:guid}/reject", RejectOne);
        group.MapPost("/batch/{batchId:guid}/approve", ApproveBatch);
        group.MapPost("/batch/{batchId:guid}/reject", RejectBatch);

        // Price history (public, no admin required)
        app.MapGet("/api/products/{productId:guid}/price-history", GetPriceHistory)
            .WithTags("Products")
            .AllowAnonymous();
    }

    private static async Task<IResult> GetPendingChanges(
        IMediator mediator,
        int page = 1,
        int pageSize = 20,
        string? status = null)
    {
        PendingChangeStatus? parsedStatus = status is not null
            ? Enum.TryParse<PendingChangeStatus>(status, true, out PendingChangeStatus s) ? s : null
            : null;

        GetPendingChangesResult result = await mediator
            .Send(new GetPendingChangesQuery(page, pageSize, parsedStatus))
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetByBatch(Guid batchId, IMediator mediator)
    {
        GetPendingChangesByBatchResult result = await mediator
            .Send(new GetPendingChangesByBatchQuery(batchId))
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    private static async Task<IResult> ApproveOne(
        Guid id,
        IMediator mediator,
        HttpContext httpContext)
    {
        string reviewedBy = httpContext.User.Identity?.Name ?? "admin";

        ApprovePendingChangeResult result = await mediator
            .Send(new ApprovePendingChangeCommand(id, reviewedBy))
            .ConfigureAwait(false);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> RejectOne(
        Guid id,
        IMediator mediator,
        HttpContext httpContext,
        RejectRequest? body = null)
    {
        string reviewedBy = httpContext.User.Identity?.Name ?? "admin";

        RejectPendingChangeResult result = await mediator
            .Send(new RejectPendingChangeCommand(id, reviewedBy, body?.Note))
            .ConfigureAwait(false);

        return result.Success
            ? Results.Ok(result)
            : Results.BadRequest(result);
    }

    private static async Task<IResult> ApproveBatch(
        Guid batchId,
        IMediator mediator,
        HttpContext httpContext)
    {
        string reviewedBy = httpContext.User.Identity?.Name ?? "admin";

        ApproveBatchResult result = await mediator
            .Send(new ApproveBatchCommand(batchId, reviewedBy))
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    private static async Task<IResult> RejectBatch(
        Guid batchId,
        IMediator mediator,
        HttpContext httpContext,
        RejectRequest? body = null)
    {
        string reviewedBy = httpContext.User.Identity?.Name ?? "admin";

        RejectBatchResult result = await mediator
            .Send(new RejectBatchCommand(batchId, reviewedBy, body?.Note))
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    private static async Task<IResult> GetPriceHistory(Guid productId, IMediator mediator)
    {
        GetPriceHistoryResult result = await mediator
            .Send(new GetPriceHistoryQuery(productId))
            .ConfigureAwait(false);

        return Results.Ok(result);
    }

    /// <summary>DTO para el body de reject. Instantiated by ASP.NET model binding.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET model binding")]
    private sealed record RejectRequest(string? Note);
}
