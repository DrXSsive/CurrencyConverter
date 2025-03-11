using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Exceptions
{
	public class InvalidCurrencyException : Exception
	{
		public InvalidCurrencyException(string message) : base(message) { }
	}
}
