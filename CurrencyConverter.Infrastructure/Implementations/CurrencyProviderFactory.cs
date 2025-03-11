using CurrencyConverter.Infrastructure.Constants;
using CurrencyConverter.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.Infrastructure.Implementations
{
	public class CurrencyProviderFactory(
		[FromKeyedServices(Keys.Frankfurter)] ICurrencyProvider frankfurter) : ICurrencyProviderFactory
	{
		public ICurrencyProvider? Create(string provider)
		{
			switch (provider)
			{
				case Keys.Frankfurter: return frankfurter;
				default: return null;
			}
		}
	}
}
