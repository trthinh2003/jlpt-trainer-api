using JlptTrainer.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace JlptTrainer.Infrastructure.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public Guid UserId
        {
            get
            {
                var idClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
                {
                    throw new UnauthorizedAccessException("Không xác định được người dùng hiện tại."); // lỗi hạ tầng
                }

                return userId;
            }
        }
    }
}
