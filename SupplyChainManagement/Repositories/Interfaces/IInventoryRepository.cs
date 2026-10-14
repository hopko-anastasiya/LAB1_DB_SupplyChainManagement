using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupplyChainManagement.Models;

namespace SupplyChainManagement.Repositories.Interfaces;

public interface IInventoryRepository
{
    Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync();
    Task<IEnumerable<SelectionOptionDto>> GetWarehouseZonesAsync();
    Task ReceiveStockBatchAsync(int productId, int zoneId, string batchNumber, decimal quantity, DateTime? prodDate, DateTime? expDate);
}
