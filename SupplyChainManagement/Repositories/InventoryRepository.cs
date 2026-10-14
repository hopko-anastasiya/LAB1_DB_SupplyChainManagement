using Dapper;
using Npgsql;
using SupplyChainManagement.Models;
using SupplyChainManagement.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class InventoryRepository : IInventoryRepository
{
    private readonly NpgsqlConnection _connection;
    private readonly Func<NpgsqlTransaction?> _getTransaction;

    public InventoryRepository(NpgsqlConnection connection, Func<NpgsqlTransaction?> getTransaction)
    {
        _connection = connection;
        _getTransaction = getTransaction;
    }

    public async Task<IEnumerable<WarehouseStockSummaryDto>> GetWarehouseStockSummaryAsync()
    {
        const string sql = "SELECT * FROM v_warehouse_stock_summary;";
        return await _connection.QueryAsync<WarehouseStockSummaryDto>(sql, transaction: _getTransaction());
    }

    public async Task<IEnumerable<SelectionOptionDto>> GetWarehouseZonesAsync()
    {
        const string sql = """
            SELECT wz.id, w.name || ' / ' || wz.zone_code AS display_name
            FROM warehouse_zones wz
            INNER JOIN warehouses w ON w.id = wz.warehouse_id
            ORDER BY w.name, wz.zone_code;
            """;
        return await _connection.QueryAsync<SelectionOptionDto>(sql, transaction: _getTransaction());
    }

    public async Task ReceiveStockBatchAsync(int productId, int zoneId, string batchNumber, decimal quantity, DateTime? prodDate, DateTime? expDate)
    {
        var parameters = new DynamicParameters();
        parameters.Add("p_product_id", productId, DbType.Int32);
        parameters.Add("p_zone_id", zoneId, DbType.Int32);
        parameters.Add("p_batch_number", batchNumber, DbType.String);
        parameters.Add("p_quantity", quantity, DbType.Decimal);
        parameters.Add("p_prod_date", prodDate, DbType.Date);
        parameters.Add("p_exp_date", expDate, DbType.Date);

        await _connection.ExecuteAsync(
            "sp_receive_stock_batch",
            parameters,
            transaction: _getTransaction(),
            commandType: CommandType.StoredProcedure
        );
    }
}
