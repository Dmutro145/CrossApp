using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    [Fact] // інваріант: відмова і незмінний стан
    public void AddLine_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("O-1", "C-1");
        service.AddLine(order.Id, "P-1", "Кабель", 10m, 2);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => service.AddLine(order.Id, "P-2", "Гвинт", 5m, -5));

        Assert.Single(store.GetById(order.Id)!.Lines);
    }

    [Fact] // бізнес-правило: DomainException, стан не змінюється
    public void Confirm_Twice_ThrowsDomainException()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("O-1", "C-1");
        service.AddLine(order.Id, "P-1", "Кабель", 10m, 2);
        service.Confirm(order.Id);

        var ex = Assert.Throws<DomainException>(() => service.Confirm(order.Id));
        Assert.Contains("уже підтверджено", ex.Message);
        Assert.Equal(OrderStatus.Confirmed, store.GetById(order.Id)!.Status);
    }

    [Fact] // подія як спосіб перевірки
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.CreateOrder("O-2", "C-1");

        Assert.Single(events);
        Assert.Equal(OrderChangeKind.OrderCreated, events[0].Kind);
    }

    [Fact] // при відмові події немає
    public void Confirm_UnknownOrder_Throws_AndRaisesNoEvent()
    {
        var service = new OrderService(new InMemoryOrderStore());
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<DomainException>(() => service.Confirm("NO-SUCH"));
        Assert.Empty(events);
    }
    [Fact] // сума замовлення після додавання рядків
    public void AddLine_UpdatesOrderTotal()
    {
        var store = new InMemoryOrderStore();
        var service = new OrderService(store);
        var order = service.CreateOrder("O-3", "C-1");

        service.AddLine(order.Id, "P-1", "Цемент", 100m, 2);
        service.AddLine(order.Id, "P-2", "Пісок", 50m, 3);

        Assert.Equal(350m, store.GetById(order.Id)!.Total);
    }
}