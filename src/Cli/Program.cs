using Core;
using System.Globalization;
using Core.Dto;
using Core.Import;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Core.Abstractions;
//lab6
using Core.Events;
Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();

Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Кертичак Дмитро, група ФЕІ-37");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС                    : {report.OsDescription}");
Console.WriteLine($"Runtime               : {report.FrameworkDescription}");
Console.WriteLine($"Архітектура           : {report.ProcessArchitecture}");
Console.WriteLine($"RID (визначено)       : {report.DetectedRid}");
Console.WriteLine($"RID (від .NET)        : {report.ReportedRid}");
Console.WriteLine($"Каталог застосунку    : {report.BaseDirectory}");
Console.WriteLine($"Поточний каталог      : {Environment.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");

Console.WriteLine();
if (args.Contains("--domain"))
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Order order = Order.Create("O-001", "C-100");
    order.AddLine("P-001", "Цемент М400 25кг", 180.50m, 10);
    order.AddLine("P-002", "Пісок", 95m, 3);
    Console.WriteLine(order);
    order.Confirm();
    Console.WriteLine(order);
    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");
    Order draft = Order.Create("O-003", "C-200");

    TryDo("порожній Id замовлення", () => Order.Create(" ", "C-100"));
    TryDo("порожній Id клієнта", () => Order.Create("O-002", ""));
    TryDo("порожній productId", () => draft.AddLine(" ", "Цегла", 10m, 5));
    TryDo("від'ємна ціна", () => draft.AddLine("P-003", "Цегла", -10m, 5));
    TryDo("нульова кількість", () => draft.AddLine("P-003", "Цегла", 10m, 0));
    TryDo("від'ємна кількість", () => draft.AddLine("P-003", "Цегла", 10m, -2));
    TryDo("підтвердження порожнього замовлення", () => draft.Confirm());
    TryDo("додати рядок до підтвердженого", () => order.AddLine("P-009", "Щебінь", 50m, 1));
    TryDo("повторне підтвердження", () => order.Confirm());

    Console.WriteLine();
    Console.WriteLine("Після всіх відмов:");
    Console.WriteLine(order);
    Console.WriteLine(draft);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 3: ToDto / FromDto ===");
    OrderDto dto = order.ToDto();
    Order restored = Order.FromDto(dto);
    Console.WriteLine($"Збережено в DTO : {dto.Id}, рядків: {dto.Lines.Count}, підтверджено: {dto.IsConfirmed}");
    Console.WriteLine($"Відновлено      : {restored}");
    Console.WriteLine($"Збігається      : {order.ToString() == restored.ToString()}");

    var emptyConfirmed = new OrderDto("O-010", "C-1", true, []);
    var badQuantity = new OrderDto("O-011", "C-1", false,
        [new OrderLineDto("P-1", "Цегла", 10m, -3)]);
    var noId = new OrderDto(" ", "C-1", false, []);

    TryDo("DTO: підтверджене без рядків", () => Order.FromDto(emptyConfirmed));
    TryDo("DTO: від'ємна кількість у рядку", () => Order.FromDto(badQuantity));
    TryDo("DTO: порожній Id", () => Order.FromDto(noId));

    //Extra lab5.1
    TryDo("DTO: порожній Id", () => Order.FromDto(noId));

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 4: ImportResult -> сутності + відхилені ===");
    var import = new ImportResult<ProductDto>(
        [
            new ProductDto("P-1", "Цемент", 180.50m),
            new ProductDto("P-2", " ", 10m),
            new ProductDto("P-3", "Цегла", -5m),
            new ProductDto("P-4", "Пісок", 95m)
        ],
        []);

    DomainLoadResult<OrderLine> loaded = OrderLineImporter.FromProducts(import, 2);
    Console.WriteLine($"Коректних рядків: {loaded.Items.Count}, відхилено: {loaded.Rejected.Count}");
    foreach (OrderLine l in loaded.Items)
        Console.WriteLine($"  + {l}");
    foreach (string r in loaded.Rejected)
        Console.WriteLine($"  ! {r}");

    ////Extra lab4.2
    Console.WriteLine();
    Console.WriteLine("=== Сценарій 5: правило на кілька замовлень (сервіс) ===");
    var service = new OrderService(new InMemoryOrderStore());
    for (int i = 1; i <= 5; i++)
        service.CreateOrder($"O-50{i}", "C-500");
    Console.WriteLine($"Незавершених у C-500: {service.All().Count(o => o.Status == OrderStatus.Draft)}");

    TryDo("6-те незавершене замовлення клієнта", () => service.CreateOrder("O-506", "C-500"));

    service.CreateOrder("O-601", "C-600");
    Console.WriteLine("  інший клієнт: ОК, правило його не блокує");

    Order first = service.All()[0];
    service.AddLine(first.Id, "P-1", "Цемент", 100m, 1);
    service.Confirm(first.Id);
    service.CreateOrder("O-506", "C-500");
    Console.WriteLine("  після підтвердження одного замовлення: ОК, нове створено");
    Console.WriteLine($"Усього замовлень у сервісі: {service.All().Count}");

    //Extra lab4.3
    Console.WriteLine();
    Console.WriteLine("=== Сценарій 6: статуси та переходи ===");
    Order o6 = Order.Create("O-700", "C-700");
    Console.WriteLine($"Початковий статус: {o6.Status}");
    TryDo("Draft -> Confirmed без рядків", () => o6.Confirm());
    o6.AddLine("P-1", "Цемент", 100m, 1);
    o6.Confirm();
    Console.WriteLine($"Після Confirm: {o6.Status}");
    o6.Cancel();
    Console.WriteLine($"Після Cancel: {o6.Status}");
    TryDo("Cancelled -> Confirmed", () => o6.Confirm());
    TryDo("Cancelled -> Cancelled", () => o6.Cancel());
    TryDo("додати рядок до скасованого", () => o6.AddLine("P-2", "Пісок", 10m, 1));
    return 0;
}
//lab6
if (args.Contains("--events"))
{
    Console.WriteLine("=== Лаба 6: події ===");

    StoreSetup eventsSetup = StoreFactory.Create(args);
    var eventsService = new OrderService(eventsSetup.Store);
    Console.WriteLine($"Сховище: {eventsSetup.Name}");

    var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    File.WriteAllText(logPath, string.Empty); // новий лог на кожен запуск

    void OnOrderChanged(object? sender, OrderChangedEventArgs e)
    {
        var line = $"{e.At:HH:mm:ss} {e.Kind,-15} {e.OrderId} рядків {e.LinesCount,2} сума {e.Total,10:F2}";
        Console.WriteLine(line);
        try
        {
            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"  [лог недоступний: {ex.Message}]");
        }
    }

    // підписник 1: локальний метод (його можна зняти через -=)
    eventsService.Changed += OnOrderChanged;

    // підписник 2: лямбда із замиканням над змінною changes
    int changes = 0;
    eventsService.Changed += (_, _) =>
    {
        changes++;
        Console.WriteLine($"   лічильник: {changes}");
    };

    // успішні операції: по одній події на кожну
    string eventsId = $"O-{Guid.NewGuid().ToString("N")[..8]}";
    eventsService.CreateOrder(eventsId, "C-100");
    eventsService.AddLine(eventsId, "P-001", "Цемент М400 25кг", 185.50m, 10);
    eventsService.Confirm(eventsId);
    Console.WriteLine($"Подій отримано: {changes}");

    // сценарій відмови: обидва підписники ще активні, подій бути не має
    Console.WriteLine("=== Сценарій відмови ===");
    int before = changes;

    try
    {
        eventsService.Confirm("O-NOT-EXISTS");
        Console.WriteLine("  Неіснуючий id: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Помилка (немає замовлення): {ex.Message}");
    }

    try
    {
        eventsService.Confirm(eventsId);
        Console.WriteLine("  Повторне підтвердження: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Помилка (повторне підтвердження): {ex.Message}");
    }

    try
    {
        eventsService.AddLine(eventsId, "P-002", "Пісок", 95m, -3);
        Console.WriteLine("  AddLine: виняток НЕ спрацював!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Помилка (AddLine, кількість -3): {ex.Message}");
    }

    Console.WriteLine($"Подій під час відмов: {changes - before}"); // має бути 0

    // відписка: дані змінюються, консоль і файл мовчать
    eventsService.Changed -= OnOrderChanged;
    Console.WriteLine("--- відписка від OnOrderChanged ---");

    string silentId = $"O-{Guid.NewGuid().ToString("N")[..8]}";
    eventsService.CreateOrder(silentId, $"C-{Guid.NewGuid().ToString("N")[..4]}");

    Console.WriteLine($"Замовлень у сховищі: {eventsService.All().Count}");
    Console.WriteLine($"Подій отримано: {changes}"); // +1 лише від лямбди
    
        // Func замість події: тут потрібне значення у відповідь
    Console.WriteLine("=== Пошук через Func<Order, bool> ===");

    var confirmed = eventsService.Search(ord => ord.Status == OrderStatus.Confirmed);
    Console.WriteLine($"Підтверджених: {confirmed.Count}");

    decimal minTotal = 3000m;
    var big = eventsService.Search(ord => ord.Total >= minTotal);
    Console.WriteLine($"Сума ≥ {minTotal}:");
    foreach (Order ord in big)
        Console.WriteLine($"  {ord.Id,-12} {ord.CustomerId,-6} {ord.Status,-10} {ord.Total,10:F2}");

    Console.WriteLine($"Подій отримано: {changes}"); // лишилось 4: пошук нічого не змінює
    Console.WriteLine($"Лог: {logPath}");
    return 0;
}

