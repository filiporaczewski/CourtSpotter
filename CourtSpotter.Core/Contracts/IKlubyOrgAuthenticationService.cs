namespace CourtSpotter.Core.Contracts;

public interface IKlubyOrgAuthenticationService
{
    Task<bool> EnsureAuthenticatedAsync(CancellationToken cancellationToken = default);
}