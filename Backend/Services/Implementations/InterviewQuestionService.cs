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
    public class InterviewQuestionService : BaseService<InterviewQuestion, InterviewQuestionResponseDto, CreateInterviewQuestionRequest, UpdateInterviewQuestionRequest>, IInterviewQuestionService
    {
        private readonly IInterviewQuestionRepository _questionRepository;
        private readonly IInterviewTopicRepository _topicRepository;

        public InterviewQuestionService(
            IInterviewQuestionRepository questionRepository,
            IInterviewTopicRepository topicRepository) : base(questionRepository)
        {
            _questionRepository = questionRepository;
            _topicRepository = topicRepository;
        }

        protected override InterviewQuestionResponseDto MapToResponseDto(InterviewQuestion entity) => entity.ToResponseDto();
        protected override InterviewQuestion MapToEntity(CreateInterviewQuestionRequest request) => request.ToEntity();
        protected override void UpdateEntity(InterviewQuestion entity, UpdateInterviewQuestionRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<InterviewQuestionResponseDto>> GetPageAsync(InterviewQuestionFilterDto filter)
        {
            var query = _questionRepository.QueryAsNoTracking()
                .Include(q => q.Topic)
                .AsQueryable();

            if (filter.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == filter.IsActive.Value);
            }

            if (filter.TopicId.HasValue)
            {
                query = query.Where(q => q.TopicId == filter.TopicId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Difficulty))
            {
                var diffs = filter.Difficulty.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(d => d.ToUpperInvariant())
                    .ToList();
                if (diffs.Count > 1)
                {
                    query = query.Where(q => diffs.Contains(q.Difficulty));
                }
                else if (diffs.Count == 1)
                {
                    query = query.Where(q => q.Difficulty == diffs[0]);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(q =>
                    q.QuestionText.ToLower().Contains(term) ||
                    (q.SampleAnswer != null && q.SampleAnswer.ToLower().Contains(term)) ||
                    q.Topic.Name.ToLower().Contains(term));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderBy(q => q.TopicId)
                .ThenBy(q => q.DisplayOrder)
                .ThenBy(q => q.Id)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<InterviewQuestionResponseDto>
            {
                Content = items.Select(q => q.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<InterviewQuestionResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _questionRepository.QueryAsNoTracking()
                .Include(q => q.Topic)
                .FirstOrDefaultAsync(q => q.Id == id);
            return entity?.ToResponseDto();
        }

        public override async Task<InterviewQuestionResponseDto> CreateAsync(CreateInterviewQuestionRequest request)
        {
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                throw new NotFoundException($"Không tìm thấy mục phỏng vấn với ID {request.TopicId}.");
            }

            var entity = request.ToEntity();
            await _questionRepository.AddAsync(entity);
            await _questionRepository.SaveChangesAsync();

            entity.Topic = topic;
            return entity.ToResponseDto();
        }

        public override async Task<InterviewQuestionResponseDto?> UpdateAsync(long id, UpdateInterviewQuestionRequest request)
        {
            var entity = await _questionRepository.Query()
                .Include(q => q.Topic)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (entity == null) return null;

            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                throw new NotFoundException($"Không tìm thấy mục phỏng vấn với ID {request.TopicId}.");
            }

            entity.UpdateEntity(request);
            await _questionRepository.SaveChangesAsync();

            entity.Topic = topic;
            return entity.ToResponseDto();
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var items = await _questionRepository.QueryAsNoTracking()
                .Include(q => q.Topic)
                .OrderBy(q => q.TopicId)
                .ThenBy(q => q.DisplayOrder)
                .ToListAsync();

            var headers = new[] { "ID", "Chủ đề", "Nội dung câu hỏi", "Độ khó", "Loại câu hỏi", "Gợi ý đáp án", "Từ khóa chấm điểm", "Trạng thái", "Ngày tạo" };
            var rows = items.Select(q => new object?[]
            {
                q.Id,
                q.Topic?.Name ?? "",
                q.QuestionText,
                q.Difficulty.ToString(),
                q.QuestionType.ToString(),
                q.SampleAnswer ?? "",
                q.Keywords ?? "",
                q.IsActive,
                q.CreatedAt
            });
            return ExcelExportHelper.CreateWorkbook("Ngân hàng câu hỏi", "NGÂN HÀNG CÂU HỎI PHỎNG VẤN (INTERVIEW QUESTIONS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var items = await _questionRepository.QueryAsNoTracking()
                .Include(q => q.Topic)
                .OrderBy(q => q.TopicId)
                .ThenBy(q => q.DisplayOrder)
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ID,Chủ đề,Nội dung câu hỏi,Độ khó,Loại câu hỏi,Gợi ý đáp án,Từ khóa chấm điểm,Trạng thái,Ngày tạo");

            foreach (var q in items)
            {
                var topicName = (q.Topic?.Name ?? "").Replace("\"", "\"\"");
                var text = q.QuestionText.Replace("\"", "\"\"");
                var sample = (q.SampleAnswer ?? "").Replace("\"", "\"\"");
                var keywords = (q.Keywords ?? "").Replace("\"", "\"\"");
                var status = q.IsActive ? "Hoạt động" : "Tạm khóa";
                sb.AppendLine($"\"{q.Id}\",\"{topicName}\",\"{text}\",\"{q.Difficulty}\",\"{q.QuestionType}\",\"{sample}\",\"{keywords}\",\"{status}\",\"{q.CreatedAt:dd/MM/yyyy HH:mm}\"");
            }

            return CsvExportHelper.ToUtf8BomBytes(sb.ToString());
        }
    }
}
