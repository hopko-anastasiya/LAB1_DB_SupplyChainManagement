using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupplyChainManagement.Models;

namespace SupplyChainManagement.Repositories.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<IEnumerable<PurchaseOrderDetailDto>> GetOrderDetailsAsync();
    Task<IEnumerable<SelectionOptionDto>> GetSuppliersAsync();
    Task<int> CreateOrderAsync(string orderNumber, int supplierId, int? contractId, int userId);
    Task AddOrderItemAsync(int orderId, int productId, decimal quantity, decimal unitPrice, int userId);
}
