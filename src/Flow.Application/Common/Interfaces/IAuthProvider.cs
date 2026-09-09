namespace Flow.Application.Common.Interfaces;

// Seam for replacing email/password auth with Azure AD SSO.
// Currently satisfied by ASP.NET Core Identity in RegisterCommandHandler.
// Swap the DI registration to wire Azure AD without changing domain logic.
public interface IAuthProvider
{
    Task<bool> ValidateExternalTokenAsync(string token, CancellationToken cancellationToken = default);
}
