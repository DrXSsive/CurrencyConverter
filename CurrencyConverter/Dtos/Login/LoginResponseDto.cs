namespace CurrencyConverter.Dtos.Login
{
	public class LoginResponseDto
	{
		public string Token { get; set; } = string.Empty;
		public DateTime ExpiresOn { get; set; }
	}
}
