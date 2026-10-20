using Core.Abstractions;
using Core.Events;

namespace Cli;

/// <summary>Одержувач сповіщень, що дописує їх у файл (додаткове завдання 1).</summary>
internal sealed class FileSink(string path) : INotificationSink
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));

    public void Notify(OrderChangedEventArgs change) => ChangeLog.TryAppend(_path, ChangeLog.Format(change));

    public override string ToString() => $"{nameof(FileSink)} ({_path})";
}
