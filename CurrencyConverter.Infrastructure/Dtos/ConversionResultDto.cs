using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Dtos
{
	public class ConversionResultDto
	{
		public string From { get; set; } = string.Empty;
		public string To { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public decimal ConvertedAmount { get; set; }
		public decimal ExchangeRate { get; set; }
		public DateTime Date { get; set; }
	}
}
