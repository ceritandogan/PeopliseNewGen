namespace Peoplise.SharedKernel.Media;

/// <summary>
/// Abstraction over wherever candidate-submitted media (video answers, uploaded
/// documents) actually lives — Azure Blob, S3, MinIO, or local disk in development.
/// Application command handlers depend on this, never on a specific storage SDK.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Stores the content and returns the URL to retrieve it by later.</summary>
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file at <paramref name="url"/>. Called for both KVKK consent
    /// withdrawal and retention-period expiry — must not throw if the file is already
    /// gone (deletion is idempotent).
    /// </summary>
    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}