//lab6 extra: INotificationSink
if (args.Contains("--sinks"))
{
    Console.WriteLine("=== Лаба 6 extra: INotificationSink ===");

    StoreSetup sinksSetup = StoreFactory.Create(args);
    
    Console.WriteLine($"Сховище: {sinksSetup.Name}");
    var sinksLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sinks.log");
    var fileSink = new FileSink(sinksLogPath);       // конструктор створює каталог
    File.WriteAllText(sinksLogPath, string.Empty);   // новий лог на кожен запуск

    // sink'и передає Cli через конструктор (ручний DI)
    var sinksService = new OrderService(sinksSetup.Store, new ConsoleSink(), fileSink);
    string sinkOrderId = $"O-{Guid.NewGuid().ToString("N")[..8]}";
    sinksService.CreateOrder(sinkOrderId, "C-100");
    sinksService.AddLine(sinkOrderId, "P-001", "Цемент М400 25кг", 185.50m, 10);
    sinksService.Confirm(sinkOrderId);

    Console.WriteLine("=== Відмова: sink'и мовчать ===");
    try
    {
        sinksService.Confirm(sinkOrderId);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Помилка: {ex.Message}");
    }

    Console.WriteLine($"Лог: {sinksLogPath}");
    return 0;
}

if (args.Length == 0 || args.Contains("--file") || args.Contains("--cache"))
{
        // ===== Composition root: вибір сховища винесено у StoreFactory =====
    StoreSetup setup = StoreFactory.Create(args);
    var orderService = new OrderService(setup.Store);
    // ===================================================================

    Console.WriteLine($"Сховище: {setup.Name}");
    if (setup.FilePath is not null) Console.WriteLine($"Файл: {setup.FilePath}");
    Console.WriteLine();

    string newId = $"O-{Guid.NewGuid().ToString("N")[..8]}";
    Order created = orderService.CreateOrder(newId, "C-100");
    orderService.AddLine(created.Id, "P-001", "Цемент М400 25кг", 185.50m, 10);
    orderService.AddLine(created.Id, "P-002", "Пісок будівельний", 95m, 3);
    orderService.Confirm(created.Id);

    Order? found = orderService.Find(created.Id);
    Console.WriteLine($"Знайдено за id: {found}");
    Console.WriteLine();

    Console.WriteLine($"Усього замовлень: {orderService.All().Count}");
    foreach (Order o in orderService.All())
          Console.WriteLine($" {o.Id,-12} {o.CustomerId,-6} {o.Status,-10} {o.Lines.Count,3} {o.Total,10:F2}");
    Console.WriteLine();
    

    //lab5.2Extra
        Console.WriteLine();
    Console.WriteLine("=== Пошук з Func<Order, bool> ===");

    var confirmed = orderService.Search(o => o.Status == OrderStatus.Confirmed);
    Console.WriteLine($"  Підтверджених: {confirmed.Count}");

    var c100 = orderService.Search(o => o.CustomerId == "C-100" && o.Total > 2000m);
    Console.WriteLine($"  Клієнт C-100, сума > 2000: {c100.Count}");

    decimal minTotal = 5000m;
    var big = orderService.Search(o => o.Total >= minTotal);
    Console.WriteLine($"  Сума ≥ {minTotal}: {string.Join(", ", big.Select(o => o.Id))}");
    Console.WriteLine("=== Сценарій відмови ===");


    // 1. Дубль id: унікальність перевіряє сховище
    try
    {
        orderService.CreateOrder(created.Id, "C-100");
        Console.WriteLine("  Дубль id: виняток НЕ спрацював!");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Помилка (дубль id): {ex.Message}");
    }

    // 2. Неіснуючий id: сервіс перетворює null зі сховища на виняток
    try
    {
        orderService.Confirm("O-NOT-EXISTS");
        Console.WriteLine("  Неіснуючий id: виняток НЕ спрацював!");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Помилка (немає замовлення): {ex.Message}");
    }

    // 3. Порушення правила сутності: повторне підтвердження
    try
    {
        orderService.Confirm(created.Id);
        Console.WriteLine("  Повторне підтвердження: виняток НЕ спрацював!");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"  Помилка (правило замовлення): {ex.Message}");
    }
    if (setup.Caching is not null)
        Console.WriteLine($"Кеш: влучань {setup.Caching.Hits}, промахів {setup.Caching.Misses}");
    return 0;
}

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

