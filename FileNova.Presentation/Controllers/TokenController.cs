using FileNova.Presentation.Filters;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects;


namespace FileNova.Presentation.Controllers
{
    [Route("api/token")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IServiceManager _service;

        public TokenController(IServiceManager service)
        {
            _service = service;
        }

        [HttpPost("refresh")]
        [ServiceFilter(typeof(ValidationFilterAttribute))]
        public async Task<IActionResult> RefreshToken([FromBody] TokenDto tokenDto)
        {
            var newToken = await _service.AuthenticationService.RefreshToken(tokenDto);
            return Ok(newToken);
        }
    }
}
