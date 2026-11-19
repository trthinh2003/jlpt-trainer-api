using System.Net;
using FluentValidation;
using JlptTrainer.Application.Common.Exceptions;
using JlptTrainer.Application.MockTests.Commands.SubmitMockTestAttempt;
using Microsoft.EntityFrameworkCore;

namespace JlptTrainer.Api.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = exception switch
            {
                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),

                NotFoundException => (HttpStatusCode.NotFound, exception.Message),

                ExcelWorkbookException => (HttpStatusCode.BadRequest, exception.Message),

                MockTestAttemptExpiredException => (HttpStatusCode.Gone, exception.Message),

                ConflictException => (HttpStatusCode.Conflict, exception.Message),

                DbUpdateConcurrencyException => (HttpStatusCode.Conflict, "Dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại."),

                InvalidCredentialsException => (HttpStatusCode.Unauthorized, exception.Message),

                InsufficientDataException => (HttpStatusCode.UnprocessableEntity, exception.Message),

                ExternalServiceException => (HttpStatusCode.BadGateway, exception.Message),

                ForbiddenAccessException => (HttpStatusCode.Forbidden, exception.Message),

                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),

                _ => (HttpStatusCode.InternalServerError, "Đã có lỗi xảy ra, vui lòng thử lại sau.")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception occurred");
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            await context.Response.WriteAsJsonAsync(new { error = message });
        }
    }
}
