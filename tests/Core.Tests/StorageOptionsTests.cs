using Core.Configuration;

namespace Core.Tests;

/// <summary>Лабораторна 9: побудова шляху до файлу даних із налаштувань.</summary>
public sealed class StorageOptionsTests : IDisposable
{
    private readonly string _baseDir = Path.Combine(Path.GetTempPath(), $"crossapp-opts-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_baseDir))
            Directory.Delete(_baseDir, recursive: true);
    }

    [Fact]
    public void ResolveFullPath_RelativeDataPath_IsCombinedWithBaseDir_AndDirectoryIsCreated()
    {
        var options = new StorageOptions { DataPath = "data", FileName = "orders.json" };

        string path = options.ResolveFullPath(_baseDir);

        Assert.Equal(Path.Combine(_baseDir, "data", "orders.json"), path);
        Assert.True(Directory.Exists(Path.Combine(_baseDir, "data")));
    }

    [Fact]   // відлік від переданого каталогу, а не від Environment.CurrentDirectory
    public void ResolveFullPath_IsAbsolute_AndUnderBaseDir()
    {
        string path = new StorageOptions().ResolveFullPath(_baseDir);

        Assert.True(Path.IsPathRooted(path));
        Assert.StartsWith(_baseDir, path);
    }

    [Fact]
    public void ResolveFullPath_AbsoluteDataPath_IsUsedAsIs()
    {
        string absolute = Path.Combine(_baseDir, "elsewhere");
        var options = new StorageOptions { DataPath = absolute, FileName = "x.json" };

        Assert.Equal(Path.Combine(absolute, "x.json"), options.ResolveFullPath("ignored-base"));
    }

    [Fact]
    public void ResolveFullPath_EmptyFileName_Throws()
    {
        var options = new StorageOptions { FileName = " " };

        Assert.Throws<InvalidOperationException>(() => options.ResolveFullPath(_baseDir));
    }

    [Fact]
    public void ResolveBaseDirectory_App_ReturnsGivenDirectory()
    {
        var options = new StorageOptions { Location = StorageLocation.App };

        Assert.Equal(_baseDir, options.ResolveBaseDirectory(_baseDir));
    }

    [Fact]
    public void Defaults_MatchDevelopmentProfile()
    {
        var options = new AppOptions();

        Assert.Equal("data", options.Storage.DataPath);
        Assert.Equal("orders.json", options.Storage.FileName);
        Assert.Equal(LogLevel.Information, options.Logging.MinimumLevel);
        Assert.Equal("UAH", options.Orders.Currency);
    }
}
