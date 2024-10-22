using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Paymant_Module_NEOXONLINE.Services.Abstractions;
using Payment.Application.Payment_DAL.Contracts;
using Payment.Domain.ECommerce;
using Payment.Domain.PayProduct;

namespace Paymant_Module_NEOXONLINE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _unitOfWork;

        public TokenController(ITokenService tokenService, IUnitOfWork unitOfWork)
        {
            _tokenService = tokenService;
            _unitOfWork = unitOfWork;
        }

        /*example token:
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJQYXltYW50X01vZHVsZV9ORU9YT05MSU5FIiwiZXhwIjoxNzMwOTAwOTk5LCJpYXQiOjE2MzMwNjQzMTIsImF1ZCI6IlBheW1hbnRfTW9kdWxlX05FT1hPTkxJTkUiLCJpZCI6IkI2NUQ0NTEzLUNBMjctNEJDNS1BOEJGLUY3REMyNUVFMzlEQiIsImVtYWlsIjoidXNlcjFAZXhhbXBsZS5jb20iLCJjb3VudHJ5IjoiVVNBIiwiYWRkcmVzcyI6IkV4YW1wbGUgc3RyZWV0IiwicGhvbmVOdW1iZXIiOiIxMTExMTExIiwicHJlZmVycmVkX3VzZXJuYW1lIjoiSmFuZSBEb2UiLCJyZWFsbV9hY2Nlc3MiOnsicm9sZXMiOlsidXNlciIsImFkbWluIl19LCJyZXNvdXJjZV9hY2Nlc3MiOnsieW91ci1jbGllbnQtaWQiOnsicm9sZXMiOlsidXNlciJdfX19.rwoJJqeQEBta4nAAUnjE-QA4fe-q7XCiS-syCRy5iNU
        */
        [HttpGet]
        public async Task<IActionResult> Get(string token)
        {
            try
            {
                var claimsList = _tokenService.DecryptToken(token);
                var user = _tokenService.GetUser(claimsList);

                user.Basket.Append(await _unitOfWork.GetRepository<Basket>()
                .AsReadOnlyQueryable()
                .FirstOrDefaultAsync(b => b.User.FirstName.Equals(user.FirstName)));

                return Ok(user);
            }
            catch (SecurityTokenException ex)
            {
                return BadRequest("token is invalid");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest("token does not contain reguired fields");
            }
        }
    }
}
