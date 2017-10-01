﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using plugins;
using Prime.Core;
using Prime.Core.Wallet;
using Prime.Plugins.Services.BitMex;

namespace Prime.TestConsole
{
    public partial class Program
    {
        public class BitMexTests
        {
            public void TestApi()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();

                var ctx = new ApiTestContext(UserContext.Current.GetApiKey(provider));
  
                try
                {
                    var r = AsyncContext.Run(() => provider.TestApiAsync(ctx));

                    Console.WriteLine($"Api success: {r}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            public void TestPortfolioAccountBalances()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();

                var c = new PortfolioProviderContext(UserContext.Current, provider, UserContext.Current.BaseAsset, 0);
                var scanner = new PortfolioProvider(c);
                try
                {
                    scanner.Update();
                    foreach (var i in scanner.Items)
                        Console.WriteLine(i.Asset.ShortCode + " " + i.AvailableBalance);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            public void GetDepositAddresses()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();

                var asset = "BTC".ToAsset(provider);

                var ctx = new WalletAddressAssetContext(asset, false, UserContext.Current);

                try
                {
                    var r = provider.GetAddressesForAssetAsync(ctx).Result;

                    Console.WriteLine("List of addresses: ");
                    foreach (var walletAddress in r)
                    {
                        Console.WriteLine($"{walletAddress.Asset}: {walletAddress.Address}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            public void GetOhlcData()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();

                var ohlcContext = new OhlcContext(new AssetPair("BTC", "USD"), TimeResolution.Minute, new TimeRange(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, TimeResolution.Minute), null);

                try
                {
                    var ohlc = AsyncContext.Run(() => provider.GetOhlcAsync(ohlcContext));

                    foreach (var data in ohlc)
                    {
                        Console.WriteLine($"{data.DateTimeUtc}: {data.High} {data.Low} {data.Open} {data.Close}");
                    }

                    Console.WriteLine($"Entries count: {ohlc.Count}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void GetLatestPrice()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();

                var ctx = new PublicPricesContext("BTC".ToAssetRaw(), new List<Asset>()
                {
                    "USD".ToAsset(provider)
                });

                try
                {
                    var c = AsyncContext.Run(() => provider.GetLatestPricesAsync(ctx));

                    Console.WriteLine($"Base asset: {ctx.BaseAsset}\n");

                    foreach (Money price in c.Prices)
                    {
                        Console.WriteLine(price.Display);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void GetAssetPairs()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();
                var ctx = new NetworkProviderContext();

                try
                {
                    var pairs = AsyncContext.Run(() => provider.GetAssetPairs(ctx));

                    foreach (var pair in pairs)
                    {
                        Console.WriteLine($"{pair}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void GetBalances()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();
                var ctx = new NetworkProviderPrivateContext(UserContext.Current);

                try
                {
                    var balances = AsyncContext.Run(() => provider.GetBalancesAsync(ctx));

                    foreach (var balance in balances)
                    {
                        Console.WriteLine($"{balance.Asset} : {balance.Balance}, {balance.Available}, {balance.Reserved}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }

            public void GetAllDepositAddresses()
            {
                var provider = Networks.I.Providers.OfType<BitMexProvider>().FirstProvider();
                
                var ctx = new WalletAddressContext(false, UserContext.Current);

                try
                {
                    var addresses = AsyncContext.Run(() => provider.GetAddressesAsync(ctx));

                    foreach (var address in addresses)
                    {
                        Console.WriteLine($"{address.Asset} : {address.Address}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }
    }
}