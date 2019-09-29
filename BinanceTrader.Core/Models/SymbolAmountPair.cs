using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceTrader.Core.Models
{
    public struct SymbolAmountPair
    {
        public string Symbol { get; set; }
        public decimal Amount { get; set; }

        public static implicit operator (string, decimal)(SymbolAmountPair pair)
        {
            return (pair.Symbol, pair.Amount);
        }

        public static implicit operator SymbolAmountPair((string, decimal) pair)
        {
            return Create(pair.Item1, pair.Item2);
        }

        public static SymbolAmountPair Create(string symbol, decimal amount)
        {
            return new SymbolAmountPair()
            {
                Symbol = symbol,
                Amount = amount,
            };
        }
    }
}
