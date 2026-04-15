namespace ScholarFlow.Domain.Interfaces;

/// <summary>
/// Provides information about the currently authenticated user extracted from the JWT.
/// Inject this anywhere (handlers, services) to access the caller's identity without
/// touching HttpContext directly.
/// </summary>
public interface ICurrentUser
{
    /// <summary>User's Guid — maps to the JWT "sub" claim.</summary>
    Guid UserId { get; }

    /// <summary>User's email — maps to the JWT "email" claim.</summary>
    string Email { get; }

    /// <summary>User's role — maps to the JWT ClaimTypes.Role claim.</summary>
    string Role { get; }

    /// <summary>True when the request carries a valid, authenticated JWT.</summary>
    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
