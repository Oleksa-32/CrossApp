namespace Core.Storage;

/// <summary>
/// Обгортка лише для читання, що після кожного читання повідомляє, яку частку
/// джерела вже прочитано, у відсотках (додаткове завдання 3). Десеріалізатор
/// читає потік сам, тож це єдиний спосіб дізнатися хід розбору великого файлу.
/// Внутрішній потік не закривається: ним володіє той, хто його відкрив.
/// </summary>
internal sealed class ProgressReadStream(Stream inner, IProgress<int> progress) : Stream
{
    private const int Complete = 100;

    private readonly long _length = inner.Length;
    private int _lastPercent = -1;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => inner.Position;
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int read = await inner.ReadAsync(buffer, cancellationToken);
        Report();
        return read;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = inner.Read(buffer, offset, count);
        Report();
        return read;
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    // Повідомлення лише при зміні відсотка: великий файл — це тисячі читань.
    private void Report()
    {
        int percent = _length == 0 ? Complete : (int)(inner.Position * Complete / _length);
        if (percent == _lastPercent)
            return;

        _lastPercent = percent;
        progress.Report(percent);
    }
}
