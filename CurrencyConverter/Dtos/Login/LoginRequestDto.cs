using System.ComponentModel.DataAnnotations;

namespace CurrencyConverter.Dtos.Login
{
	public class LoginRequestDto
	{
		[Required, EmailAddress, MaxLength(100)]
		public string Email { get; set; } = string.Empty;

		[Required, MaxLength(100)]
		public string Password { get; set; } = string.Empty;
	}
}
