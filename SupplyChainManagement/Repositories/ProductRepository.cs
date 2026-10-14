using Npgsql;
using Dapper;
using SupplyChainManagement.Models;
using SupplyChainManagement.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ProductRepository : IProductRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly Func<NpgsqlTransaction?> _getTransaction;

    public ProductRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> getTransaction)
    {
        _connection = connection;
        _getTransaction = getTransaction;
    }

    public async Task<IEnumerable<ActiveProductDto>> GetActiveProductsAsync()
    {
        const string sql = "SELECT * FROM v_active_products ORDER BY product_name;";
        return await _connection.QueryAsync<ActiveProductDto>(sql, transaction: _getTransaction());
    }

    public async Task<decimal> GetAvailableStockAsync(int productId)
    {
        const string sql = "SELECT fn_get_product_available_stock(@ProductId);";
        return await _connection.ExecuteScalarAsync<decimal>(sql, new { ProductId = productId }, transaction: _getTransaction());
    }
}