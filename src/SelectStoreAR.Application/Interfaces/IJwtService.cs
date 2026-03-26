using SelectStoreAR.Domain.Entities;

namespace SelectStoreAR.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
