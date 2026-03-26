using MediatR;

namespace SelectStoreAR.Application.Commands.Products;

public sealed record DeleteProductCommand(Guid Id) : IRequest;
