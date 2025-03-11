using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Dtos
{
	public class ExchangeRateDto
	{
		public string BaseCurrency { get; set; } = string.Empty;
		public Dictionary<string, decimal> Rates { get; set; } = new();
		public DateTime Date { get; set; }
	}
}
