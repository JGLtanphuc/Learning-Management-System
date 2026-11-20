using Microsoft.AspNetCore.Http;

namespace Backend.DTOs.Common
{
    public class FileUploadRequest
    {
        public IFormFile? File { get; set; }
    }
}

