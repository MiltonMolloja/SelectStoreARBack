using MediatR;
using SelectStoreAR.Application.DTOs;
using SelectStoreAR.Application.Queries.Landing;

namespace SelectStoreAR.WebAPI.Endpoints;

public static class LandingEndpoints
{
    public static void MapLandingEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/landing").WithTags("Landing");

        group.MapGet("/", GetLandingData)
            .WithName("GetLandingData")
            .Produces<LandingDto>();
    }

    private static async Task<IResult> GetLandingData(
        ISender sender,
        CancellationToken cancellationToken)
    {
        LandingDto data = await sender.Send(new GetLandingDataQuery(), cancellationToken);
        return Results.Ok(data);
    }
}
