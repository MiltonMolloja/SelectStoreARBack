using MediatR;
using SelectStoreAR.Application.Commands.ExchangeRates;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.ExchangeRates;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class ExchangeRateEndpoints
{
    public static void MapExchangeRateEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/exchange-rate").WithTags("ExchangeRate");

        group.MapGet("/", GetExchangeRate)
            .WithName("GetExchangeRate")
            .Produces<ExchangeRateDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/", UpdateExchangeRate)
            .WithName("UpdateExchangeRate")
            .RequireAuthorization("admin")
            .Produces<ExchangeRateDto>()
            .Produces(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GetExchangeRate(
        ISender sender,
        CancellationToken cancellationToken)
    {
        ExchangeRateDto? rate = await sender.Send(new GetExchangeRateQuery(), cancellationToken);
        return rate is null ? Results.NotFound() : Results.Ok(rate);
    }

    private static async Task<IResult> UpdateExchangeRate(
        UpdateExchangeRateCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        ExchangeRateDto rate = await sender.Send(command, cancellationToken);
        return Results.Ok(rate);
    }
}
