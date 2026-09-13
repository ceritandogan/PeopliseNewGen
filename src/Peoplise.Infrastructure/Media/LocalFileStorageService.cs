using Microsoft.Extensions.Configuration;
using Peoplise.SharedKernel.Media;

namespace Peoplise.Infrastructure.Media;

/// <summary>
/// Writes to local disk under a configured root directory. A real, working
/// implementation — not a stub — but a development-only stand-in for the Azure Blob /
/// S3 / MinIO implementation the architecture decision actually calls for; nothing here
/// is durable across container restarts or shared across multiple instances, so this
/// must not be what runs in any shared/staging/production environment.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private const string UrlScheme = "local-storage://";

    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:LocalRootPath"] ?? Path.Combine(Path.GetTempPath(), "peoplise-storage");
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var storedName = $"{Guid.NewGuid():N}-{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(_rootPath, storedName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return $"{UrlScheme}{storedName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!url.StartsWith(UrlScheme, StringComparison.Ordinal))
            return Task.CompletedTask;

        var storedName = url[UrlScheme.Length..];
        var fullPath = Path.Combine(_rootPath, storedName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