if (args.Contains("--mixed"))
{
    MixedImportResult mixed = MixedCsvImporterExtra.Load(path);

    Console.WriteLine($"Товарів: {mixed.Products.Count}, клієнтів: {mixed.Customers.Count}");
    foreach (ProductDto p in mixed.Products)
        Console.WriteLine($" Товар   {p.Id,-6} {p.Name,-30} {p.Price.ToString("F2", CultureInfo.InvariantCulture),8}");
    foreach (CustomerDto c in mixed.Customers)
        Console.WriteLine($" Клієнт  {c.Id,-6} {c.Name,-30} {c.Email ?? "—"}");

    if (mixed.Errors.Count > 0)
    {
        Console.WriteLine($"Пропущено рядків: {mixed.Errors.Count}");
        foreach (string e in mixed.Errors)
            Console.WriteLine($" ! {e}");
    }
    Console.WriteLine(mixed.Summary());
    return 0;
}

//lab6.2 extra: Action<string> для звіту про пропущені рядки
var skippedLog = Path.Combine(AppContext.BaseDirectory, "logs", "skipped.log");
Directory.CreateDirectory(Path.GetDirectoryName(skippedLog)!);
File.WriteAllText(skippedLog, string.Empty);
int skippedCount = 0;

void ReportSkipped(string message)
{
    skippedCount++;
    File.AppendAllText(skippedLog, message + Environment.NewLine);
}

