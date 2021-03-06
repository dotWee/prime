﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Prime.Common;
using Prime.Utility;

namespace Prime.Plugins.Services.OkCoin
{
    /// <author email="scaruana_prime@outlook.com">Sean Caruana</author>
    // https://www.okcoin.com/rest_api.html
    public class OkCoinProvider : IPublicPricingProvider, IAssetPairsProvider, IOrderBookProvider
    {
        private const string OkCoinApiVersion = "v1";
        private const string OkCoinApiUrl = "https://www.okcoin.com/api/" + OkCoinApiVersion;

        private static readonly ObjectId IdHash = "prime:okcoin".GetObjectIdHashCode();

        //Each IP can send maximum of 3000 https requests within 5 minutes. If the 3000 limit is exceeded, the system will automatically block the IP for one hour. 
        //After that hour, the IP will be automatically unfrozen.
        //https://www.okcoin.com/rest_faq.html
        private static readonly IRateLimiter Limiter = new PerMinuteRateLimiter(3000, 5);

        private RestApiClientProvider<IOkCoinApi> ApiProvider { get; }

        private const string PairsCsv = "btcusd,ltcusd,ethusd,etcusd,bchusd";

        public Network Network { get; } = Networks.I.Get("OkCoin");

        public bool Disabled => false;
        public int Priority => 100;
        public string AggregatorName => null;
        public string Title => Network.Name;
        public ObjectId Id => IdHash;
        public IRateLimiter RateLimiter => Limiter;
        public char? CommonPairSeparator => '_';

        public bool IsDirect => true;

        public ApiConfiguration GetApiConfiguration => ApiConfiguration.Standard2;

        public OkCoinProvider()
        {
            ApiProvider = new RestApiClientProvider<IOkCoinApi>(OkCoinApiUrl, this, (k) => null);
        }

        private AssetPairs _pairs;
        public AssetPairs Pairs => _pairs ?? (_pairs = new AssetPairs(3, PairsCsv, this));

        public async Task<bool> TestPublicApiAsync(NetworkProviderContext context)
        {
            var api = ApiProvider.GetApi(context);
            var r = await api.GetTickerAsync("btc_usd").ConfigureAwait(false);

            return r?.ticker.last > 0;
        }

        public Task<AssetPairs> GetAssetPairsAsync(NetworkProviderContext context)
        {
            return Task.Run(() => Pairs);
        }

        public IAssetCodeConverter GetAssetCodeConverter()
        {
            return null;
        }

        private static readonly PricingFeatures StaticPricingFeatures = new PricingFeatures()
        {
            Single = new PricingSingleFeatures() { CanStatistics = true, CanVolume = true }
        };

        public PricingFeatures PricingFeatures => StaticPricingFeatures;

        public async Task<MarketPrices> GetPricingAsync(PublicPricesContext context)
        {
            var api = ApiProvider.GetApi(context);
            var pairCode = context.Pair.ToTicker(this);
            var r = await api.GetTickerAsync(pairCode).ConfigureAwait(false);

            return new MarketPrices(new MarketPrice(Network, context.Pair, r.ticker.last)
            {
                PriceStatistics = new PriceStatistics(Network, context.Pair.Asset2, r.ticker.sell, r.ticker.buy, r.ticker.low, r.ticker.high),
                Volume = new NetworkPairVolume(Network, context.Pair, r.ticker.vol)
            });
        }

        public async Task<OrderBook> GetOrderBookAsync(OrderBookContext context)
        {
            var api = ApiProvider.GetApi(context);
            var pairCode = context.Pair.ToTicker(this).ToLower();

            var r = await api.GetOrderBookAsync(pairCode).ConfigureAwait(false);
            var orderBook = new OrderBook(Network, context.Pair);

            var maxCount = 1000;

            var asks = context.MaxRecordsCount == int.MaxValue ? r.asks.Take(maxCount) : r.asks.Take(context.MaxRecordsCount);
            var bids = context.MaxRecordsCount == int.MaxValue ? r.bids.Take(maxCount) : r.bids.Take(context.MaxRecordsCount);

            foreach (var i in bids.Select(GetBidAskData))
                orderBook.AddBid(i.Item1, i.Item2, true);

            foreach (var i in asks.Select(GetBidAskData))
                orderBook.AddAsk(i.Item1, i.Item2, true);

            return orderBook;
        }

        private Tuple<decimal, decimal> GetBidAskData(decimal[] data)
        {
            decimal price = data[0];
            decimal amount = data[1];

            return new Tuple<decimal, decimal>(price, amount);
        }
    }
}
