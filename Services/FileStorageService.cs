using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace NexusServiceMarketingSystem.Services;

/// <summary>Options that describe where the customer documents are stored.</summary>
public class FileStorageOptions
{
    /// <summary>Root folder of the document archive (outside wwwroot).</summary>
    public string RootPath { get; set; } = "Uploads";

    /// <summary>Maximum size of an uploaded file in bytes (2 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;

    /// <summary>Extensions that are accepted by the upload form.</summary>
    public string[] AllowedExtensions { get; set; } = { ".pdf", ".jpg", ".jpeg", ".png" };
}

/// <summary>Outcome of an upload attempt.</summary>
public class FileUploadResult
{
    /// <summary>True when the file was stored successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Error message shown to the user when the upload failed.</summary>
    public string? Error { get; init; }

    /// <summary>Relative path of the stored file.</summary>
    public string? RelativePath { get; init; }

    /// <summary>Randomly generated file name.</summary>
    public string? StoredFileName { get; init; }

    /// <summary>Size of the stored file in bytes.</summary>
    public long FileSizeBytes { get; init; }

    /// <summary>MIME type of the stored file.</summary>
    public string? ContentType { get; init; }
}

/// <summary>
/// Stores the customer documents (address proof, application form, receipts).
/// The files are kept outside wwwroot in the folder structure Uploads/{Year}/{City}/{ReferenceId}/
/// and can only be served through an authorised controller action.
/// </summary>
public class FileStorageService
{
    /// <summary>Options describing the archive location and the accepted file types.</summary>
    private readonly FileStorageOptions _options;

    /// <summary>Host environment used to resolve the physical application root path.</summary>
    private readonly IWebHostEnvironment _environment;

    /// <summary>Creates the service with the injected options and host environment.</summary>
    public FileStorageService(IOptions<FileStorageOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    /// <summary>Root folder of the document archive.</summary>
    public string RootPath => Path.Combine(_environment.ContentRootPath, _options.RootPath);

    /// <summary>
    /// Validates and stores an uploaded file.
    /// </summary>
    /// <param name="file">File posted by the user.</param>
    /// <param name="year">Year folder, for example 2026.</param>
    /// <param name="cityName">City / area folder.</param>
    /// <param name="referenceId">Order id or account id used as the last folder.</param>
    public async Task<FileUploadResult> SaveAsync(IFormFile file, int year, string cityName, string referenceId)
    {
        if (file == null || file.Length == 0)
        {
            return new FileUploadResult { Success = false, Error = "No file was selected." };
        }

        // Rule 1 - maximum size of 2 MB.
        if (file.Length > _options.MaxFileSizeBytes)
        {
            return new FileUploadResult
            {
                Success = false,
                Error = $"The file is larger than {_options.MaxFileSizeBytes / (1024 * 1024)} MB."
            };
        }

        // Rule 2 - only PDF, JPG and PNG files are accepted.
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
        {
            return new FileUploadResult
            {
                Success = false,
                Error = "Only PDF, JPG and PNG documents are accepted."
            };
        }

        // Rule 3 - the content type has to match the extension.
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var mimeMatches = extension switch
        {
            ".pdf" => contentType.Contains("pdf") || contentType.Length == 0,
            ".jpg" or ".jpeg" => contentType.Contains("jpeg") || contentType.Contains("jpg") || contentType.Length == 0,
            ".png" => contentType.Contains("png") || contentType.Length == 0,
            _ => false
        };

        if (!mimeMatches)
        {
            return new FileUploadResult
            {
                Success = false,
                Error = "The content of the file does not match its extension."
            };
        }

        // Rule 4 - the stored file always gets a random name, the original name is only kept in the database.
        var safeCity = SanitizeFolderName(cityName);
        var safeReference = SanitizeFolderName(referenceId);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";

        var relativeFolder = Path.Combine(year.ToString(), safeCity, safeReference);
        var absoluteFolder = Path.Combine(RootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var absolutePath = Path.Combine(absoluteFolder, storedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        return new FileUploadResult
        {
            Success = true,
            RelativePath = Path.Combine(relativeFolder, storedFileName),
            StoredFileName = storedFileName,
            FileSizeBytes = file.Length,
            ContentType = file.ContentType
        };
    }

    /// <summary>Returns the physical path of a stored document or null when the file is missing.</summary>
    public string? ResolvePhysicalPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        // The path is combined with the archive root, therefore a crafted value cannot escape it.
        var combined = Path.GetFullPath(Path.Combine(RootPath, relativePath));
        var root = Path.GetFullPath(RootPath);

        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return File.Exists(combined) ? combined : null;
    }

    /// <summary>Removes every character that is not allowed inside a folder name.</summary>
    private static string SanitizeFolderName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "General";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "General" : cleaned;
    }
}
