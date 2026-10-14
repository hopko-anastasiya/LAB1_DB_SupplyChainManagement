using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupplyChainManagement.Models;

public class PurchaseOrderDetailDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContractNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public long ItemsCount { get; set; }
    public string? LastModifiedBy { get; set; }
}