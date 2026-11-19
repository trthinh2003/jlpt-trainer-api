using System.ComponentModel.DataAnnotations;
using JlptTrainer.Api.Auth;
using JlptTrainer.Application.Auth.Commands.ChangePassword;
using JlptTrainer.Application.Auth.Commands.Login;
using JlptTrainer.Application.Auth.Commands.Register;
using JlptTrainer.Application.Auth.Commands.ResetPassword;
using JlptTrainer.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

        [Authorize]
        [HttpGet("me")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult<AccountCapabilities>> GetCurrentAccount([FromServices] IAuthorizationService authorization)
        {
            var permission = await authorization.AuthorizeAsync(User, CatalogAuthorization.ManagePolicy);
            return Ok(new AccountCapabilities(permission.Succeeded));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordCommand command, CancellationToken cancellationToken)
        {
            await mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [EnableRateLimiting("password-reset")]
        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest request, [FromServices] IPasswordResetRequestQueue requests)
        {
            if (!requests.TryEnqueue(request.Email))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Tạm thời chưa thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau." });
            }

            return Accepted(value: new { message = "Nếu email này đã được đăng ký, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu." });
        }

        [EnableRateLimiting("password-reset")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
        {
            await mediator.Send(command, cancellationToken);
            return NoContent();
        }
    }

    public sealed record ForgotPasswordRequest([Required, EmailAddress, StringLength(255)] string Email);
    public sealed record AccountCapabilities(bool CanManageCatalog);
}
