using InternalChat.Domain.Entities;

namespace InternalChat.Application.Interfaces.Services;

/// <summary>
/// Service for generating JWT tokens.
/// </summary>
public interface IJwtProvider
{
    string GenerateToken(User user);
}
