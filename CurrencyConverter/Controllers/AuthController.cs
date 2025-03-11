using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using CurrencyConverter.Dtos.Login;

namespace MyApi.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthController : ControllerBase
	{
		private readonly IConfiguration _configuration;

		public AuthController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		[HttpPost("login")]
		[ProducesResponseType(200, Type = typeof(LoginResponseDto))]
		public IActionResult Login([FromBody] LoginRequestDto request)
		{
			const string validEmail = "admin@gmail.com";
			const string validPassword = "12345";

			if (request.Email != validEmail || request.Password != validPassword)
			{
				return Unauthorized(new { Message = "Invalid email or password." });
			}

			var tokenResponse = GenerateJwtToken(request.Email);
			return Ok(tokenResponse);
		}

		private LoginResponseDto GenerateJwtToken(string email)
		{
			var jwtSettings = _configuration.GetSection("JwtSettings");
			var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new ArgumentNullException("JwtSettings"));

			var claims = new[]
			{
				new Claim(ClaimTypes.NameIdentifier, email),
				new Claim(JwtRegisteredClaimNames.Sub, email),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
				new Claim(ClaimTypes.Role, "Admin")
			};

			var tokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"]));

			var token = new JwtSecurityToken(
				issuer: jwtSettings["Issuer"],
				audience: jwtSettings["Audience"],
				claims: claims,
				expires: tokenExpiry,
				signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256)
			);

			return new LoginResponseDto
			{
				Token = new JwtSecurityTokenHandler().WriteToken(token),
				ExpiresOn = tokenExpiry
			};
		}
	}
}
