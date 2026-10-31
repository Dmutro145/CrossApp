using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order order);
    void Update(Order order);
    bool Remove(string id);
}