using System;
using System.Collections.Generic;
using System.Text;

namespace BinanceTrader.Core.Utils
{
    public static class SymbolUtilities
    {
        public static string ConstructSymbolPair(string firstSymbol, string secondSymbol)
        {
            return $"{firstSymbol.ToUpper()}{secondSymbol.ToUpper()}";
        }
    }
}
