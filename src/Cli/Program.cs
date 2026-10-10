using Core;
using System.Globalization;
using Core.Dto;
using Core.Import;

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