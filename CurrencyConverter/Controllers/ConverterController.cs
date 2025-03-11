using Asp.Versioning;
using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Infrastructure.Implementations;
using CurrencyConverter.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;

namespace MyApi.Controllers
{
	[ApiController]
	[Route("api/v{version:apiVersion}/converter")]
	[ApiVersion(1.0)]
	[Authorize]
	public class ConverterController : ControllerBase
	{
		private static readonly HashSet<string> ExcludedCurrencies = new() { "TRY", "PLN", "THB", "MXN" };

		private ICurrencyProvider currencyProvider;

		public ConverterController(ICurrencyProviderFactory currencyProviderFactory)
		{
			currencyProvider = currencyProviderFactory.Create(Keys.Frankfurter) ?? throw new NullReferenceException("External crrency service 'Frankfurter' is null.");
		}

		[HttpGet("latest")]
		[EnableRateLimiting(Keys.RateLimiting_FixedWindow)]
		public async Task<IActionResult> GetLatestExchangeRates([FromQuery] string baseCurrency)
		{
			if (string.IsNullOrWhiteSpace(baseCurrency))
			{
				return BadRequest("Base currency is required.");
			}

			var response = await currencyProvider.GetLatestExchangeRates(baseCurrency);
			if (response == null)
			{
				return NoContent();
			}
			else
			{
				return Ok(response);
			}
		}

		[HttpGet("convert")]
		[EnableRateLimiting(Keys.RateLimiting_FixedWindow)]
		public async Task<IActionResult> ConvertCurrency([FromQuery] string from, [FromQuery] string to, [FromQuery] decimal amount)
		{
			if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
			{
				return BadRequest("Both 'from' and 'to' currencies are required.");
			}

			if (ExcludedCurrencies.Contains(from.ToUpper()) || ExcludedCurrencies.Contains(to.ToUpper()))
			{
				return BadRequest("Currency conversion involving TRY, PLN, THB, and MXN is not allowed.");
			}

			if (amount <= 0)
			{
				return BadRequest("Amount must be greater than zero.");
			}

			var response = await currencyProvider.ConvertCurrency(from, to, amount);
			if (response == null)
			{
				return NoContent();
			}
			else
			{
				return Ok(response);
			}
		}

		[HttpGet("history")]
		[EnableRateLimiting(Keys.RateLimiting_FixedWindow)]
		public async Task<IActionResult> GetHistoricalRates(
			[FromQuery] string baseCurrency,
			[FromQuery] DateTime startDate,
			[FromQuery] DateTime endDate,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10)
		{
			if (string.IsNullOrWhiteSpace(baseCurrency))
			{
				return BadRequest("Base currency is required.");
			}

			if (startDate > endDate)
			{
				return BadRequest("Start date must be before end date.");
			}

			if (page < 1 || pageSize < 1)
			{
				return BadRequest("Page and pageSize must be greater than zero.");
			}

			var response = await currencyProvider.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);
			if (response == null)
			{
				return NoContent();
			}
			else
			{
				return Ok(response);
			}
		}
	}
}
