using Microsoft.AspNetCore.Http;
using TripGeniusBackend.Application.Interfaces;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class FileUploader : IFileUploader
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FileUploader(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<string> UploadFile(Stream file,string extension, string folder, int id)
    {   
  
                    
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        extension = extension.ToLower();

        if (!allowedExtensions.Contains(extension))
            throw new Exception("Invalid file type");
        
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, id.ToString());
            
        if (Directory.Exists(uploadFolder))
        {
            Directory.Delete(uploadFolder, true);    
        } 
        Directory.CreateDirectory(uploadFolder);

        var filename = $"{Guid.NewGuid()}{extension}";
        var path = Path.Combine(uploadFolder, filename);
        using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream); 
        var url = $"{_httpContextAccessor.HttpContext!.Request.Scheme}://{_httpContextAccessor.HttpContext!.Request.Host}/{folder}/{id}/{filename}";
        return url;
    }
    public void DeleteFolder(string folder, int id)
    {
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, id.ToString());
        if (Directory.Exists(uploadFolder))
        {
            Directory.Delete(uploadFolder, true);    
        } 
    }
}