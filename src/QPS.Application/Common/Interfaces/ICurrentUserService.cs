namespace QPS.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }

    /// <summary>The authenticated user's id; throws when the request is anonymous.</summary>
    Guid RequireUserId();
}
