using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Infrastructure.Exceptions;
using CurrencyConverter.Infrastructure.Implementations;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CurrencyConverter.Tests
{
	public class FrankfurterTests
	{
		private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
		private readonly IMemoryCache _memoryCache;
		private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
		private readonly Frankfurter _frankfurterService;

		public FrankfurterTests()
		{
			_httpMessageHandlerMock = new Mock<HttpMessageHandler>();

			var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
			{
				BaseAddress = new Uri("https://api.frankfurter.app/")
			};

			_httpClientFactoryMock = new Mock<IHttpClientFactory>();
			_httpClientFactoryMock
				.Setup(x => x.CreateClient(Keys.Frankfurter))
				.Returns(httpClient);

			_memoryCache = new MemoryCache(new MemoryCacheOptions());

			_frankfurterService = new Frankfurter(_httpClientFactoryMock.Object, _memoryCache);
		}

		[Fact]
		public async Task GetLatestExchangeRates_ShouldReturnRates_WhenApiCallIsSuccessful()
		{
			// Arrange
			var baseCurrency = "EUR";
			var fakeApiResponse = new
			{
				rates = new Dictionary<string, decimal> { { "USD", 1.2m } }
			};

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonSerializer.Serialize(fakeApiResponse))
				});

			// Act
			var result = await _frankfurterService.GetLatestExchangeRates(baseCurrency);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(baseCurrency, result.BaseCurrency);
			Assert.Contains("USD", result.Rates);
			Assert.Equal(1.2m, result.Rates["USD"]);
		}

		[Fact]
		public async Task ConvertCurrency_ShouldReturnConvertedAmount_WhenApiCallIsSuccessful()
		{
			// Arrange
			var from = "EUR";
			var to = "USD";
			var amount = 10m;
			var fakeApiResponse = new
			{
				rates = new Dictionary<string, decimal> { { "USD", 1.1m } }
			};

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonSerializer.Serialize(fakeApiResponse))
				});

			// Act
			var result = await _frankfurterService.ConvertCurrency(from, to, amount);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(from, result.From);
			Assert.Equal(to, result.To);
			Assert.Equal(11m, result.ConvertedAmount);
		}

		[Fact]
		public async Task GetLatestExchangeRates_ShouldThrowApiException_WhenApiIsUnavailable()
		{
			// Arrange
			var baseCurrency = "EUR";

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.InternalServerError
				});

			// Act & Assert
			var exception = await Assert.ThrowsAsync<ApiException>(() => _frankfurterService.GetLatestExchangeRates(baseCurrency));
			Assert.Equal("The API provider is unavailable or returning errors.", exception.Message);
		}

		[Fact]
		public async Task ConvertCurrency_ShouldThrowApiException_WhenApiReturnsError()
		{
			// Arrange
			var from = "EUR";
			var to = "USD";
			var amount = 10m;

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.BadRequest,
					Content = new StringContent("Invalid request")
				});

			// Act & Assert
			var exception = await Assert.ThrowsAsync<ApiException>(() => _frankfurterService.ConvertCurrency(from, to, amount));
			Assert.Contains("The API provider is unavailable or returning errors.", exception.Message);
		}

		[Fact]
		public async Task GetLatestExchangeRates_ShouldThrowInvalidCurrencyException_WhenCurrencyIsInvalid()
		{
			// Arrange
			var baseCurrency = "";

			// Act & Assert
			var exception = await Assert.ThrowsAsync<InvalidCurrencyException>(() => _frankfurterService.GetLatestExchangeRates(baseCurrency));
			Assert.Equal("Base currency is required.", exception.Message);
		}

		[Fact]
		public async Task GetHistoricalRates_ShouldReturnPagedRates_WhenApiCallIsSuccessful()
		{
			// Arrange
			var baseCurrency = "EUR";
			var startDate = new DateTime(2024, 01, 01);
			var endDate = new DateTime(2024, 01, 10);
			var page = 1;
			var pageSize = 5;
			var fakeApiResponse = new
			{
				rates = new Dictionary<string, Dictionary<string, decimal>>
			{
				{ "2024-01-01", new Dictionary<string, decimal> { { "USD", 1.2m } } },
				{ "2024-01-02", new Dictionary<string, decimal> { { "USD", 1.3m } } }
			}
			};

			_httpMessageHandlerMock.Protected()
				.Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
				.ReturnsAsync(new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.OK,
					Content = new StringContent(JsonSerializer.Serialize(fakeApiResponse))
				});

			// Act
			var result = await _frankfurterService.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

			// Assert
			Assert.NotNull(result);
			Assert.Equal(baseCurrency, result.BaseCurrency);
			Assert.NotEmpty(result.Rates);
		}
	}
}
