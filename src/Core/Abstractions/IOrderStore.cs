using Core.Domain;
//Extra lab 6.1
using Core.Events; 
namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Order> List();
    Order? GetById(string id);
    void Add(Order order);
    void Update(Order order);
    bool Remove(string id);
}

//lab6 extra
public interface INotificationSink
{
    void Notify(OrderChangedEventArgs e);
}