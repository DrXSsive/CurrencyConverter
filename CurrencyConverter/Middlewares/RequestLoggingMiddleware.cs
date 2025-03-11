using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CurrencyConverter.Middlewares
{
	public class RequestLoggingMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<RequestLoggingMiddleware> _logger;

		public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context)
		{
			var stopwatch = Stopwatch.StartNew();
			var clientIp = context.Connection.RemoteIpAddress?.ToString();
			var clientId = GetClientIdFromToken(context);
			var httpMethod = context.Request.Method;
			var endpoint = context.Request.Path;
			var correlationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

			context.Response.OnCompleted(() =>
			{
				stopwatch.Stop();
				var responseTime = stopwatch.ElapsedMilliseconds;
				var statusCode = context.Response.StatusCode;

				_logger.LogInformation("Request Details: {@RequestDetails}",
					new
					{
						ClientIP = clientIp,
						ClientId = clientId,
						HttpMethod = httpMethod,
						Endpoint = endpoint,
						ResponseCode = statusCode,
						ResponseTimeMs = responseTime,
						CorrelationId = correlationId
					});

				return Task.CompletedTask;
			});

			await _next(context);
		}

		private string GetClientIdFromToken(HttpContext context)
		{
			var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
			if (authHeader?.StartsWith("Bearer ") == true)
			{
				var token = authHeader.Substring(7);
				var jwtHandler = new JwtSecurityTokenHandler();
				if (jwtHandler.CanReadToken(token))
				{
					var jwtToken = jwtHandler.ReadJwtToken(token);
					return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
				}
			}

			return "Unknown";
		}
	}
}
