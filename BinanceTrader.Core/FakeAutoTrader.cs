using System;

namespace BinanceTrader.Core
{
    public class FakeAutoTrader : IAutoTrader
    {
        public EventHandler<ProfitableUserTradedEventArgs> ProfitableUserTradedHandler => HandleEvent;

        private void HandleEvent(object sender, ProfitableUserTradedEventArgs e)
        {
        }
    }
}