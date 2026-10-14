using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Npgsql;
using SupplyChainManagement.Repositories.Interfaces;

namespace SupplyChainManagement.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly NpgsqlConnection _connection;
    private NpgsqlTransaction? _transaction;

    public NpgsqlTransaction? CurrentTransaction => _transaction;

    public IProductRepository Products { get; }
    public IPurchaseOrderRepository PurchaseOrders { get; }
    public IInventoryRepository Inventory { get; }

    public UnitOfWork(string connectionString)
    {
        _connection = new NpgsqlConnection(connectionString);
        _connection.Open();

        Products = new ProductRepository(_connection, () => CurrentTransaction);
        PurchaseOrders = new PurchaseOrderRepository(_connection, () => CurrentTransaction);
        Inventory = new InventoryRepository(_connection, () => CurrentTransaction);
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null) return;
        _transaction = await _connection.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction == null)
            throw new InvalidOperationException("Транзакцію не було розпочато.");

        await _transaction.CommitAsync();
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
        }
        await _connection.DisposeAsync();
    }
}