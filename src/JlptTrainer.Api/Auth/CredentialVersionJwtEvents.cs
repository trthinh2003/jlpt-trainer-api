using System.Globalization;
using System.Security.Claims;
using JlptTrainer.Application.Common.Interfaces;
using JlptTrainer.Domain.Enums;
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
            context.Principal?.Identity is not ClaimsIdentity identity)
        {
            context.Fail("Phiên đăng nhập không còn hợp lệ.");
            return;
        }

        var role = await dbContext.Users
            .Where(user => user.Id == userId && user.CredentialVersion == version)
            .Select(user => (UserRole?)user.Role)
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (role is null)
        {
            context.Fail("Phiên đăng nhập không còn hợp lệ.");
            return;
        }

        // Dùng quyền hiện hành trong database, kể cả khi JWT đã được cấp trước lúc đổi quyền.
        foreach (var claim in identity.FindAll(CatalogAuthorization.ManageClaim).ToArray())
        {
            identity.RemoveClaim(claim);
        }
        if (role == UserRole.CatalogManager)
        {
            identity.AddClaim(new Claim(CatalogAuthorization.ManageClaim, "true", ClaimValueTypes.Boolean));
        }
    }
}
