using Dapper;
using Npgsql;
using SupplyChainManagement.Models;
using SupplyChainManagement.Repositories;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
Console.OutputEncoding = System.Text.Encoding.UTF8;

const string connectionString = "Host=localhost;Port=5432;Database=LAB1_DB;Username=postgres;Password=1234";

await EnsureSeedDataAsync(connectionString);

await using var uow = new UnitOfWork(connectionString);

bool exit = false;
while (!exit)
{
    Console.Clear();
    Console.WriteLine("      СИСТЕМА УПРАВЛІННЯ ЛАНЦЮГАМИ ПОСТАЧАННЯ       ");
    Console.WriteLine("1. Переглянути активні товари (View: v_active_products)");
    Console.WriteLine("2. Переглянути залишки на складах (View: v_warehouse_stock_summary)");
    Console.WriteLine("3. Переглянути замовлення на закупівлю (View: v_purchase_order_details)");
    Console.WriteLine("4. Перевірити доступний залишок товару (Function: fn_get_product_available_stock)");
    Console.WriteLine("5. Оформити нове замовлення постачальнику (Unit of Work + Stored Procedures)");
    Console.WriteLine("6. Оприбуткувати партію товару на склад (Procedure: sp_receive_stock_batch)");
    Console.WriteLine("0. Вихід");
    Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
    Console.Write("Оберіть пункт меню: ");

    string? choice = Console.ReadLine();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1":
                await ShowActiveProductsAsync(uow);
                break;
            case "2":
                await ShowWarehouseStockAsync(uow);
                break;
            case "3":
                await ShowPurchaseOrdersAsync(uow);
                break;
            case "4":
                await CheckProductStockAsync(uow);
                break;
            case "5":
                await CreatePurchaseOrderInteractiveAsync(uow);
                break;
            case "6":
                await ReceiveStockBatchInteractiveAsync(uow);
                break;
            case "0":
                exit = true;
                Console.WriteLine("Роботу завершено");
                continue;
            default:
                Console.WriteLine("Неправильне занчення. Спробуйте ще раз");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Помилка: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine("\nНатисніть будь-яку клавішу для повернення в меню...");
    Console.ReadKey();
}


static async Task ShowActiveProductsAsync(UnitOfWork uow)
{
    Console.WriteLine("--- КАТАЛОГ АКТИВНИХ ТОВАРІВ ---");
    var products = await uow.Products.GetActiveProductsAsync();
    foreach (var p in products)
    {
        Console.WriteLine($"SKU: {p.Sku,-15} | Назва: {p.ProductName,-25} | Категорія: {p.CategoryName,-20} | Од.: {p.UomCode}");
    }
}

static async Task ShowWarehouseStockAsync(UnitOfWork uow)
{
    Console.WriteLine("--- ПОТОЧНІ ЗАЛИШКИ НА СКЛАДАХ ---");
    var stocks = await uow.Inventory.GetWarehouseStockSummaryAsync();
    foreach (var s in stocks)
    {
        Console.WriteLine($"Склад: {s.WarehouseName} | Товар: {s.ProductName} | Фізично: {s.TotalPhysicalQuantity} | В резерві: {s.TotalReservedQuantity} | Доступно: {s.AvailableQuantity}");
    }
}

static async Task ShowPurchaseOrdersAsync(UnitOfWork uow)
{
    Console.WriteLine("--- ЖУРНАЛ ЗАМОВЛЕНЬ НА ЗАКУПІВЛЮ ---");
    var orders = await uow.PurchaseOrders.GetOrderDetailsAsync();
    foreach (var o in orders)
    {
        Console.WriteLine($"Номер: {o.OrderNumber,-20} | Постачальник: {o.SupplierName,-20} | Сума: {o.TotalAmount,10:F2} грн | Рядків: {o.ItemsCount} | Статус: {o.Status}");
    }
}

static async Task CheckProductStockAsync(UnitOfWork uow)
{
    var product = await SelectProductAsync(uow);
    if (product != null)
    {
        decimal available = await uow.Products.GetAvailableStockAsync(product.ProductId);
        Console.WriteLine($"Вільний залишок для товару {product.Sku} ({product.ProductName}): {available} шт.");
    }
}

