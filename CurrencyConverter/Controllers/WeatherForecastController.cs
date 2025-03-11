using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers
{
	[ApiController]
	[Route("api/v{version:apiVersion}/weather")]
	[ApiVersion(1.0)]
	public class WeatherV1Controller : ControllerBase
	{
		[HttpGet]
		public IActionResult GetWeather()
		{
			return Ok(new { Version = "1.0", Temperature = "22°C" });
		}
	}

	[ApiController]
	[Route("api/v{version:apiVersion}/weather")]
	[ApiVersion(2.0)]
	public class WeatherV2Controller : ControllerBase
	{
		[HttpGet]
		public IActionResult GetWeather()
		{
			return Ok(new { Version = "2.0", Temperature = "24°C", Humidity = "60%" });
		}
	}
}
