using Core;

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
