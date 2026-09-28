using Identity.Business.DTOs;

namespace Identity.Business.Interfaces.ServiceInterfaces;

public interface ITokenService
{
    AccessToken CreateAccessToken(AuthUser user, IReadOnlyCollection<string> roles);
}