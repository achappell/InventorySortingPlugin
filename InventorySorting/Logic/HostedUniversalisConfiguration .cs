using CriticalCommonLib.MarketBoard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySorting.Logic
{
    public class HostedUniversalisConfiguration : IHostedUniversalisConfiguration
    {
        public int SaleHistoryLimit { get; set; }
    }
}
