using Payment.Domain.Identity;
using System.Security.Claims;

namespace Paymant_Module_NEOXONLINE.Services.Abstractions
{
    public interface ITokenService
    {
        List<Claim> DecryptToken(string token);
        User GetUser(List<Claim> claimsList);
    }
}
