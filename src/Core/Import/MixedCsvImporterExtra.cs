using System.Globalization;
using Core.Dto;

namespace Core.Import;
public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors);
public static class MixedCsvImporterExtra
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductParsed p:
                    products.Add(p.Value);
                    break;
                case CustomerParsed c:
                    customers.Add(c.Value);
                    break;
                case LineFailed f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static LineOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", "", ..] or ["P", _, "", ..]
                => new LineFailed("товар: ID або назва порожні"),

            ["P", _, _, var price] when !decimal.TryParse(price, NumberStyles.AllowDecimalPoint,
                                                          CultureInfo.InvariantCulture, out decimal value) || value < 0
                => new LineFailed($"товар: ціна '{price}' не є невід'ємним числом"),

            ["P", var id, var name, var price]
                => new ProductParsed(new ProductDto(id, name,
                       decimal.Parse(price, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture))),

            ["C", "", ..] or ["C", _, "", ..]
                => new LineFailed("клієнт: ID або ім'я порожні"),

            ["C", var id, var name]
                => new CustomerParsed(new CustomerDto(id, name)),

            ["C", var id, var name, var email]
                => new CustomerParsed(new CustomerDto(id, name, email == "" ? null : email)),

            ["P", ..] or ["C", ..]
                => new LineFailed($"неправильна кількість колонок для '{parts[0]}': {parts.Length}"),

            _ => new LineFailed($"невідомий префікс '{parts[0]}'")
        };
    }

    private abstract record LineOutcome;
    private sealed record ProductParsed(ProductDto Value) : LineOutcome;
    private sealed record CustomerParsed(CustomerDto Value) : LineOutcome;
    private sealed record LineFailed(string Reason) : LineOutcome;
}
