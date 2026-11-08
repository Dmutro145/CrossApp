using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private sealed class FakeImporter(IReadOnlyList<Order> items) : IOrderImporter
    {
        public Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(items);
        }
    }

    [Fact] // скасування
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", cts.Token));
    }

    [Fact] // ідемпотентність і події, без диска
    public async Task ImportAsync_IsIdempotent_AndRaisesEventPerAddedOrder()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var importer = new FakeImporter(TestData.Items());

        var first = await service.ImportAsync(importer, "ignored.json");
        var second = await service.ImportAsync(importer, "ignored.json");

        Assert.Equal(4, first);
        Assert.Equal(0, second);
        Assert.Equal(4, events.Count);
    }

    [Fact] // єдиний тест із тимчасовим файлом
    public async Task LoadAsync_WithInvalidRecord_ThrowsDomainException_WithRecordNumber()
    {
        var path = Path.Combine(Path.GetTempPath(), $"import-{Guid.NewGuid():N}.json");
        const string json = """
            [
              { "id": "T-1", "customerId": "C-1", "isConfirmed": false,
                "lines": [ { "productId": "P-1", "name": "Цемент", "price": 100, "quantity": 2 } ] },
              { "id": "T-2", "customerId": "C-1", "isConfirmed": false,
                "lines": [ { "productId": "P-2", "name": "Пісок", "price": 95, "quantity": -3 } ] }
            ]
            """;
        try
        {
            await File.WriteAllTextAsync(path, json);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => new JsonOrderImporter().LoadAsync(path));

            Assert.Contains("Запис 2", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }
}