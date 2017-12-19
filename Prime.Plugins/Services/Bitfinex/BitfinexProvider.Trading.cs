﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prime.Common;

namespace Prime.Plugins.Services.Bitfinex
{
    /// <author email="yasko.alexander@gmail.com">Alexander Yasko</author>
    // https://bitfinex.readme.io/v1/reference
    public partial class BitfinexProvider : IOrderLimitProvider, IBalanceProvider, IWithdrawalPlacementProvider
    {
        public async Task<PlacedOrderLimitResponse> PlaceOrderLimitAsync(PlaceOrderLimitContext context)
        {
            var api = ApiProvider.GetApi(context);

            var body = new BitfinexSchema.NewOrderRequest.Descriptor
            {
                symbol = context.Pair.ToTicker(this),
                amount = context.Quantity.ToString(CultureInfo.InvariantCulture),
                price = context.Rate.ToDecimalValue().ToString(CultureInfo.InvariantCulture),
                side = context.IsSell ? "sell" : "buy"
            };

            var rRaw = await api.PlaceNewOrderAsync(body).ConfigureAwait(false);

            CheckBitfinexResponseErrors(rRaw);

            var r = rRaw.GetContent();

            return new PlacedOrderLimitResponse(r.order_id.ToString());
        }

        public Task<TradeOrderStatus> GetOrderStatusAsync(RemoteIdContext context)
        {
            throw new NotImplementedException();
        }

        public decimal MinimumTradeVolume => throw new NotImplementedException();

        public async Task<BalanceResults> GetBalancesAsync(NetworkProviderPrivateContext context)
        {
            var api = ApiProvider.GetApi(context);

            var body = new BitfinexSchema.WalletBalancesRequest.Descriptor();

            var rRaw = await api.GetWalletBalancesAsync(body).ConfigureAwait(false);

            CheckBitfinexResponseErrors(rRaw);

            var r = rRaw.GetContent();

            var balances = new BalanceResults(this);

            foreach (var rBalance in r)
            {
                var asset = rBalance.currency.ToAsset(this);
                balances.Add(asset, rBalance.available, rBalance.amount - rBalance.available);
            }

            return balances;
        }

        private static readonly Lazy<Dictionary<Asset, string>> WithdrawalAssetsToTypes = new Lazy<Dictionary<Asset,string>>(() => new Dictionary<Asset, string>()
        {
            // TODO: Bitfinex - clarify keys.
            { "BTC".ToAssetRaw(), "bitcoin" },
            { "LTC".ToAssetRaw(), "litecoin" },
            { "ETH".ToAssetRaw(), "ethereum" },
            { "ETC".ToAssetRaw(), "ethereumc" },
            { "mastercoin".ToAssetRaw(), "mastercoin" },
            { "ZEC".ToAssetRaw(), "zcash" },
            { "XMR".ToAssetRaw(), "monero" },
            { "wire".ToAssetRaw(), "wire" },
            { "DASH".ToAssetRaw(), "dash" },
            { "XRP".ToAssetRaw(), "ripple" },
            { "EOS".ToAssetRaw(), "eos" },
            { "NEO".ToAssetRaw(), "neo" },
            { "AVT".ToAssetRaw(), "aventus" },
            { "QTUM".ToAssetRaw(), "qtum" },
            { "eidoo".ToAssetRaw(), "eidoo" },
        });

        public bool IsFeeIncluded => true; // Confirmed.

        public Task<WithdrawalPlacementResult> PlaceWithdrawalAsync(WithdrawalPlacementContextExtended context)
        {
            throw new NotImplementedException();
        }

        public async Task<WithdrawalPlacementResult> PlaceWithdrawalAsync(WithdrawalPlacementContext context)
        {
            var api = ApiProvider.GetApi(context);

            var body = new BitfinexSchema.WithdrawalRequest.Descriptor();

            if(!WithdrawalAssetsToTypes.Value.TryGetValue(context.Amount.Asset, out var withdrawalType))
                throw new ApiResponseException("Withdrawal of specified asset is not supported", this);

            body.withdraw_type = withdrawalType;
            body.walletselected = "exchange"; // Can be trading, exchange, deposit.
            body.amount = context.Amount.ToDecimalValue().ToString(CultureInfo.InvariantCulture);
            body.address = context.Address.Address;
            body.payment_id = string.IsNullOrWhiteSpace(context.Description) ? null : context.Description;

            var rRaw = await api.WithdrawAsync(body).ConfigureAwait(false);

            CheckBitfinexResponseErrors(rRaw);

            var r = rRaw.GetContent().FirstOrDefault();
            if(r == null)
                throw new ApiResponseException("No result return after withdrawal operation", this);

            if(r.status.Equals("error", StringComparison.InvariantCultureIgnoreCase))
                throw new ApiResponseException(r.message, this);

            return new WithdrawalPlacementResult()
            {
                WithdrawalRemoteId = r.withdrawal_id.ToString()
            };
        }
    }
}