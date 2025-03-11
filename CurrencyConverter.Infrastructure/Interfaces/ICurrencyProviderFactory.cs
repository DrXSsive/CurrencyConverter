using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Interfaces
{
	public interface ICurrencyProviderFactory
	{
		ICurrencyProvider? Create(string provider);
	}
}
