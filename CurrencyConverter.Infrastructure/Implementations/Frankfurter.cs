using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Infrastructure.Dtos;
using CurrencyConverter.Infrastructure.Exceptions;
using CurrencyConverter.Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Implementations
{
	public class Frankfurter : ICurrencyProvider
	{
		private readonly ILogger<Frankfurter>  _logger;
		private readonly HttpClient _httpClient;
		private readonly IMemoryCache _cache;
		private readonly AsyncPolicyWrap<HttpResponseMessage> _policy;

		public Frankfurter(ILogger<Frankfurter> logger, IHttpClientFactory httpClientFactory, IMemoryCache cache)
		{
			_httpClient = httpClientFactory.CreateClient(Keys.Frankfurter);
			_logger = logger;
			_cache = cache;

			var retryPolicy = Policy
				.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
				.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

			var circuitBreakerPolicy = Policy
				.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
				.CircuitBreakerAsync(2, TimeSpan.FromSeconds(30),
					onBreak: (outcome, timespan) =>
					{
						Console.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds} seconds.");
					},
					onReset: () =>
					{
						Console.WriteLine("Circuit breaker reset.");
					});

			_policy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
		}

		public async Task<ConversionResultDto?> ConvertCurrency(string from, string to, decimal amount)
		{
			string cacheKey = $"convert-{from}-{to}-{amount}";
			if (!_cache.TryGetValue(cacheKey, out ConversionResultDto? response))
			{
				try
				{
					var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
					_logger.LogInformation("Calling Frankfurter API | CorrelationId: {CorrelationId} | From: {From} | To: {To} | Amount: {Amount} |", correlationId, from, to, amount);

					var httpResponse = await _policy.ExecuteAsync(() => _httpClient.GetAsync($"latest?base={from}&symbols={to}"));

					if (!httpResponse.IsSuccessStatusCode)
					{
						_logger.LogError("Frankfurter API Error | CorrelationId: {CorrelationId} | Status: {StatusCode}", correlationId, httpResponse.StatusCode);

						string errorMessage = await httpResponse.Content.ReadAsStringAsync();
						throw new ApiException($"Failed to convert currency: {httpResponse.StatusCode} - {errorMessage}");
					}

					var result = await httpResponse.Content.ReadAsStringAsync();
					var data = JsonSerializer.Deserialize<JsonElement>(result);

					var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(data.GetProperty("rates").ToString());
					if (rates == null)
					{
						throw new ApiException($"Unable to fetch the rate.");
					}

					decimal rate = rates[to];
					decimal convertedAmount = Math.Round(amount * rate, 2);

					response = new ConversionResultDto
					{
						From = from,
						To = to,
						Amount = amount,
						ConvertedAmount = convertedAmount,
						ExchangeRate = rate,
						Date = DateTime.UtcNow
					};

					_cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
				}
				catch (BrokenCircuitException)
				{
					throw new ApiException("The API provider is unavailable or returning errors.");
				}
			}

			return response;
		}

		public async Task<HistoricalRatesDto?> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize)
		{
			string cacheKey = $"history-{baseCurrency}-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}-{page}-{pageSize}";
			if (!_cache.TryGetValue(cacheKey, out HistoricalRatesDto? response))
			{
				try
				{
					var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
					_logger.LogInformation("Calling Frankfurter API | CorrelationId: {CorrelationId} | Base: {BaseCurrency} | StartDate: {StartDate} | EndDate: {EndDate} | Page: {Page} | PageSize: {PageSize}", correlationId, baseCurrency, startDate, endDate, page, pageSize);

					string apiUrl = $"{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?symbols={baseCurrency}";
					var httpResponse = await _policy.ExecuteAsync(() => _httpClient.GetAsync(apiUrl));

					if (!httpResponse.IsSuccessStatusCode)
					{
						_logger.LogError("Frankfurter API Error | CorrelationId: {CorrelationId} | Status: {StatusCode}", correlationId, httpResponse.StatusCode);

						string errorMessage = await httpResponse.Content.ReadAsStringAsync();
						throw new ApiException($"Failed to fetch historical rates: {httpResponse.StatusCode} - {errorMessage}");
					}

					var result = await httpResponse.Content.ReadAsStringAsync();
					var data = JsonSerializer.Deserialize<JsonElement>(result);

					var rates = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, decimal>>>(data.GetProperty("rates").ToString());

					var pagedRates = new Dictionary<string, Dictionary<string, decimal>>();
					if (rates != null)
					{
						foreach (var (date, rate) in rates.Skip((page - 1) * pageSize).Take(pageSize))
						{
							pagedRates[date] = rate;
						}
					}

					response = new HistoricalRatesDto
					{
						BaseCurrency = baseCurrency,
						Page = page,
						PageSize = pageSize,
						Rates = pagedRates
					};

					_cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
				}
				catch (BrokenCircuitException)
				{
					throw new ApiException("The API provider is unavailable or returning errors.");
				}
			}

			return response;
		}

		public async Task<ExchangeRateDto?> GetLatestExchangeRates(string baseCurrency)
		{
			if (string.IsNullOrWhiteSpace(baseCurrency))
				throw new InvalidCurrencyException("Base currency is required.");

			string cacheKey = $"latest-{baseCurrency}";
			if (!_cache.TryGetValue(cacheKey, out ExchangeRateDto? response))
			{
				try
				{
					var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
					_logger.LogInformation("Calling Frankfurter API | CorrelationId: {CorrelationId} | Base: {BaseCurrency}", correlationId, baseCurrency);

					var httpResponse = await _policy.ExecuteAsync(() => _httpClient.GetAsync($"latest?base={baseCurrency}"));

					if (!httpResponse.IsSuccessStatusCode)
					{
						_logger.LogError("Frankfurter API Error | CorrelationId: {CorrelationId} | Status: {StatusCode}", correlationId, httpResponse.StatusCode);

						string errorMessage = await httpResponse.Content.ReadAsStringAsync();
						throw new ApiException($"Failed to fetch exchange rates: {httpResponse.StatusCode} - {errorMessage}");
					}

					var result = await httpResponse.Content.ReadAsStringAsync();
					var data = JsonSerializer.Deserialize<JsonElement>(result);

					response = new ExchangeRateDto
					{
						BaseCurrency = baseCurrency,
						Date = DateTime.UtcNow,
						Rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(data.GetProperty("rates").ToString()) ?? new()
					};

					_cache.Set(cacheKey, response, TimeSpan.FromMinutes(10));
				}
				catch (BrokenCircuitException)
				{
					throw new ApiException("The API provider is unavailable or returning errors.");
				}
			}

			return response;
		}
	}
}
