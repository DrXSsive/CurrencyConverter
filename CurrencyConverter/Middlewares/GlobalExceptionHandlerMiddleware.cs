using CurrencyConverter.Infrastructure.Exceptions;
using System.Net;
using System.Text.Json;

namespace CurrencyConverter.Middlewares
{
	public class GlobalExceptionHandlerMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

		public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An unhandled exception occurred.");
				await HandleExceptionAsync(context, ex);
			}
		}

		private static Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			HttpStatusCode statusCode;
			string message;

			if (exception is InvalidCurrencyException || exception is ApiException || exception is ArgumentNullException)
			{
				statusCode = HttpStatusCode.BadRequest;
				message = exception.Message;
			}
			else
			{
				statusCode = HttpStatusCode.InternalServerError;
				message = "An unexpected error occurred. Please try again later.";
			}

			var errorResponse = new
			{
				StatusCode = (int)statusCode,
				Message = message
			};

			context.Response.ContentType = "application/json";
			context.Response.StatusCode = (int)statusCode;
			return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
		}
	}
}
