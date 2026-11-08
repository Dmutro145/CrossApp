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
    private sealed class ListProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    private sealed class SlowImporter : IOrderImporter
    {
        public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);   // чекає, поки токен не скасують
            return [];
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

    [Fact] // прогрес: 4 записи → 25, 50, 75, 100
    public async Task ImportAsync_ReportsProgress_EndingAt100()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var progress = new ListProgress();

        await service.ImportAsync(new FakeImporter(TestData.Items()), "x.json", progress: progress);

        Assert.Equal(new[] { 25, 50, 75, 100 }, progress.Values);
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
    //lab8:exta2
    [Fact] // доводить, що спрацював саме таймаут
    public async Task ImportWithTimeoutAsync_WhenTooSlow_ThrowsTimeoutException()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.ImportWithTimeoutAsync(new SlowImporter(), "x.json", TimeSpan.FromMilliseconds(50)));
    }

    [Fact] // скасував користувач, а не таймаут
    public async Task ImportWithTimeoutAsync_WhenUserCancels_ThrowsOperationCanceled_NotTimeout()
    {
        using var userCts = new CancellationTokenSource();
        userCts.CancelAfter(TimeSpan.FromMilliseconds(50));
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportWithTimeoutAsync(new SlowImporter(), "x.json",
                TimeSpan.FromSeconds(10), userCts.Token));
    }
}