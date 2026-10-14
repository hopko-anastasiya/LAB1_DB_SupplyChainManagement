using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyChainManagement.Models;

public class WarehouseStockSummaryDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalPhysicalQuantity { get; set; }
    public decimal TotalReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
}
