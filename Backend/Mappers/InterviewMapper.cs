using Backend.DTOs.Interview;
using Backend.Entities;

namespace Backend.Mappers
{
    public static class InterviewMapper
    {
        // --- Topic ---
        public static InterviewTopicResponseDto ToResponseDto(this InterviewTopic entity)
        {
            return new InterviewTopicResponseDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                DisplayOrder = entity.DisplayOrder,
                QuestionCount = entity.Questions?.Count ?? 0,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }

        public static InterviewTopic ToEntity(this CreateInterviewTopicRequest request)
        {
            return new InterviewTopic
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DisplayOrder = request.DisplayOrder ?? 0,
                IsActive = request.IsActive
            };
        }

        public static void UpdateEntity(this InterviewTopic entity, UpdateInterviewTopicRequest request)
        {
            entity.Name = request.Name.Trim();
            entity.Description = request.Description?.Trim();
            entity.DisplayOrder = request.DisplayOrder ?? 0;
            entity.IsActive = request.IsActive;
        }

        // --- Question ---
        public static InterviewQuestionResponseDto ToResponseDto(this InterviewQuestion entity)
        {
            return new InterviewQuestionResponseDto
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                TopicName = entity.Topic?.Name ?? string.Empty,
                QuestionText = entity.QuestionText,
                SampleAnswer = entity.SampleAnswer,
                Keywords = entity.Keywords,
                Difficulty = entity.Difficulty,
                QuestionType = entity.QuestionType,
                OptionsJson = entity.OptionsJson,
                CorrectOptionId = entity.CorrectOptionId,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy
            };
        }

        public static InterviewQuestion ToEntity(this CreateInterviewQuestionRequest request)
        {
            return new InterviewQuestion
            {
                TopicId = request.TopicId,
                QuestionText = request.QuestionText.Trim(),
                SampleAnswer = request.SampleAnswer?.Trim(),
                Keywords = request.Keywords?.Trim(),
                Difficulty = (request.Difficulty ?? "MEDIUM").ToUpperInvariant(),
                QuestionType = string.IsNullOrWhiteSpace(request.QuestionType) ? "WRITTEN" : request.QuestionType.ToUpperInvariant(),
                OptionsJson = request.OptionsJson,
                CorrectOptionId = request.CorrectOptionId,
                DisplayOrder = request.DisplayOrder ?? 0,
                IsActive = request.IsActive
            };
        }

        public static void UpdateEntity(this InterviewQuestion entity, UpdateInterviewQuestionRequest request)
        {
            entity.TopicId = request.TopicId;
            entity.QuestionText = request.QuestionText.Trim();
            entity.SampleAnswer = request.SampleAnswer?.Trim();
            entity.Keywords = request.Keywords?.Trim();
            entity.Difficulty = (request.Difficulty ?? "MEDIUM").ToUpperInvariant();
            entity.QuestionType = string.IsNullOrWhiteSpace(request.QuestionType) ? entity.QuestionType : request.QuestionType.ToUpperInvariant();
            entity.OptionsJson = request.OptionsJson;
            entity.CorrectOptionId = request.CorrectOptionId;
            entity.DisplayOrder = request.DisplayOrder ?? 0;
            entity.IsActive = request.IsActive;
        }

        // --- Assignment ---
        public static InterviewAssignmentResponseDto ToResponseDto(this InterviewAssignment entity)
        {
            var submittedSessions = entity.Sessions?.Where(s => s.Status != "IN_PROGRESS").ToList() ?? new();
            return new InterviewAssignmentResponseDto
            {
                Id = entity.Id,
                TopicId = entity.TopicId,
                TopicName = entity.Topic?.Name ?? string.Empty,
                ClassId = entity.ClassId,
                ClassCode = entity.Class?.ClassCode,
                ClassName = entity.Class?.ClassName,
                Title = entity.Title,
                Description = entity.Description,
                DurationMinutes = entity.DurationMinutes,
                TotalQuestions = entity.TotalQuestions,
                EasyCount = entity.EasyCount,
                MediumCount = entity.MediumCount,
                HardCount = entity.HardCount,
                PassingScore = entity.PassingScore,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                Status = entity.Status,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedBy = entity.UpdatedBy,
                TotalSubmissions = submittedSessions.Count,
                PendingReviewCount = submittedSessions.Count(s => s.Status == "SUBMITTED"),
                AverageScore = submittedSessions.Any(s => s.TotalScore.HasValue)
                    ? System.Math.Round(submittedSessions.Where(s => s.TotalScore.HasValue).Average(s => s.TotalScore!.Value), 1)
                    : null
            };
        }

        public static InterviewAssignment ToEntity(this CreateInterviewAssignmentRequest request)
        {
            return new InterviewAssignment
            {
                TopicId = request.TopicId,
                ClassId = request.ClassId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 45,
                TotalQuestions = request.TotalQuestions > 0 ? request.TotalQuestions : 10,
                EasyCount = request.EasyCount,
                MediumCount = request.MediumCount,
                HardCount = request.HardCount,
                PassingScore = request.PassingScore > 0 ? request.PassingScore : 7.0m,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "PUBLISHED" : request.Status.ToUpperInvariant(),
                IsActive = request.IsActive
            };
        }

        public static void UpdateEntity(this InterviewAssignment entity, UpdateInterviewAssignmentRequest request)
        {
            entity.TopicId = request.TopicId;
            entity.ClassId = request.ClassId;
            entity.Title = request.Title.Trim();
            entity.Description = request.Description?.Trim();
            entity.DurationMinutes = request.DurationMinutes > 0 ? request.DurationMinutes : 45;
            entity.TotalQuestions = request.TotalQuestions > 0 ? request.TotalQuestions : 10;
            entity.EasyCount = request.EasyCount;
            entity.MediumCount = request.MediumCount;
            entity.HardCount = request.HardCount;
            entity.PassingScore = request.PassingScore > 0 ? request.PassingScore : 7.0m;
            entity.StartTime = request.StartTime;
            entity.EndTime = request.EndTime;
            entity.Status = string.IsNullOrWhiteSpace(request.Status) ? entity.Status : request.Status.ToUpperInvariant();
            entity.IsActive = request.IsActive;
        }
    }
}
