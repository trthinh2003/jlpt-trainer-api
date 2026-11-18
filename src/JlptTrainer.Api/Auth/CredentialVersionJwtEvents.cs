using System.Globalization;
using System.Security.Claims;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Api.Auth;

public sealed class CredentialVersionJwtEvents(IApplicationDbContext dbContext) : JwtBearerEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var versionClaim = context.Principal?.FindFirstValue(JwtTokenGenerator.CredentialVersionClaim);

        // JWT phát trước migration dùng phiên bản 0 cho đến khi người dùng đổi mật khẩu.
        if (!Guid.TryParse(userIdClaim, out var userId) ||
            !int.TryParse(versionClaim ?? "0", NumberStyles.None, CultureInfo.InvariantCulture, out var version) ||
            !await dbContext.Users.AnyAsync(
                user => user.Id == userId && user.CredentialVersion == version,
                context.HttpContext.RequestAborted))
        {
            context.Fail("Phiên đăng nhập không còn hợp lệ.");
        }
    }
}
