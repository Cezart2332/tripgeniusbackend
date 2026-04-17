namespace TripGeniusBackend.Application.Interfaces;

public interface IFileUploader
{
    public Task<string> UploadFile(Stream file,string extension, string folder, int id);
    public void DeleteFolder(string folder, int id);
}