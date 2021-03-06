﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Prime.Plugins.Services.Apiary
{
    internal class ApiarySchema
    {
        internal class TickerResponse : Dictionary<string, TickerEntryResponse>
        {

        }

        internal class AssetPairsResponse
        {
            public long server_time;
            public Dictionary<string, AssetPairEntryResponse> pairs;
        }

        internal class AssetPairEntryResponse
        {
            public int decimal_places;
            public int hidden;
            public int fee;
            public int amount_decimal_places;
            public decimal min_price;
            public decimal max_price;
            public decimal min_amount;
        }

        internal class TickerEntryResponse
        {
            public decimal high;
            public decimal low;
            public decimal last;
            public decimal buy;
            public decimal sell;
            public decimal avg;
            public decimal vol;
            public decimal vol_cur;
            public long updated;
        }
    }
}
