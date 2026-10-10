using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();

        try
        {
            string json = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];

            for (int i = 0; i < parsed.Count; i++)
            {
                ProductDto? p = parsed[i];
                string? problem = p switch
                {
                    null => "порожній елемент",
                    { Id: null or "" } => "відсутній id",
                    { Name: null or "" } => "відсутня назва",
                    { Price: < 0 } => $"від'ємна ціна {p.Price}",
                    _ => null
                };

                if (problem is null)
                    items.Add(p!);
                else
                    errors.Add($"елемент {i + 1}: {problem}");
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"некоректний JSON: {ex.Message}");
        }

        return new ImportResult<ProductDto>(items, errors);
    }
}