using System.Collections.Generic;
using System.Linq;
using Binance.Net.Objects;
using BinanceTrader.Core.Models;

namespace BinanceTrader.Core.DataAccess
{
    /// <summary>
    /// Thats a quick impl of in memory trade repo.
    /// </summary>
    public class TradeRepository : ITradeRepository
    {
        private List<TraderModel> _traders;
        private List<BinanceStreamTrade> _trades;

        public TradeRepository()
        {
            Flush();
        }

        public void Flush()
        {
            _trades = new List<BinanceStreamTrade>();
            _traders = new List<TraderModel>();
        }

        public BinanceStreamTrade GetTradeById(long tradeId)
        {
            var result = _trades.SingleOrDefault(t => t.TradeId == tradeId);
            return result;
        }

        public List<TraderModel> GetTradersWithBalance(decimal balance, decimal maxFees = 0.1m)
        {
            var minBalance = balance * (100m - maxFees);
            var result = _traders
                .Where(t =>
                t.Balance >= minBalance &&
                t.Balance <= balance).ToList();

            return result;
        }

        public void SaveTrade(BinanceStreamTrade trade)
        {
            _trades.Add(trade);
            var pair = TraderModel.CreatePairFromTrade(trade);

            _traders.Add(pair.buyer);
            _traders.Add(pair.seller);
        }

        public void SaveTrades(List<BinanceStreamTrade> trades)
        {
            foreach (var trade in trades)
            {
                SaveTrade(trade);
            }
        }
    }
}