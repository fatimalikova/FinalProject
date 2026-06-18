namespace AppointmentAPP.Services.Interfaces
{
    public interface IFileUploadService
    {
        Task<string> SaveToWebRootAsync(IFormFile file, string folderName);
        Task<string> SaveFileAsync(IFormFile file, string subfolder);
        void DeleteFile(string? relativePath);
    }
}
