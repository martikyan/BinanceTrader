using System;

namespace BinanceTrader.Core.Utils
{
    public static class IdentificationUtilities
    {
        public static string GetRandomIdentifier()
        {
            return Guid.NewGuid().ToString();
        }
    }
}