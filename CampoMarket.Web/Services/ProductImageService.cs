namespace CampoMarket.Web.Services;

public sealed class ProductImageService(IWebHostEnvironment environment) : IProductImageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

    public async Task<(bool Ok, string Message, string? Url)> SaveAsync(IFormFile image)
    {
        if (image.Length == 0)
        {
            return (false, "Selecciona una imagen valida.", null);
        }

        if (image.Length > 5 * 1024 * 1024)
        {
            return (false, "La imagen no puede superar 5 MB.", null);
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return (false, "Usa una imagen JPG, PNG, GIF o WEBP.", null);
        }

        if (!string.IsNullOrWhiteSpace(image.ContentType)
            && !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            && !image.ContentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "El archivo seleccionado debe ser una imagen.", null);
        }

        var uploadsRoot = Path.Combine(environment.WebRootPath, "uploads", "productos");
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsRoot, fileName);

        await using var stream = File.Create(filePath);
        await image.CopyToAsync(stream);

        return (true, "", $"/uploads/productos/{fileName}");
    }
}
