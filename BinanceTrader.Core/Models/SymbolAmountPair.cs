using System;

namespace BinanceTrader.Core.Models
{
    public struct SymbolAmountPair : IEquatable<SymbolAmountPair>
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

        public override bool Equals(object obj)
        {
            return (obj is SymbolAmountPair pair) && Equals(pair);
        }

        public override int GetHashCode()
        {
            return Symbol.GetHashCode() ^ Amount.GetHashCode() ^ -50;
        }

        public bool Equals(SymbolAmountPair other)
        {
            return
                other.Amount == Amount &&
                other.Symbol == Symbol;
        }

        public static bool operator ==(SymbolAmountPair left, SymbolAmountPair right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SymbolAmountPair left, SymbolAmountPair right)
        {
            return !(left == right);
        }
    }
}