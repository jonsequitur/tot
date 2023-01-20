using System;
using System.IO;

namespace tot.Tests;

public class DisposableDirectory : IDisposable
{
    public static DisposableDirectory Create()
    {
        var tempDir = System.IO.Directory.CreateDirectory(
            Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()));
        return new DisposableDirectory(tempDir);
    }

    private DisposableDirectory(DirectoryInfo directory)
    {
        Directory = directory;
    }

    public DirectoryInfo Directory { get; }

    public void Dispose()
    {
        Directory.Delete(recursive: true);
    }
}