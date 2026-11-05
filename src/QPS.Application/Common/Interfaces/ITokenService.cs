using QPS.Domain.Entities;

namespace QPS.Application.Common.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
}
