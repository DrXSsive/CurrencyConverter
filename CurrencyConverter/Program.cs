using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using CurrencyConverter.Infrastructure;
using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Middlewares;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Trace;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
	options.ReportApiVersions = true;
	options.AssumeDefaultVersionWhenUnspecified = true;
	options.DefaultApiVersion = new ApiVersion(1, 0);
	options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
	options.GroupNameFormat = "'v'VVV";
	options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
	var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

	foreach (var description in provider.ApiVersionDescriptions)
	{
		options.SwaggerDoc(
			description.GroupName,
			new OpenApiInfo
			{
				Title = $"Currency Converter Documentation {description.ApiVersion}",
				Version = description.ApiVersion.ToString(),
			});
	}

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Enter 'Bearer <your-token-here>'"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] {}
		}
	});
});

// Logging
Log.Logger = new LoggerConfiguration()
	.WriteTo.Console()
	.Enrich.FromLogContext()
	.CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddOpenTelemetry()
	.WithTracing(tracing =>
	{
		tracing
			.AddAspNetCoreInstrumentation()
			.AddHttpClientInstrumentation()
			.AddConsoleExporter();
	});

builder.Services.AddHttpLogging(options =>
{
	options.LoggingFields = HttpLoggingFields.RequestMethod |
							HttpLoggingFields.RequestPath |
							HttpLoggingFields.ResponseStatusCode |
							HttpLoggingFields.Duration;
});

builder.Services.AddRateLimiter(options =>
{
	options.AddFixedWindowLimiter(Keys.RateLimiting_FixedWindow, limiterOptions =>
	{
		limiterOptions.PermitLimit = 5;
		limiterOptions.Window = TimeSpan.FromSeconds(10);
		limiterOptions.QueueLimit = 2;
		limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
	});
});

builder.Services.AddInfrastructureDependencies(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseHttpLogging();

app.UseSerilogRequestLogging();

app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(options =>
	{
		var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

		foreach (var description in provider.ApiVersionDescriptions)
		{
			options.SwaggerEndpoint(
				$"/swagger/{description.GroupName}/swagger.json",
				description.GroupName.ToUpperInvariant());
		}
	});
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();