namespace Core.Domain;
using Core.Dto;

//Extralab5.3
public enum OrderStatus
{
    Draft,
    Confirmed,
    Cancelled
}

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public bool IsConfirmed => Status == OrderStatus.Confirmed;
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(l => l.LineTotal);

    private Order(string id, string customerId)
    {
        Id = id;
        CustomerId = customerId;
    }

    public static Order Create(string id, string customerId)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор замовлення обов'язковий", nameof(id));

        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        return new Order(id.Trim(), customerId.Trim());
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(
                $"Замовлення {Id} має статус {Status}, рядки додавати не можна");

        // Створюємо рядок через його фабрику — усі перевірки кількості і ціни там
        var line = OrderLine.Create(productId, name, price, quantity);
        _lines.Add(line);
    }

    private void ChangeStatus(OrderStatus target)
    {
        bool allowed = (Status, target) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new DomainException(
                $"Замовлення {Id}: перехід {Status} -> {target} неможливий");

        Status = target;
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
            throw new DomainException($"Замовлення {Id} уже підтверджено");

        if (_lines.Count == 0)
            throw new DomainException($"Не можна підтвердити порожнє замовлення {Id}");

        ChangeStatus(OrderStatus.Confirmed);
    }

    public void Cancel() => ChangeStatus(OrderStatus.Cancelled);

    // ЗМІНЕНО: у DTO додано Status.ToString() останнім аргументом
    public OrderDto ToDto() =>
        new(Id, CustomerId, IsConfirmed, _lines.Select(l => l.ToDto()).ToList(), Status.ToString());

    // ЗМІНЕНО: відновлюємо статус (Draft / Confirmed / Cancelled), а не лише IsConfirmed
    public static Order FromDto(OrderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var order = Create(dto.Id, dto.CustomerId);

        foreach (OrderLineDto line in dto.Lines ?? [])
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        // Старі DTO без Status: статус виводимо з IsConfirmed.
        string status = dto.Status
            ?? (dto.IsConfirmed ? nameof(OrderStatus.Confirmed) : nameof(OrderStatus.Draft));

        if (!Enum.TryParse(status, ignoreCase: true, out OrderStatus target)
            || !Enum.IsDefined(target))
            throw new ArgumentException($"Невідомий статус замовлення '{status}'", nameof(dto));

        // Статус виставляємо в кінці: до підтвердженого чи скасованого рядки вже не додати.
        switch (target)
        {
            case OrderStatus.Confirmed: order.Confirm(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
        }

        return order;
    }

    public override string ToString() =>
        $"Order {Id} (клієнт {CustomerId}) — рядків: {_lines.Count}, сума: {Total:F2}, " +
        $"статус: {Status}";
}