using CEA_RPL.Application.Interfaces;

namespace CEA_RPL.Infrastructure.Services;

public class LocalFileService : IFileService
{
    private readonly string _uploadDirectory;

    public LocalFileService()
    {
        _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(_uploadDirectory, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        // Return relative path for web access
        return $"/uploads/{uniqueFileName}";
    }
}
