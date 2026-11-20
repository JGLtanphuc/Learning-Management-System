using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Interview;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Implementations
{
    public class InterviewTopicService : BaseService<InterviewTopic, InterviewTopicResponseDto, CreateInterviewTopicRequest, UpdateInterviewTopicRequest>, IInterviewTopicService
    {
        private readonly IInterviewTopicRepository _topicRepository;

        public InterviewTopicService(IInterviewTopicRepository topicRepository) : base(topicRepository)
        {
            _topicRepository = topicRepository;
        }

        protected override InterviewTopicResponseDto MapToResponseDto(InterviewTopic entity) => entity.ToResponseDto();
        protected override InterviewTopic MapToEntity(CreateInterviewTopicRequest request) => request.ToEntity();
        protected override void UpdateEntity(InterviewTopic entity, UpdateInterviewTopicRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<InterviewTopicResponseDto>> GetPageAsync(InterviewTopicFilterDto filter)
        {
            var query = _topicRepository.QueryAsNoTracking()
                .Include(t => t.Questions.Where(q => q.IsActive))
                .AsQueryable();

            if (filter.IsActive.HasValue)
            {
                query = query.Where(t => t.IsActive == filter.IsActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Id)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<InterviewTopicResponseDto>
            {
                Content = items.Select(t => t.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<InterviewTopicResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _topicRepository.QueryAsNoTracking()
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);
            return entity?.ToResponseDto();
        }

        public override async Task<InterviewTopicResponseDto> CreateAsync(CreateInterviewTopicRequest request)
        {
            var normalizedName = request.Name.Trim().ToLower();
            var exists = await _topicRepository.QueryAsNoTracking()
                .AnyAsync(t => t.Name.ToLower() == normalizedName);

            if (exists)
            {
                throw new ConflictException($"Mục phỏng vấn '{request.Name.Trim()}' đã tồn tại.");
            }

            return await base.CreateAsync(request);
        }

        public override async Task<InterviewTopicResponseDto?> UpdateAsync(long id, UpdateInterviewTopicRequest request)
        {
            var entity = await _topicRepository.Query()
                .Include(t => t.Questions)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return null;

            var normalizedName = request.Name.Trim().ToLower();
            var duplicate = await _topicRepository.QueryAsNoTracking()
                .AnyAsync(t => t.Id != id && t.Name.ToLower() == normalizedName);

            if (duplicate)
            {
                throw new ConflictException($"Mục phỏng vấn '{request.Name.Trim()}' đã tồn tại.");
            }

            UpdateEntity(entity, request);
            await _topicRepository.SaveChangesAsync();
            return entity.ToResponseDto();
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _topicRepository.QueryAsNoTracking()
                .Include(t => t.Questions)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var headers = new[] { "ID", "Tên chủ đề", "Mô tả", "Thứ tự hiển thị", "Số lượng câu hỏi", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(t => new object?[]
            {
                t.Id,
                t.Name,
                t.Description,
                t.DisplayOrder,
                t.Questions.Count(q => q.IsActive),
                t.IsActive,
                t.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Chủ đề phỏng vấn", "DANH SÁCH CHỦ ĐỀ PHỎNG VẤN (INTERVIEW TOPICS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _topicRepository.QueryAsNoTracking()
                .Include(t => t.Questions)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,Tên chủ đề,Mô tả,Thứ tự hiển thị,Số lượng câu hỏi,Trạng thái,Ngày tạo");

            foreach (var t in items)
            {
                var desc = (t.Description ?? "").Replace("\"", "\"\"");
                var name = t.Name.Replace("\"", "\"\"");
                var status = t.IsActive ? "Hoạt động" : "Tạm khóa";
                var qCount = t.Questions.Count(q => q.IsActive);
                sb.AppendLine($"\"{t.Id}\",\"{name}\",\"{desc}\",{t.DisplayOrder},{qCount},\"{status}\",\"{t.CreatedAt:dd/MM/yyyy HH:mm}\"");
            }

            return CsvExportHelper.ToUtf8BomBytes(sb.ToString());
        }
    }
}
