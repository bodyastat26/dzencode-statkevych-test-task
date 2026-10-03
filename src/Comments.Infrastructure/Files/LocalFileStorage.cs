namespace Comments.Infrastructure.Files;

/// Stores files on disk with random names, so user-provided names never become paths.
public sealed class LocalFileStorage : IFileStorage
{
    public LocalFileStorage(string rootPath)
    {
        RootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct)
    {
        var name = $"{Guid.NewGuid():N}{extension}";
        await using var file = File.Create(PathFor(name));
        await content.CopyToAsync(file, ct);
        return name;
    }

    public Task<byte[]> ReadAsync(string storedFileName, CancellationToken ct) =>
        File.ReadAllBytesAsync(PathFor(storedFileName), ct);

    public void Delete(string storedFileName)
    {
        var path = PathFor(storedFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    // Path.GetFileName strips any "../" — protection against path traversal
    private string PathFor(string name) => Path.Combine(RootPath, Path.GetFileName(name));
}