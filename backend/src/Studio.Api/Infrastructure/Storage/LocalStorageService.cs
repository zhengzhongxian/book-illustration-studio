namespace Studio.Api.Infrastructure.Storage;

public interface ILocalStorageService
{
    Task<string> SaveImageBase64Async(string subfolder, string fileNameWithoutExt, string base64Data, string mimeType = "image/png");
    Task<string> SaveImageBytesAsync(string subfolder, string fileNameWithoutExt, byte[] bytes, string mimeType = "image/png");
    (Stream? Stream, string ContentType)? GetImageStream(string relativePath);
    Task<string?> ReadImageAsBase64Async(string? relativePath);
}

public class LocalStorageService : ILocalStorageService
{
    private readonly string _storageRoot;

    public LocalStorageService(IWebHostEnvironment env)
    {
        _storageRoot = Path.Combine(env.ContentRootPath, "storage");
        Directory.CreateDirectory(_storageRoot);
        Directory.CreateDirectory(Path.Combine(_storageRoot, "portraits"));
        Directory.CreateDirectory(Path.Combine(_storageRoot, "illustrations"));
    }

    public async Task<string> SaveImageBase64Async(string subfolder, string fileNameWithoutExt, string base64Data, string mimeType = "image/png")
    {
        var bytes = Convert.FromBase64String(base64Data);
        return await SaveImageBytesAsync(subfolder, fileNameWithoutExt, bytes, mimeType);
    }

    public async Task<string> SaveImageBytesAsync(string subfolder, string fileNameWithoutExt, byte[] bytes, string mimeType = "image/png")
    {
        var folder = Path.Combine(_storageRoot, subfolder);
        Directory.CreateDirectory(folder);

        var ext = mimeType.Contains("jpeg") || mimeType.Contains("jpg") ? ".jpg" : ".png";
        var fileName = $"{fileNameWithoutExt}{ext}";
        var fullPath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(fullPath, bytes);
        return Path.Combine(subfolder, fileName).Replace("\\", "/");
    }

    public (Stream? Stream, string ContentType)? GetImageStream(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var fullPath = Path.Combine(_storageRoot, relativePath);
        if (!File.Exists(fullPath)) return null;

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext == ".jpg" || ext == ".jpeg" ? "image/jpeg" : "image/png";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, contentType);
    }

    public async Task<string?> ReadImageAsBase64Async(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var fullPath = Path.Combine(_storageRoot, relativePath);
        if (!File.Exists(fullPath)) return null;

        var bytes = await File.ReadAllBytesAsync(fullPath);
        return Convert.ToBase64String(bytes);
    }
}
