using CurrencyConverter.Infrastructure.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Interfaces
{
	public interface ICurrencyProvider
	{
		/// <summary>
		/// Retrieves the latest exchange rates for a given base currency.
		/// </summary>
		/// <param name="baseCurrency">The base currency (e.g., EUR).</param>
		/// <returns>A ExchangeRateDto containing exchange rate information.</returns>
		Task<ExchangeRateDto?> GetLatestExchangeRates(string baseCurrency);

		/// <summary>
		/// Converts an amount from one currency to another.
		/// </summary>
		/// <param name="from">The currency to convert from.</param>
		/// <param name="to">The currency to convert to.</param>
		/// <param name="amount">The amount to convert.</param>
		/// <returns>A ConversionResultDto containing the conversion result.</returns>
		Task<ConversionResultDto?> ConvertCurrency(string from, string to, decimal amount);

		/// <summary>
		/// Retrieves historical exchange rates for a given time period with pagination.
		/// </summary>
		/// <param name="baseCurrency">The base currency (e.g., EUR).</param>
		/// <param name="startDate">The start date for historical data.</param>
		/// <param name="endDate">The end date for historical data.</param>
		/// <param name="page">The page number for pagination.</param>
		/// <param name="pageSize">The number of records per page.</param>
		/// <returns>A HistoricalRatesDto containing historical exchange rate data.</returns>
		Task<HistoricalRatesDto?> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize);
	}
}
