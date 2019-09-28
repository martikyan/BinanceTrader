using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
