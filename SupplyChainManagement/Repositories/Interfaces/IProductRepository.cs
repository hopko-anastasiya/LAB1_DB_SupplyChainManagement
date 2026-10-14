using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SupplyChainManagement.Models;

namespace SupplyChainManagement.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<ActiveProductDto>> GetActiveProductsAsync();
    Task<decimal> GetAvailableStockAsync(int productId);
}