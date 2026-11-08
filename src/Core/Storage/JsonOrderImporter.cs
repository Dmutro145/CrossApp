using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonOrderImporter : IOrderImporter, IOrderStreamImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Order>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try { result.Add(Order.FromDto(dtos[i])); }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            { throw new DomainException($"Запис {i + 1}: {ex.Message}", ex); }
        }
        return result;
    }

    //lab8:extra1
    public async IAsyncEnumerable<Order> StreamAsync(string path,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);

        var index = 0;
        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            index++;
            Order order;
            try { order = Order.FromDto(dto!); }
            catch (Exception ex) when (ex is ArgumentException or DomainException)
            { throw new DomainException($"Запис {index}: {ex.Message}", ex); }
            yield return order;   // yield поза try/catch: у C# в try з catch його не можна
        }
    }
}