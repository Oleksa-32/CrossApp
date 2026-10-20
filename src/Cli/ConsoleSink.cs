using Core.Abstractions;
using Core.Events;

namespace Cli;

/// <summary>Одержувач сповіщень, що друкує їх у консоль (додаткове завдання 1).</summary>
internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs change) => Console.WriteLine(ChangeLog.Format(change));

    public override string ToString() => $"{nameof(ConsoleSink)} (консоль)";
}
