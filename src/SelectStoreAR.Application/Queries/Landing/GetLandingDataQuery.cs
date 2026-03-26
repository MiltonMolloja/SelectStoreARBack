using MediatR;
using SelectStoreAR.Application.DTOs;

namespace SelectStoreAR.Application.Queries.Landing;

public sealed record GetLandingDataQuery : IRequest<LandingDto>;
