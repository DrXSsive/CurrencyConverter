using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Infrastructure.Implementations;
using CurrencyConverter.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure
{
	public static class InfrastructureDependencies
	{
		public static IServiceCollection AddInfrastructureDependencies(this IServiceCollection services, ConfigurationManager configuration)
		{
			// Adding JWT configurations
			var jwtSettings = configuration.GetSection("JwtSettings");
			var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new ArgumentNullException("JwtSettings"));

			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
			.AddJwtBearer(options =>
			{
				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings["Issuer"],
					ValidAudience = jwtSettings["Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(secretKey)
				};
			});

			services.AddAuthorization();

			services.AddHttpClient(Keys.Frankfurter, client =>
			{
				client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/");
			});

			services.AddMemoryCache();

			// Currency providers
			services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();
			services.AddKeyedScoped<ICurrencyProvider, Frankfurter>(Keys.Frankfurter);

			return services;
		}
	}
}
