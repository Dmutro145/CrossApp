//Extralab5.1
using Core.Dto;

namespace Core.Domain;

public sealed record DomainLoadResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Rejected);

public static class OrderLineImporter
{
    // Бере результат імпорту з лаби 3 і будує з нього рядки замовлення.
    public static DomainLoadResult<OrderLine> FromProducts(ImportResult<ProductDto> import, int quantity)
    {
        ArgumentNullException.ThrowIfNull(import);

        var lines = new List<OrderLine>();
        var rejected = new List<string>();

        foreach (ProductDto p in import.Items)
        {
            try
            {
                lines.Add(OrderLine.Create(p.Id, p.Name, p.Price, quantity));
            }
            catch (ArgumentException ex) // ловить і ArgumentOutOfRangeException
            {
                rejected.Add($"товар '{p.Id}': {ex.Message.ReplaceLineEndings(" ")}");
            }
        }

        return new DomainLoadResult<OrderLine>(lines, rejected);
    }
}

//Extra lab5.2 Delete
 