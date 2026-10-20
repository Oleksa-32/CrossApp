using Core.Events;

namespace Core.Abstractions;

/// <summary>
/// Одержувач сповіщень про зміни замовлень (додаткове завдання 1 тижня 6).
/// Альтернатива події: сервіс отримує одержувача через конструктор, як сховище
/// на тижні 5. Контракт у Core, реалізації — у Cli, бо Core не знає ні про
/// консоль, ні про файли.
/// </summary>
public interface INotificationSink
{
    void Notify(OrderChangedEventArgs change);
}
