using Core;
using System.Globalization;
using Core.Dto;
using Core.Import;
using Core.Domain;

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

ImportResult<ProductDto>? result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv"  => ProductCsvImporter.Load(path),
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