static async Task CreatePurchaseOrderInteractiveAsync(UnitOfWork uow)
{
    Console.WriteLine("--- СТВОРЕННЯ ЗАМОВЛЕННЯ (UNIT OF WORK) ---");
    var supplier = await SelectSupplierAsync(uow);
    if (supplier == null) return;

    var product = await SelectProductAsync(uow);
    if (product == null) return;

    Console.Write("Введіть кількість: ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal qty) || qty <= 0)
    {
        Console.WriteLine("Некоректна кількість.");
        return;
    }

    Console.Write("Введіть ціну за одиницю (грн): ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal price) || price <= 0)
    {
        Console.WriteLine("Некоректна ціна.");
        return;
    }

    string orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

    await uow.BeginTransactionAsync();
    try
    {
        int newOrderId = await uow.PurchaseOrders.CreateOrderAsync(
            orderNumber: orderNumber,
            supplierId: supplier.Id,
            contractId: null,
            userId: 1
        );
        Console.WriteLine($"[1/2] Створено замовлення #{orderNumber}");

        await uow.PurchaseOrders.AddOrderItemAsync(
            orderId: newOrderId,
            productId: product.ProductId,
            quantity: qty,
            unitPrice: price,
            userId: 1
        );
        Console.WriteLine($"[2/2] Додано позицію: {qty} шт. по {price:F2} грн");

        await uow.CommitAsync();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("УСПІХ: Транзакцію Unit of Work зафіксовано (COMMIT). Замовлення оформлено!");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        await uow.RollbackAsync();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"ПОМИЛКА: Виконано відкат транзакції (ROLLBACK). Причина: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task ReceiveStockBatchInteractiveAsync(UnitOfWork uow)
{
    Console.WriteLine("--- ОПРИБУТКУВАННЯ ПАРТІЇ ТОВАРУ НА СКЛАД ---");
    var product = await SelectProductAsync(uow);
    if (product == null) return;

    var zone = await SelectOptionAsync(
        "складську зону",
        await uow.Inventory.GetWarehouseZonesAsync());
    if (zone == null) return;

    Console.Write("Номер партії (наприклад, BATCH-99): ");
    string? batchNumber = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(batchNumber)) batchNumber = $"BATCH-{DateTime.UtcNow:mmss}";

    Console.Write("Кількість для зарахування: ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal qty) || qty <= 0)
    {
        Console.WriteLine("Некоректна кількість.");
        return;
    }

    await uow.Inventory.ReceiveStockBatchAsync(
        productId: product.ProductId,
        zoneId: zone.Id,
        batchNumber: batchNumber,
        quantity: qty,
        prodDate: DateTime.UtcNow.Date,
        expDate: DateTime.UtcNow.Date.AddYears(2)
    );

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"УСПІХ: Партію {batchNumber} у кількості {qty} шт. успішно зараховано на склад!");
    Console.ResetColor();
}

static async Task<ActiveProductDto?> SelectProductAsync(UnitOfWork uow)
{
    var products = (await uow.Products.GetActiveProductsAsync()).ToList();
    if (products.Count == 0)
    {
        Console.WriteLine("Активних товарів не знайдено.");
        return null;
    }

    Console.WriteLine("Доступні товари:");
    foreach (var product in products)
    {
        Console.WriteLine($"- {product.Sku}: {product.ProductName}");
    }

    Console.Write("Введіть SKU товару: ");
    string? sku = Console.ReadLine();
    var selected = products.FirstOrDefault(product =>
        string.Equals(product.Sku, sku?.Trim(), StringComparison.OrdinalIgnoreCase));

    if (selected == null)
        Console.WriteLine("Товар із таким SKU не знайдено.");

    return selected;
}

static async Task<SelectionOptionDto?> SelectSupplierAsync(UnitOfWork uow)
{
    return await SelectOptionAsync(
        "постачальника",
        await uow.PurchaseOrders.GetSuppliersAsync());
}

static Task<SelectionOptionDto?> SelectOptionAsync(
    string entityName,
    IEnumerable<SelectionOptionDto> options)
{
    var availableOptions = options.ToList();
    if (availableOptions.Count == 0)
    {
        Console.WriteLine($"Доступних варіантів для вибору {entityName} не знайдено.");
        return Task.FromResult<SelectionOptionDto?>(null);
    }

    Console.WriteLine($"Доступні варіанти ({entityName}):");
    for (int index = 0; index < availableOptions.Count; index++)
    {
        Console.WriteLine($"{index + 1}. {availableOptions[index].DisplayName}");
    }

    Console.Write("Оберіть номер зі списку: ");
    if (!int.TryParse(Console.ReadLine(), out int selectedNumber) ||
        selectedNumber < 1 || selectedNumber > availableOptions.Count)
    {
        Console.WriteLine("Некоректний вибір.");
        return Task.FromResult<SelectionOptionDto?>(null);
    }

    return Task.FromResult<SelectionOptionDto?>(availableOptions[selectedNumber - 1]);
}

static async Task EnsureSeedDataAsync(string connStr)
{
    await using var seedConn = new NpgsqlConnection(connStr);
    await seedConn.OpenAsync();
    await seedConn.ExecuteAsync(@"
        INSERT INTO users (id, username, email, password_hash)
        VALUES (1, 'logistic_admin', 'admin@scm.ua', 'hash123')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO suppliers (id, name, tax_code)
        VALUES (1, 'ТОВ «ПромПостач»', 'TAX-12345678')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO product_categories (id, name)
        VALUES (1, 'Електроніка та мікросхеми')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO units_of_measure (id, code, name)
        VALUES (1, 'PCS', 'Штуки')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO products (id, category_id, uom_id, sku, name, min_stock_level, updated_by)
        VALUES (1, 1, 1, 'SKU-CHIP-01', 'Мікроконтролер ESP32', 20, 1)
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO warehouses (id, name, code)
        VALUES (1, 'Головний логістичний склад (Київ)', 'WH-KYIV-01')
        ON CONFLICT (id) DO NOTHING;

        INSERT INTO warehouse_zones (id, warehouse_id, zone_code)
        VALUES (1, 1, 'ZONE-A-RACK-01')
        ON CONFLICT (id) DO NOTHING;
    ");
}