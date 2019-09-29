using System;

namespace BinanceTrader.Core.Models
{
    public struct SymbolPair : IEquatable<SymbolPair>
    {
        public string Symbol1 { get; }
        public string Symbol2 { get; }

        public SymbolPair(string symbol1, string symbol2)
        {
            Symbol1 = symbol1?.ToUpper() ?? throw new ArgumentNullException(nameof(symbol1));
            Symbol2 = symbol2?.ToUpper() ?? throw new ArgumentNullException(nameof(symbol2));
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            return obj.ToString() == this.ToString();
        }

        public bool Equals(SymbolPair other)
        {
            return other.ToString() == this.ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol1, Symbol2, 12);
        }

        public override string ToString()
        {
            return $"{Symbol1}{Symbol2}";
        }
    }
}