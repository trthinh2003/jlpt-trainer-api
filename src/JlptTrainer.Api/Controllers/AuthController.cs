using JlptTrainer.Application.Auth.Commands.Login;
using JlptTrainer.Application.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace JlptTrainer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ISender mediator) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<AuthResult>> Register(RegisterCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResult>> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
