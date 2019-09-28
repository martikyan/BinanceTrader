using System;
using System.Threading;
using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using BinanceTrader.Core.DataAccess;
using BinanceTrader.Core.Models;
using System.Linq;

namespace BinanceTrader.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            var manualResetEvent = new ManualResetEvent(false);
            var tradeRepository = new TradeRepository();
            var clientOptions = new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials("ulpzzxiPL0AXbOMSMHibmqA0rRqs9FxU45T2MTlB7PwFSX6wWJwg6g3Ubb2etup3", "H2PuW26sWwYwkpzwsNt7lpXd4y9f61zdSAookNRqxHSODKidkVcTZ9barBwRF5nA"),
            };

            //using (var client = new BinanceClient(new BinanceClientOptions() { ApiCredentials = new ApiCredentials("ulpzzxiPL0AXbOMSMHibmqA0rRqs9FxU45T2MTlB7PwFSX6wWJwg6g3Ubb2etup3", "H2PuW26sWwYwkpzwsNt7lpXd4y9f61zdSAookNRqxHSODKidkVcTZ9barBwRF5nA") }))
            //{

            //    var lastId = 97808846l;
            //    foreach (var i in Enumerable.Range(1, 100))
            //    {
            //        var data = client.GetHistoricalTrades("ETHUSDT", 1000, fromId: lastId);
            //        foreach (var trade in data.Data)
            //        {
            //            var tradeStreamModel = new BinanceStreamTrade()
            //            {
            //                TradeId = trade.Id,
            //                Price = trade.Price,
            //                Quantity = trade.Quantity,
            //                TradeTime = trade.Time,
            //            };
            //            if (lastId < trade.Id)
            //            {
            //                lastId = trade.Id;
            //            }

            //            tradeRepository.SaveTrade(tradeStreamModel);
            //        }
            //    }
            //}

            using (var client = new BinanceSocketClient(clientOptions))
            {
                var successTrades = client.SubscribeToTradesStream("ETHUSDT", (trade) =>
                {
                    var tradeBuyerResult = TradeResultModel.FromTrade(trade, true);
                    var tradeSellerResult = TradeResultModel.FromTrade(trade, false);

                    var buyerResults = tradeRepository.GetTradeResultsByBuyerBalance(tradeBuyerResult.Balance);
                    var sellerResults = tradeRepository.GetTradeResultsBySellerBalance(tradeSellerResult.Balance);

                    if (buyerResults.Count != 0)
                    {
                        Console.WriteLine($"Found {buyerResults.Count} buyer accounts that we can associate to the trade with info:");
                        Console.WriteLine($"TradeId: {trade.TradeId}");
                        Console.WriteLine($"Quantity: {trade.Quantity}");
                        Console.WriteLine($"Price: {trade.Price}");
                        Console.WriteLine();

                        foreach (var result in buyerResults)
                        {
                            Console.WriteLine($"From tradeId {result.FromTradeId}");
                            Console.WriteLine($"Balance {result.Balance}");
                        }

                        Console.WriteLine("Done buyers results associations.");
                    }

                    if (sellerResults.Count != 0)
                    {
                        Console.WriteLine($"Found {sellerResults.Count} seller accounts that we can associate to the trade with info:");
                        Console.WriteLine($"TradeId: {trade.TradeId}");
                        Console.WriteLine($"Quantity: {trade.Quantity}");
                        Console.WriteLine($"Price: {trade.Price}");
                        Console.WriteLine();

                        foreach (var result in sellerResults)
                        {
                            Console.WriteLine($"From tradeId {result.FromTradeId}");
                            Console.WriteLine($"Balance {result.Balance}");
                        }

                        Console.WriteLine("Done seller results associations.");
                    }

                    tradeRepository.SaveTrade(trade);
                });
                manualResetEvent.WaitOne();
            }

        }
    }
}
