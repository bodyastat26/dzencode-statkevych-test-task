namespace Comments.Infrastructure.Files;

public interface IFileStorage
{
    string RootPath { get; }
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct);
    Task<byte[]> ReadAsync(string storedFileName, CancellationToken ct);
    void Delete(string storedFileName);
}