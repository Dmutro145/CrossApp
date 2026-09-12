using System.Runtime.InteropServices;
using System.Text.Json;
var info = new
{
    Student = "Кертичак Дмитро, група ФЕІ-37",
    OsDescription = RuntimeInformation.OSDescription,
    OsEnvironment = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotnetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (клієнт, товар, замовлення, рядок замовлення)"
};
if (args.Contains("--json"))
{
    var json = JsonSerializer.Serialize(info, new JsonSerializerOptions
    {
    WriteIndented = false,
    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
    });
    Console.WriteLine(json);
}
else
{
Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine("Студент: Кертичак Дмитро, група ФЕІ-37");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription)   : {RuntimeInformation.OSDescription}");
Console.WriteLine($"ОС (Environment)     : {Environment.OSVersion}");
Console.WriteLine($"Архітектура процесу  : {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR)    : {Environment.Version}");
Console.WriteLine($"Runtime              : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Каталог застосунку   : {AppContext.BaseDirectory}");
Console.WriteLine($"Поточний каталог     : {Environment.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");
}
