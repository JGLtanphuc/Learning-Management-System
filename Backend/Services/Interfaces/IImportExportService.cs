using Backend.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace Backend.Services.Interfaces
{
    public interface IImportExportService
    {
        Task<ImportResultDto> ImportStudentsAsync(IFormFile? file);
        Task<ImportResultDto> ImportClassesAsync(IFormFile? file);
        Task<ImportResultDto> ImportAttendancesAsync(IFormFile? file);
        Task<ImportResultDto> ImportAllocationsAsync(IFormFile? file);
        Task<ImportResultDto> ImportMockProjectsAsync(IFormFile? file);
        Task<ImportResultDto> ImportUsersAsync(IFormFile? file);
        Task<ImportResultDto> ImportBooksAsync(IFormFile? file);
        Task<ImportResultDto> ImportStudentScoresAsync(IFormFile? file);
        Task<ImportResultDto> ImportRolesAsync(IFormFile? file);
        Task<ImportResultDto> ImportPermissionsAsync(IFormFile? file);
        Task<ImportResultDto> ImportModuleGroupsAsync(IFormFile? file);
        Task<ImportResultDto> ImportModulesAsync(IFormFile? file);
        Task<ImportResultDto> ImportInterviewTopicsAsync(IFormFile? file);
        Task<ImportResultDto> ImportInterviewQuestionsAsync(IFormFile? file);
    }
}

