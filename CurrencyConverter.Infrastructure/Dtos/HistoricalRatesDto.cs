using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Dtos
{
	public class HistoricalRatesDto
	{
		public string BaseCurrency { get; set; } = string.Empty;
		public int Page { get; set; }
		public int PageSize { get; set; }
		public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();
	}
}