ImportResult<ProductDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv"  => ProductCsvImporter.Load(path, ReportSkipped),
    ".json" => ProductJsonImporter.Load(path),
    _       => null
};

if (result is null)
{
    Console.WriteLine($"Непідтримуваний формат файлу: {Path.GetExtension(path)} (очікую .csv або .json)");
    return 2;
}

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($" {p.Id,-6} {p.Name,-30} {p.Price.ToString("F2", CultureInfo.InvariantCulture),8}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($" ! {e}");
}
Console.WriteLine(result.Summary());
if (skippedCount > 0)
    Console.WriteLine($"Звіт про пропущені рядки ({skippedCount}): {skippedLog}");
return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}


//Extra lab 5.3
public sealed record StoreSetup(
    IOrderStore Store,
    string Name,
    CachingOrderStore? Caching,
    string? FilePath);
 
 public static class StoreFactory
{
    public static StoreSetup Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "orders.json");

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        string name = store.GetType().Name;
        CachingOrderStore? caching = null;

        if (args.Contains("--cache"))
        {
            store = caching = new CachingOrderStore(store);
            name = $"{nameof(CachingOrderStore)} → {name}";
        }

        return new StoreSetup(store, name, caching, useFile ? dataPath : null);
    }
}

//lab6 extra
public sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs e) =>
        Console.WriteLine($"{e.At:HH:mm:ss} {e.Kind,-15} {e.OrderId} рядків {e.LinesCount,2} сума {e.Total,10:F2}");
}

public sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Notify(OrderChangedEventArgs e)
    {
        var line = $"{e.At:HH:mm:ss} {e.Kind,-15} {e.OrderId} рядків {e.LinesCount,2} сума {e.Total,10:F2}";
        try
        {
            File.AppendAllText(_path, line + Environment.NewLine);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"  [лог недоступний: {ex.Message}]");
        }
    }
}