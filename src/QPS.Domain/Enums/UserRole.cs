namespace QPS.Domain.Enums;

/// <summary>
/// The two roles of the system. Professors do not proctor, so there is no professor role.
/// </summary>
public enum UserRole
{
    Admin = 1,
    TA = 2
}
