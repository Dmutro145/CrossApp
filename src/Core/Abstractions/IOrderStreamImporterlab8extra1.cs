using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStreamImporter
{
    IAsyncEnumerable<Order> StreamAsync(string path, CancellationToken ct = default);
}