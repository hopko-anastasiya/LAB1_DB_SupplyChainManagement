using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Dapper;
using Npgsql;
using SupplyChainManagement.Models;
using SupplyChainManagement.Repositories.Interfaces;

namespace SupplyChainManagement.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly Func<NpgsqlTransaction?> _getTransaction;

    public PurchaseOrderRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> getTransaction)
    {
        _connection = connection;
        _getTransaction = getTransaction;
    }

    public async Task<IEnumerable<PurchaseOrderDetailDto>> GetOrderDetailsAsync()
    {
        const string sql = "SELECT * FROM v_purchase_order_details ORDER BY order_date DESC;";
        return await _connection.QueryAsync<PurchaseOrderDetailDto>(sql, transaction: _getTransaction());
    }

    public async Task<IEnumerable<SelectionOptionDto>> GetSuppliersAsync()
    {
        const string sql = """
            SELECT id, name AS display_name
            FROM suppliers
            ORDER BY name;
            """;
        return await _connection.QueryAsync<SelectionOptionDto>(sql, transaction: _getTransaction());
    }

    public async Task<int> CreateOrderAsync(string orderNumber, int supplierId, int? contractId, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_order_number", orderNumber, DbType.String);
        parameters.Add("p_supplier_id", supplierId, DbType.Int32);
        parameters.Add("p_contract_id", contractId, DbType.Int32);
        parameters.Add("p_user_id", userId, DbType.Int32);
        parameters.Add("p_new_order_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await _connection.ExecuteAsync(
            "sp_create_purchase_order",
            parameters,
            transaction: _getTransaction(),
            commandType: CommandType.StoredProcedure
        );

        return parameters.Get<int>("p_new_order_id");
    }

    public async Task AddOrderItemAsync(int orderId, int productId, decimal quantity, decimal unitPrice, int userId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_order_id", orderId, DbType.Int32);
        parameters.Add("p_product_id", productId, DbType.Int32);
        parameters.Add("p_quantity", quantity, DbType.Decimal);
        parameters.Add("p_unit_price", unitPrice, DbType.Decimal);
        parameters.Add("p_user_id", userId, DbType.Int32);

        await _connection.ExecuteAsync(
            "sp_add_order_item",
            parameters,
            transaction: _getTransaction(),
            commandType: CommandType.StoredProcedure
        );
    }
}