using Core.Domain;
using Core.Storage;

namespace Core.Tests;

/// <summary>
/// Лабораторна 9: формат файлу сховища і атомарний запис. Інтеграційні тести з диском —
/// кожен у власному тимчасовому каталозі, який видаляється в Dispose.
/// </summary>
public sealed class FileOrderStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"crossapp-store-{Guid.NewGuid():N}");
    private string DataFile => Path.Combine(_dir, "data", "orders.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Add_ThenNewStore_ReadsSameCollection()
    {
        var store = new FileOrderStore(DataFile);
        foreach (Order order in TestData.Orders())
            store.Add(order);

        var reopened = new FileOrderStore(DataFile);

        Assert.Equal(
            TestData.Orders().Select(o => (o.Id, o.Status, o.Total)).OrderBy(x => x.Id),
            reopened.List().Select(o => (o.Id, o.Status, o.Total)).OrderBy(x => x.Id));
    }

    [Fact]
    public void Write_KeepsCyrillicReadable_UsesCamelCase_AndLeavesNoTmpFile()
    {
        OrderJsonFile.Write(DataFile, TestData.Orders());

        string json = File.ReadAllText(DataFile);
        Assert.Contains("\"productName\": \"Ноутбук\"", json);
        Assert.DoesNotContain("\\u04", json);
        Assert.False(File.Exists(DataFile + ".tmp"));
    }

    [Fact]
    public void Read_FileOfPreviousLabs_WithPascalCaseNames_IsAccepted()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        File.WriteAllText(DataFile, """
            [{"Id":"O-1","CustomerId":"C-1","CreatedOn":"2026-11-01T10:00:00","Status":"Confirmed",
              "Lines":[{"ProductId":"P-1","ProductName":"Миша","UnitPrice":10,"Quantity":2}]}]
            """);

        Order order = Assert.Single(OrderJsonFile.Read(DataFile));

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal("Миша", order.Lines[0].ProductName);   // \uXXXX — лише запис, дані ті самі
    }

    [Fact]
    public void List_BrokenJson_ThrowsInvalidDataException_WithFileName()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        File.WriteAllText(DataFile, """[{"id":"O-1",""");

        var ex = Assert.Throws<InvalidDataException>(() => new FileOrderStore(DataFile).List());

        Assert.Contains("orders.json", ex.Message);
    }

    [Fact]
    public void Read_RecordBreakingInvariant_ReportsRecordNumber()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DataFile)!);
        File.WriteAllText(DataFile, """
            [{"id":"O-1","customerId":"C-1","createdOn":"2026-11-01T10:00:00","status":"Draft","lines":[]},
             {"id":"O-2","customerId":"C-1","createdOn":"2026-11-01T10:00:00","status":"Confirmed","lines":[]}]
            """);

        var ex = Assert.Throws<InvalidDataException>(() => OrderJsonFile.Read(DataFile));

        Assert.Contains("запис 2", ex.Message);
    }

    [Fact]
    public void Write_OverExistingFile_ReplacesContent()
    {
        OrderJsonFile.Write(DataFile, TestData.Orders());
        OrderJsonFile.Write(DataFile, TestData.Orders().Take(1));

        Assert.Single(OrderJsonFile.Read(DataFile));
    }
}
