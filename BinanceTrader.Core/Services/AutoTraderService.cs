﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BinanceTrader.Core.Services
{
    public class AutoTraderService
    {
        private readonly CoreConfiguration _config;
        private readonly List<string> _trackingUsers;

        public AutoTraderService(CoreConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void StartAutoTrader()
        {

        }
    }
}