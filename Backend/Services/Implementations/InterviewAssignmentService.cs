using Backend.Common;
using Backend.Common.Exceptions;
using Backend.DTOs.Common;
using Backend.DTOs.Interview;
using Backend.Entities;
using Backend.Mappers;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Services.Implementations
{
    public class InterviewAssignmentService : BaseService<InterviewAssignment, InterviewAssignmentResponseDto, CreateInterviewAssignmentRequest, UpdateInterviewAssignmentRequest>, IInterviewAssignmentService
    {
        private readonly IInterviewAssignmentRepository _assignmentRepository;
        private readonly IInterviewSessionRepository _sessionRepository;
        private readonly IInterviewAnswerRepository _answerRepository;
        private readonly IInterviewQuestionRepository _questionRepository;
        private readonly IInterviewTopicRepository _topicRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IClassRepository _classRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IInterviewAiEvaluator _aiEvaluator;
        private readonly ILogger<InterviewAssignmentService> _logger;

        public InterviewAssignmentService(
            IInterviewAssignmentRepository assignmentRepository,
            IInterviewSessionRepository sessionRepository,
            IInterviewAnswerRepository answerRepository,
            IInterviewQuestionRepository questionRepository,
            IInterviewTopicRepository topicRepository,
            IStudentRepository studentRepository,
            IClassRepository classRepository,
            ICurrentUserService currentUserService,
            IInterviewAiEvaluator aiEvaluator,
            ILogger<InterviewAssignmentService> logger) : base(assignmentRepository)
        {
            _assignmentRepository = assignmentRepository;
            _sessionRepository = sessionRepository;
            _answerRepository = answerRepository;
            _questionRepository = questionRepository;
            _topicRepository = topicRepository;
            _studentRepository = studentRepository;
            _classRepository = classRepository;
            _currentUserService = currentUserService;
            _aiEvaluator = aiEvaluator;
            _logger = logger;
        }

        protected override InterviewAssignmentResponseDto MapToResponseDto(InterviewAssignment entity) => entity.ToResponseDto();
        protected override InterviewAssignment MapToEntity(CreateInterviewAssignmentRequest request) => request.ToEntity();
        protected override void UpdateEntity(InterviewAssignment entity, UpdateInterviewAssignmentRequest request) => entity.UpdateEntity(request);

        public async Task<PageResponse<InterviewAssignmentResponseDto>> GetPageAsync(InterviewAssignmentFilterDto filter)
        {
            var query = _assignmentRepository.QueryAsNoTracking()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .Include(a => a.Sessions)
                .AsQueryable();

            // Phân quyền phạm vi dữ liệu (Data Scope Isolation)
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Class != null && a.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.ClassId == null || (a.Class != null && a.Class.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value))));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(a => a.IsActive == filter.IsActive.Value);
            }

            if (filter.TopicId.HasValue)
            {
                query = query.Where(a => a.TopicId == filter.TopicId.Value);
            }

            if (filter.ClassId.HasValue)
            {
                query = query.Where(a => a.ClassId == filter.ClassId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                var st = filter.Status.Trim().ToUpperInvariant();
                query = query.Where(a => a.Status == st);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(a =>
                    a.Title.ToLower().Contains(term) ||
                    (a.Description != null && a.Description.ToLower().Contains(term)) ||
                    a.Topic.Name.ToLower().Contains(term) ||
                    (a.Class != null && a.Class.ClassName.ToLower().Contains(term)));
            }

            var totalElements = await query.LongCountAsync();

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip(filter.Page * filter.Size)
                .Take(filter.Size)
                .ToListAsync();

            return new PageResponse<InterviewAssignmentResponseDto>
            {
                Content = items.Select(a => a.ToResponseDto()).ToList(),
                Page = filter.Page,
                Size = filter.Size,
                TotalElements = totalElements
            };
        }

        public override async Task<InterviewAssignmentResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _assignmentRepository.QueryAsNoTracking()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .Include(a => a.Sessions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null) return null;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (entity.Class == null || entity.Class.TeacherId != _currentUserService.UserId.Value)
                {
                    return null;
                }
            }

            return entity.ToResponseDto();
        }

        public override async Task<InterviewAssignmentResponseDto> CreateAsync(CreateInterviewAssignmentRequest request)
        {
            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                throw new NotFoundException($"Không tìm thấy chủ đề phỏng vấn với ID {request.TopicId}.");
            }

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (!request.ClassId.HasValue)
                {
                    throw new BadRequestException("Giảng viên bắt buộc phải chọn lớp học do mình phụ trách khi tạo đợt phỏng vấn.");
                }

                var cls = await _classRepository.GetByIdAsync(request.ClassId.Value);
                if (cls == null || cls.TeacherId != _currentUserService.UserId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn chỉ được tạo đợt phỏng vấn cho lớp học do bạn phụ trách.");
                }
            }

            var entity = request.ToEntity();
            entity.IsActive = true;
            await _assignmentRepository.AddAsync(entity);
            await _assignmentRepository.SaveChangesAsync();

            entity.Topic = topic;
            return entity.ToResponseDto();
        }

        public override async Task<InterviewAssignmentResponseDto?> UpdateAsync(long id, UpdateInterviewAssignmentRequest request)
        {
            var entity = await _assignmentRepository.Query()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .Include(a => a.Sessions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null) return null;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (entity.Class == null || entity.Class.TeacherId != _currentUserService.UserId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa đợt phỏng vấn của lớp khác.");
                }

                if (request.ClassId.HasValue && request.ClassId.Value != entity.ClassId)
                {
                    var newCls = await _classRepository.GetByIdAsync(request.ClassId.Value);
                    if (newCls == null || newCls.TeacherId != _currentUserService.UserId.Value)
                    {
                        throw new UnauthorizedAccessException("Bạn không thể chuyển đợt phỏng vấn sang lớp mà bạn không phụ trách.");
                    }
                }
            }

            var topic = await _topicRepository.GetByIdAsync(request.TopicId);
            if (topic == null)
            {
                throw new NotFoundException($"Không tìm thấy chủ đề phỏng vấn với ID {request.TopicId}.");
            }

            entity.UpdateEntity(request);
            await _assignmentRepository.SaveChangesAsync();

            entity.Topic = topic;
            return entity.ToResponseDto();
        }

        public override async Task<bool> DeleteAsync(long id)
        {
            var entity = await _assignmentRepository.Query()
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null) return false;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (entity.Class == null || entity.Class.TeacherId != _currentUserService.UserId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xóa đợt phỏng vấn của lớp khác.");
                }
            }

            _assignmentRepository.Delete(entity);
            await _assignmentRepository.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // STUDENT WORKFLOWS
        // ==========================================

        public async Task<List<InterviewAssignmentResponseDto>> GetMyAssignmentsAsync()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem danh sách bài kiểm tra phỏng vấn.");
            }

            // Find student's class
            var student = await _studentRepository.QueryAsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            long? classId = student?.ClassId;

            // Get published assignments for this class or open to all
            var assignments = await _assignmentRepository.QueryAsNoTracking()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .Include(a => a.Sessions.Where(s => s.UserId == userId.Value))
                .Where(a => a.IsActive && a.Status == "PUBLISHED" && (!a.ClassId.HasValue || a.ClassId == classId))
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var result = new List<InterviewAssignmentResponseDto>();
            foreach (var a in assignments)
            {
                var dto = a.ToResponseDto();
                var userSession = a.Sessions.OrderByDescending(s => s.StartedAt).FirstOrDefault();
                if (userSession != null)
                {
                    dto.MySessionStatus = userSession.Status;
                    dto.MySessionId = userSession.Id;
                    dto.MyTotalScore = userSession.Status == "PUBLISHED" ? userSession.TotalScore : null;
                }
                else
                {
                    dto.MySessionStatus = "NOT_STARTED";
                }
                result.Add(dto);
            }

            return result;
        }

        public async Task<InterviewSessionResponseDto> StartSessionAsync(long assignmentId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập để bắt đầu làm bài.");
            }

            var assignment = await _assignmentRepository.Query()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                throw new NotFoundException($"Không tìm thấy đợt phỏng vấn #{assignmentId}.");
            }

            if (!assignment.IsActive || assignment.Status != "PUBLISHED")
            {
                throw new BadRequestException("Đợt phỏng vấn này hiện không khả dụng hoặc đã đóng.");
            }

            if (assignment.EndTime.HasValue && assignment.EndTime.Value < DateTime.UtcNow)
            {
                throw new BadRequestException("Đợt phỏng vấn này đã quá hạn nộp bài.");
            }

            // Check if there is already an active session in progress
            var existingSession = await _sessionRepository.Query()
                .Include(s => s.Answers)
                    .ThenInclude(ans => ans.Question)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.UserId == userId.Value && s.Status == "IN_PROGRESS");

            if (existingSession != null)
            {
                int duration = assignment.DurationMinutes;
                var elapsedSecs = (int)(DateTime.UtcNow - existingSession.StartedAt).TotalSeconds;
                if (elapsedSecs >= duration * 60)
                {
                    return await SubmitSessionAsync(existingSession.Id);
                }

                // Resume existing session
                return BuildSessionResponseDto(existingSession, assignment, isScoreRevealed: false);
            }

            // Random draw questions by difficulty matrix
            var pool = await _questionRepository.QueryAsNoTracking()
                .Where(q => q.TopicId == assignment.TopicId && q.IsActive)
                .ToListAsync();

            if (pool.Count == 0)
            {
                throw new BadRequestException("Ngân hàng câu hỏi của chủ đề này hiện chưa có câu hỏi nào.");
            }

            var easyPool = pool.Where(q => string.Equals(q.Difficulty, "EASY", StringComparison.OrdinalIgnoreCase)).ToList();
            var medPool = pool.Where(q => string.Equals(q.Difficulty, "MEDIUM", StringComparison.OrdinalIgnoreCase)).ToList();
            var hardPool = pool.Where(q => string.Equals(q.Difficulty, "HARD", StringComparison.OrdinalIgnoreCase)).ToList();

            var selected = new List<InterviewQuestion>();

            // Pick Easy
            var pickedEasy = easyPool.OrderBy(_ => Random.Shared.Next()).Take(assignment.EasyCount).ToList();
            selected.AddRange(pickedEasy);

            // Pick Medium
            var pickedMed = medPool.OrderBy(_ => Random.Shared.Next()).Take(assignment.MediumCount).ToList();
            selected.AddRange(pickedMed);

            // Pick Hard
            var pickedHard = hardPool.OrderBy(_ => Random.Shared.Next()).Take(assignment.HardCount).ToList();
            selected.AddRange(pickedHard);

            // If shortfall due to bank size, fill from remaining questions
            var selectedIds = selected.Select(q => q.Id).ToHashSet();
            var remainingPool = pool.Where(q => !selectedIds.Contains(q.Id)).OrderBy(_ => Random.Shared.Next()).ToList();
            var shortfall = assignment.TotalQuestions - selected.Count;
            if (shortfall > 0 && remainingPool.Count > 0)
            {
                selected.AddRange(remainingPool.Take(shortfall));
            }

            // Shuffle final selected list for candidate
            selected = selected.OrderBy(_ => Random.Shared.Next()).ToList();

            // Create session
            var session = new InterviewSession
            {
                AssignmentId = assignmentId,
                UserId = userId.Value,
                StartedAt = DateTime.UtcNow,
                Status = "IN_PROGRESS"
            };
            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            // Create pre-allocated answer rows for session questions
            var answers = new List<InterviewAnswer>();
            foreach (var q in selected)
            {
                var ans = new InterviewAnswer
                {
                    SessionId = session.Id,
                    QuestionId = q.Id,
                    UserId = userId.Value,
                    AnswerText = string.Empty,
                    AnswerMode = "TEXT",
                    IsFlagged = false,
                    IsActive = true
                };
                await _answerRepository.AddAsync(ans);
                ans.Question = q;
                answers.Add(ans);
            }
            await _answerRepository.SaveChangesAsync();

            session.Answers = answers;
            session.Assignment = assignment;

            return BuildSessionResponseDto(session, assignment, isScoreRevealed: false);
        }

        public async Task<InterviewSessionResponseDto?> GetSessionAsync(long sessionId)
        {
            var session = await _sessionRepository.QueryAsNoTracking()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Topic)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Class)
                .Include(s => s.User)
                .Include(s => s.Answers)
                    .ThenInclude(ans => ans.Question)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null) return null;

            var currentUserId = _currentUserService.UserId;
            bool isStaff = _currentUserService.IsAdmin || _currentUserService.IsTeacher;
            if (!isStaff && session.UserId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem phiên làm bài này.");
            }

            if (_currentUserService.IsTeacher && currentUserId.HasValue)
            {
                if (session.Assignment?.Class == null || session.Assignment.Class.TeacherId != currentUserId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem phiên làm bài của lớp khác.");
                }
            }

            // AUTO-SUBMIT CHECK: If session is IN_PROGRESS but time has expired, auto-submit in backend
            if (session.Status == "IN_PROGRESS")
            {
                int duration = session.Assignment?.DurationMinutes ?? 45;
                var elapsedSecs = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;
                if (elapsedSecs >= duration * 60)
                {
                    return await SubmitSessionAsync(sessionId);
                }
            }

            bool isScoreRevealed = isStaff || session.Status == "PUBLISHED";
            return BuildSessionResponseDto(session, session.Assignment, isScoreRevealed);
        }

        public async Task<bool> SaveAnswerAsync(long sessionId, SaveSessionAnswerRequest request)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
            }

            var session = await _sessionRepository.Query()
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new NotFoundException($"Không tìm thấy phiên làm bài #{sessionId}.");
            }

            if (session.UserId != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("Bạn không thể chỉnh sửa bài làm của người khác.");
            }

            if (session.Status != "IN_PROGRESS")
            {
                throw new BadRequestException("Bài thi đã nộp hoặc đã kết thúc, không thể lưu thêm câu trả lời.");
            }

            var answer = await _answerRepository.Query()
                .FirstOrDefaultAsync(a => a.SessionId == sessionId && a.QuestionId == request.QuestionId);

            if (answer == null)
            {
                throw new BadRequestException("Câu hỏi này không thuộc đề thi của bạn.");
            }

            answer.AnswerText = request.AnswerText?.Trim() ?? string.Empty;
            answer.SelectedOptionId = request.SelectedOptionId?.Trim();
            answer.AnswerMode = string.IsNullOrWhiteSpace(request.AnswerMode) ? answer.AnswerMode : request.AnswerMode.ToUpperInvariant();
            answer.IsFlagged = request.IsFlagged;

            await _answerRepository.SaveChangesAsync();
            return true;
        }

        public async Task<InterviewSessionResponseDto> SubmitSessionAsync(long sessionId)
        {
            var currentUserId = _currentUserService.UserId;
            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
            }

            var session = await _sessionRepository.Query()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Topic)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Class)
                .Include(s => s.User)
                .Include(s => s.Answers)
                    .ThenInclude(ans => ans.Question)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new NotFoundException($"Không tìm thấy phiên làm bài #{sessionId}.");
            }

            if (session.UserId != currentUserId.Value && !_currentUserService.IsAdmin && !_currentUserService.IsTeacher)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền nộp bài thi này.");
            }

            bool isStaff = _currentUserService.IsAdmin || _currentUserService.IsTeacher;
            if (session.Status != "IN_PROGRESS")
            {
                // Already submitted, return current results
                return BuildSessionResponseDto(session, session.Assignment, isStaff || session.Status == "PUBLISHED");
            }

            session.Status = "SUBMITTED";
            session.SubmittedAt = DateTime.UtcNow;

            var topicName = session.Assignment?.Topic?.Name ?? "Chung";

            // AUTO-GRADING ENGINE (MCQ deterministic check + AI evaluation for written answers)
            foreach (var ans in session.Answers)
            {
                var q = ans.Question;
                if (q == null) continue;

                var qType = (q.QuestionType ?? "WRITTEN").ToUpperInvariant();
                if (qType == "MCQ")
                {
                    // Automatic deterministic option check
                    bool hasChosen = !string.IsNullOrWhiteSpace(ans.SelectedOptionId);
                    bool isCorrect = hasChosen && string.Equals(ans.SelectedOptionId?.Trim(), q.CorrectOptionId?.Trim(), StringComparison.OrdinalIgnoreCase);

                    ans.IsCorrect = isCorrect;
                    ans.Score = isCorrect ? 10.0m : 0.0m;
                    ans.AiScore = ans.Score;
                    ans.AiVerdict = isCorrect ? "Đúng" : "Sai";
                    ans.AiFeedback = isCorrect
                        ? "Đáp án chính xác."
                        : (hasChosen
                            ? $"Đáp án bạn chọn ({ans.SelectedOptionId}) chưa chính xác. Đáp án đúng là {q.CorrectOptionId}."
                            : $"Học viên chưa chọn đáp án. Đáp án đúng là {q.CorrectOptionId}.");
                    ans.GradedBy = "SYSTEM_MCQ";
                    ans.GradedAt = DateTime.UtcNow;
                }
                else
                {
                    // WRITTEN / VOICE / CODE / SQL -> Gemini AI Evaluation
                    if (!string.IsNullOrWhiteSpace(ans.AnswerText))
                    {
                        try
                        {
                            var aiResult = await _aiEvaluator.EvaluateAnswerAsync(
                                topicName,
                                q.QuestionText,
                                q.Difficulty,
                                q.SampleAnswer,
                                q.Keywords,
                                ans.AnswerText
                            );

                            ans.Score = aiResult.Score;
                            ans.AiScore = aiResult.Score;
                            ans.AiVerdict = aiResult.Verdict;
                            ans.AiFeedback = aiResult.Feedback;
                            ans.GradedBy = aiResult.GradedBy;
                            ans.GradedAt = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi gọi AI chấm câu hỏi #{QuestionId} trong phiên #{SessionId}", q.Id, sessionId);
                            ans.Score = 5.0m;
                            ans.AiScore = 5.0m;
                            ans.AiVerdict = "Cần giáo viên xem xét";
                            ans.AiFeedback = "Hệ thống AI tạm thời bận. Câu trả lời của bạn đã được ghi nhận và chuyển cho giảng viên chấm trực tiếp.";
                            ans.GradedBy = "FALLBACK_PENDING";
                            ans.GradedAt = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        ans.Score = 0.0m;
                        ans.AiScore = 0.0m;
                        ans.AiVerdict = "Chưa đạt";
                        ans.AiFeedback = "Học viên chưa cung cấp câu trả lời.";
                        ans.GradedBy = "AUTO_RUBRIC";
                        ans.GradedAt = DateTime.UtcNow;
                    }
                }
            }

            // Compute Session Aggregates across ALL questions (unanswered questions score 0)
            if (session.Answers.Count > 0)
            {
                decimal totalPoints = session.Answers.Sum(a => a.AiScore ?? a.Score ?? 0.0m);
                decimal avg = Math.Round(totalPoints / session.Answers.Count, 1);

                session.AiAverageScore = avg;
                session.TotalScore = avg;
                decimal passingScore = session.Assignment?.PassingScore ?? 7.0m;
                session.OverallVerdict = avg >= passingScore ? "PASS" : "FAIL";
                session.OverallSummary = session.OverallVerdict == "PASS"
                    ? $"Học viên đạt kết quả {avg:F1}/10, vượt qua tiêu chuẩn {passingScore:F1}/10 điểm. Khả năng tư duy và phản hồi tốt."
                    : $"Học viên đạt kết quả {avg:F1}/10, dưới ngưỡng tiêu chuẩn {passingScore:F1}/10 điểm. Cần ôn luyện thêm kiến thức trọng tâm.";
            }

            await _sessionRepository.SaveChangesAsync();
            return BuildSessionResponseDto(session, session.Assignment, isStaff || session.Status == "PUBLISHED");
        }

        // ==========================================
        // TEACHER & ADMIN REVIEW WORKFLOWS
        // ==========================================

        public async Task<List<InterviewSessionSubmissionSummaryDto>> GetSubmissionsByAssignmentAsync(long assignmentId)
        {
            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var assignment = await _assignmentRepository.QueryAsNoTracking()
                    .Include(a => a.Class)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null || assignment.Class == null || assignment.Class.TeacherId != _currentUserService.UserId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền xem danh sách bài nộp của lớp khác.");
                }
            }

            var sessions = await _sessionRepository.QueryAsNoTracking()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Class)
                .Include(s => s.User)
                .Where(s => s.AssignmentId == assignmentId)
                .OrderByDescending(s => s.SubmittedAt)
                .ThenByDescending(s => s.StartedAt)
                .ToListAsync();

            var userIds = sessions.Select(s => s.UserId).Distinct().ToList();
            var students = await _studentRepository.QueryAsNoTracking()
                .Where(st => st.UserId.HasValue && userIds.Contains(st.UserId.Value))
                .ToDictionaryAsync(st => st.UserId!.Value, st => st);

            var result = new List<InterviewSessionSubmissionSummaryDto>();
            foreach (var s in sessions)
            {
                students.TryGetValue(s.UserId, out var st);
                result.Add(new InterviewSessionSubmissionSummaryDto
                {
                    SessionId = s.Id,
                    AssignmentId = s.AssignmentId,
                    AssignmentTitle = s.Assignment?.Title ?? string.Empty,
                    UserId = s.UserId,
                    StudentCode = st?.StudentCode ?? $"HV{s.UserId}",
                    StudentName = st?.FullName ?? $"{s.User?.LastName} {s.User?.FirstName}".Trim(),
                    StudentEmail = s.User?.Email ?? string.Empty,
                    ClassName = s.Assignment?.Class?.ClassName,
                    StartedAt = s.StartedAt,
                    SubmittedAt = s.SubmittedAt,
                    Status = s.Status,
                    TotalScore = s.TotalScore,
                    AiAverageScore = s.AiAverageScore,
                    TeacherScore = s.TeacherScore,
                    OverallVerdict = s.OverallVerdict,
                    ReviewedAt = s.ReviewedAt
                });
            }

            return result;
        }

        public async Task<InterviewSessionResponseDto> ReviewSessionAsync(long sessionId, ReviewSessionRequest request)
        {
            var reviewerId = _currentUserService.UserId;
            if (!reviewerId.HasValue)
            {
                throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
            }

            var session = await _sessionRepository.Query()
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Topic)
                .Include(s => s.Assignment)
                    .ThenInclude(a => a.Class)
                .Include(s => s.User)
                .Include(s => s.Answers)
                    .ThenInclude(ans => ans.Question)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new NotFoundException($"Không tìm thấy phiên làm bài #{sessionId}.");
            }

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                if (session.Assignment?.Class == null || session.Assignment.Class.TeacherId != reviewerId.Value)
                {
                    throw new UnauthorizedAccessException("Bạn không có quyền chấm bài phỏng vấn của lớp khác.");
                }
            }

            // Update individual question overrides
            if (request.QuestionScores != null && request.QuestionScores.Count > 0)
            {
                var scoreDict = request.QuestionScores.ToDictionary(qs => qs.QuestionId, qs => qs);
                foreach (var ans in session.Answers)
                {
                    if (scoreDict.TryGetValue(ans.QuestionId, out var qs))
                    {
                        if (qs.TeacherScore.HasValue)
                        {
                            ans.TeacherScore = qs.TeacherScore.Value;
                            ans.Score = qs.TeacherScore.Value;
                        }
                        if (!string.IsNullOrWhiteSpace(qs.TeacherNote))
                        {
                            ans.TeacherNote = qs.TeacherNote.Trim();
                        }
                    }
                }
            }

            session.TeacherFeedback = request.TeacherFeedback?.Trim();
            session.ReviewedBy = reviewerId.Value;
            session.ReviewedAt = DateTime.UtcNow;

            if (request.SessionTeacherScore.HasValue)
            {
                session.TeacherScore = request.SessionTeacherScore.Value;
                session.TotalScore = request.SessionTeacherScore.Value;
            }
            else
            {
                if (session.Answers.Count > 0)
                {
                    decimal totalPoints = session.Answers.Sum(a => a.TeacherScore ?? a.Score ?? a.AiScore ?? 0.0m);
                    session.TeacherScore = Math.Round(totalPoints / session.Answers.Count, 1);
                    session.TotalScore = session.TeacherScore;
                }
            }

            decimal passingScore = session.Assignment?.PassingScore ?? 7.0m;
            decimal finalScore = session.TotalScore ?? session.TeacherScore ?? session.AiAverageScore ?? 0.0m;

            if (!string.IsNullOrWhiteSpace(request.OverallVerdict))
            {
                var requestedVerdict = request.OverallVerdict.Trim().ToUpperInvariant();
                if (requestedVerdict == "PASS" && finalScore < passingScore)
                {
                    throw new BadRequestException($"Không thể xếp loại ĐẠT: Điểm đánh giá ({finalScore:0.0}/10) chưa đạt mức chuẩn qua môn ({passingScore:0.0}/10). Vui lòng cập nhật điểm hoặc chọn CHƯA ĐẠT.");
                }
                session.OverallVerdict = requestedVerdict;
            }
            else
            {
                session.OverallVerdict = finalScore >= passingScore ? "PASS" : "FAIL";
            }

            session.Status = request.PublishResult ? "PUBLISHED" : "REVIEWED";

            await _sessionRepository.SaveChangesAsync();
            return BuildSessionResponseDto(session, session.Assignment, isScoreRevealed: true);
        }

        // ==========================================
        // HELPER BUILDER
        // ==========================================

        private InterviewSessionResponseDto BuildSessionResponseDto(InterviewSession session, InterviewAssignment? assignment, bool isScoreRevealed)
        {
            assignment ??= session.Assignment;

            int durationMinutes = assignment?.DurationMinutes ?? 45;
            int remainingSeconds = 0;
            if (session.Status == "IN_PROGRESS")
            {
                var elapsedSeconds = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;
                var totalAllowedSeconds = durationMinutes * 60;
                remainingSeconds = Math.Max(0, totalAllowedSeconds - elapsedSeconds);
            }

            var studentName = $"{session.User?.LastName} {session.User?.FirstName}".Trim();
            if (string.IsNullOrWhiteSpace(studentName))
            {
                studentName = session.User?.Email ?? $"User {session.UserId}";
            }

            var questionDtos = new List<InterviewSessionQuestionDto>();
            int order = 1;

            var orderedAnswers = session.Answers?.OrderBy(a => a.Id).ToList() ?? new();

            foreach (var ans in orderedAnswers)
            {
                var q = ans.Question;
                questionDtos.Add(new InterviewSessionQuestionDto
                {
                    QuestionId = ans.QuestionId,
                    DisplayOrder = order++,
                    QuestionText = q?.QuestionText ?? string.Empty,
                    Difficulty = q?.Difficulty ?? "MEDIUM",
                    QuestionType = q?.QuestionType ?? "WRITTEN",
                    OptionsJson = q?.OptionsJson,
                    // Reveal sample answer and correct option only when score is officially revealed
                    SampleAnswer = isScoreRevealed ? q?.SampleAnswer : null,
                    CorrectOptionId = isScoreRevealed ? q?.CorrectOptionId : null,

                    AnswerText = ans.AnswerText,
                    SelectedOptionId = ans.SelectedOptionId,
                    AnswerMode = ans.AnswerMode,
                    IsFlagged = ans.IsFlagged,

                    IsCorrect = isScoreRevealed ? ans.IsCorrect : null,
                    Score = isScoreRevealed ? ans.Score : null,
                    AiScore = isScoreRevealed ? ans.AiScore : null,
                    AiVerdict = isScoreRevealed ? ans.AiVerdict : null,
                    AiFeedback = isScoreRevealed ? ans.AiFeedback : null,
                    TeacherScore = isScoreRevealed ? ans.TeacherScore : null,
                    TeacherNote = isScoreRevealed ? ans.TeacherNote : null
                });
            }

            return new InterviewSessionResponseDto
            {
                SessionId = session.Id,
                AssignmentId = session.AssignmentId,
                AssignmentTitle = assignment?.Title ?? string.Empty,
                TopicName = assignment?.Topic?.Name ?? string.Empty,
                DurationMinutes = durationMinutes,
                TotalQuestions = assignment?.TotalQuestions ?? questionDtos.Count,
                PassingScore = assignment?.PassingScore ?? 7.0m,
                UserId = session.UserId,
                StudentName = studentName,
                StudentCode = $"HV{session.UserId}",
                ClassName = assignment?.Class?.ClassName,
                StartedAt = session.StartedAt,
                SubmittedAt = session.SubmittedAt,
                RemainingSeconds = remainingSeconds,
                Status = session.Status,
                TotalScore = isScoreRevealed ? session.TotalScore : null,
                AiAverageScore = isScoreRevealed ? session.AiAverageScore : null,
                TeacherScore = isScoreRevealed ? session.TeacherScore : null,
                OverallVerdict = isScoreRevealed ? session.OverallVerdict : null,
                OverallSummary = isScoreRevealed
                    ? session.OverallSummary
                    : (session.Status == "IN_PROGRESS"
                        ? null
                        : "Bài thi của bạn đã được ghi nhận trên hệ thống và đang chờ Giảng viên đánh giá, công bố điểm chính thức."),
                TeacherFeedback = isScoreRevealed ? session.TeacherFeedback : null,
                ReviewedBy = isScoreRevealed ? session.ReviewedBy : null,
                ReviewedAt = isScoreRevealed ? session.ReviewedAt : null,
                Questions = questionDtos,
                Report = isScoreRevealed ? BuildCandidateReport(session, assignment, questionDtos) : null
            };
        }

        private static CandidateInterviewReportDto BuildCandidateReport(
            InterviewSession session,
            InterviewAssignment? assignment,
            List<InterviewSessionQuestionDto> questions)
        {
            int totalQuestions = questions.Count;
            int answeredQuestions = questions.Count(q =>
                q.QuestionType == "MCQ" ? !string.IsNullOrWhiteSpace(q.SelectedOptionId) : !string.IsNullOrWhiteSpace(q.AnswerText));

            decimal completionRate = totalQuestions > 0
                ? Math.Round(((decimal)answeredQuestions / totalQuestions) * 100.0m, 1)
                : 0m;

            // Tính điểm trung bình thực tế toàn bài (các câu chưa làm = 0 điểm)
            decimal actualAverageFromQuestions = totalQuestions > 0
                ? Math.Round(questions.Sum(q => q.Score ?? q.AiScore ?? 0m) / totalQuestions, 1)
                : 0m;

            // Xác định điểm số toàn bài:
            decimal totalScore = actualAverageFromQuestions;
            if (session.TotalScore.HasValue)
            {
                // Nếu điểm lưu trong DB lớn hơn điểm tính thực tế (do chấm riêng câu đã làm mà không phạt câu bỏ trống),
                // thì quy đổi theo tỷ lệ hoàn thành để đảm bảo tính công bằng:
                if (answeredQuestions < totalQuestions && totalQuestions > 0 && session.TotalScore.Value > actualAverageFromQuestions)
                {
                    totalScore = Math.Round(session.TotalScore.Value * ((decimal)answeredQuestions / totalQuestions), 1);
                }
                else
                {
                    totalScore = session.TotalScore.Value;
                }
            }

            decimal totalScorePercent = Math.Round(Math.Clamp((totalScore / 10.0m) * 100.0m, 0m, 100m), 1);

            // Determine Proficiency Rank & Recommendation with strict Completion Rate Gating:
            // Ứng viên BẮT BUỘC phải hoàn thành khối lượng tối thiểu thì mới được xét các thứ hạng đạt chuẩn!
            string rank;
            string rankTitle;
            string rankSummary;
            string ojtRecommendation;

            if (completionRate < 50m)
            {
                rank = "NOVICE";
                rankTitle = "Chưa đạt (Không hoàn thành đủ bài thi)";
                rankSummary = $"Ứng viên chỉ hoàn thành {completionRate}% ({answeredQuestions}/{totalQuestions} câu hỏi), bỏ trống phần lớn bài thi nên chưa đủ điều kiện đánh giá chuẩn đầu ra.";
                ojtRecommendation = "Yêu cầu thi lại toàn bộ bài phỏng vấn để có đủ dữ liệu đánh giá năng lực thực tế.";
            }
            else if (completionRate < 70m)
            {
                rank = "DEVELOPING";
                rankTitle = "Trung bình (Developing - Chưa đủ khối lượng thi)";
                rankSummary = $"Ứng viên mới hoàn thành {completionRate}% ({answeredQuestions}/{totalQuestions} câu hỏi). Cần rèn luyện tính kỷ luật, quản lý thời gian và hoàn thành trọn vẹn đề thi.";
                ojtRecommendation = "Cần tham gia thêm 1 đợt phỏng vấn bổ trợ và hoàn thành tối thiểu 70% số câu hỏi trước khi xét tiếp nhận dự án.";
            }
            else if (totalScorePercent >= 90m && completionRate >= 90m)
            {
                rank = "ADVANCED";
                rankTitle = "Xuất sắc (Ready to Deploy - Fast Track)";
                rankSummary = "Ứng viên nắm vững toàn diện kiến thức nền tảng và chuyên sâu, tư duy hệ thống và khả năng giải thích bản chất kỹ thuật xuất sắc.";
                ojtRecommendation = "Sẵn sàng tham gia trực tiếp vào các dự án phần mềm trọng điểm (Core Developer). Khuyến nghị vào các dự án công nghệ mới.";
            }
            else if (totalScorePercent >= 80m && completionRate >= 80m)
            {
                rank = "PROFICIENT";
                rankTitle = "Giỏi (Ready to Deploy - Standard)";
                rankSummary = "Ứng viên có năng lực chuyên môn tốt, hiểu sâu cơ chế hoạt động và cấu trúc hệ thống, phản xạ kỹ thuật vững vàng.";
                ojtRecommendation = "Đạt chuẩn yêu cầu phỏng vấn tuyển dụng trực tiếp vào Business Unit (BU) FPT Software. Sẵn sàng nhận dự án thực tế.";
            }
            else if (totalScorePercent >= 70m && completionRate >= 70m)
            {
                rank = "COMPETENT";
                rankTitle = "Khá (Competent Fresher)";
                rankSummary = "Ứng viên đáp ứng chuẩn đầu ra môn học, nắm chắc kiến thức cơ bản và vận dụng tốt các kỹ năng lập trình thông dụng.";
                ojtRecommendation = "Đạt chuẩn tốt nghiệp khóa đào tạo Fresher. Khuyến nghị phân công Mentor kèm cặp 1-2 tuần đầu khi onboard dự án.";
            }
            else if (totalScorePercent >= 50m)
            {
                rank = "DEVELOPING";
                rankTitle = "Trung bình (Developing - Cần bổ túc)";
                rankSummary = "Ứng viên nắm được các khái niệm cơ bản nhưng còn hạn chế ở các câu hỏi tình huống thực tế và tối ưu hóa hệ thống.";
                ojtRecommendation = "Cần tham gia thêm 1 Sprint ôn tập và làm bài tập thực hành bổ trợ trước khi tham gia phỏng vấn lại.";
            }
            else
            {
                rank = "NOVICE";
                rankTitle = "Chưa đạt (Requires Retraining)";
                rankSummary = "Ứng viên chưa nắm vững các từ khóa trọng tâm và kiến thức cốt lõi của môn học.";
                ojtRecommendation = "Cần bố trí đào tạo lại (Retrain) module chuyên môn này để củng cố nền tảng kiến thức.";
            }

            // Difficulty Breakdown
            var easyQuestions = questions.Where(q => string.Equals(q.Difficulty, "EASY", StringComparison.OrdinalIgnoreCase)).ToList();
            var mediumQuestions = questions.Where(q => string.Equals(q.Difficulty, "MEDIUM", StringComparison.OrdinalIgnoreCase)).ToList();
            var hardQuestions = questions.Where(q => string.Equals(q.Difficulty, "HARD", StringComparison.OrdinalIgnoreCase)).ToList();

            decimal easyScorePercent = easyQuestions.Count > 0
                ? Math.Round((easyQuestions.Average(q => q.Score ?? q.AiScore ?? 0m) / 10.0m) * 100.0m, 1)
                : 0m;
            decimal mediumScorePercent = mediumQuestions.Count > 0
                ? Math.Round((mediumQuestions.Average(q => q.Score ?? q.AiScore ?? 0m) / 10.0m) * 100.0m, 1)
                : 0m;
            decimal hardScorePercent = hardQuestions.Count > 0
                ? Math.Round((hardQuestions.Average(q => q.Score ?? q.AiScore ?? 0m) / 10.0m) * 100.0m, 1)
                : 0m;

            // Format Breakdown
            var mcqQuestions = questions.Where(q => string.Equals(q.QuestionType, "MCQ", StringComparison.OrdinalIgnoreCase)).ToList();
            int mcqTotal = mcqQuestions.Count;
            int mcqCorrect = mcqQuestions.Count(q => q.IsCorrect == true);
            decimal mcqAccuracyRate = mcqTotal > 0 ? Math.Round(((decimal)mcqCorrect / mcqTotal) * 100.0m, 1) : 0m;

            var subjectiveQuestions = questions.Where(q => !string.Equals(q.QuestionType, "MCQ", StringComparison.OrdinalIgnoreCase)).ToList();
            int subjectiveTotal = subjectiveQuestions.Count;
            decimal subjectiveScorePercent = subjectiveTotal > 0
                ? Math.Round((subjectiveQuestions.Average(q => q.Score ?? q.AiScore ?? 0m) / 10.0m) * 100.0m, 1)
                : 0m;

            // Strengths and Areas For Improvement
            var strengths = new List<string>();
            var growthAreas = new List<string>();
            var recommendations = new List<string>();

            if (mcqAccuracyRate >= 80m && mcqTotal > 0)
            {
                strengths.Add($"Nắm chắc kiến thức trắc nghiệm lý thuyết căn bản (Độ chính xác {mcqAccuracyRate}%).");
            }
            if (easyScorePercent >= 80m && easyQuestions.Count > 0)
            {
                strengths.Add($"Nền tảng cú pháp và khái niệm cơ bản vững vàng (Mức Dễ: {easyScorePercent}%).");
            }
            if (mediumScorePercent >= 75m && mediumQuestions.Count > 0)
            {
                strengths.Add($"Hiểu đúng cơ chế hoạt động và kiến trúc giải pháp (Mức Trung bình: {mediumScorePercent}%).");
            }
            if (hardScorePercent >= 70m && hardQuestions.Count > 0)
            {
                strengths.Add($"Khả năng phân tích và xử lý bài toán kỹ thuật phức tạp tốt (Mức Khó: {hardScorePercent}%).");
            }

            if (strengths.Count == 0)
            {
                strengths.Add("Học viên đã nỗ lực hoàn thành đầy đủ các câu hỏi theo thời gian quy định.");
            }

            if (hardScorePercent < 70m && hardQuestions.Count > 0)
            {
                growthAreas.Add($"Cần nâng cao kỹ năng xử lý các bài toán mức Khó (Hiện đạt {hardScorePercent}%).");
            }
            if (mcqAccuracyRate < 80m && mcqTotal > 0)
            {
                growthAreas.Add($"Cần rà soát lại các khái niệm lý thuyết cốt lõi để giảm thiểu sai sót trắc nghiệm (Đúng {mcqCorrect}/{mcqTotal} câu).");
            }
            if (subjectiveScorePercent < 75m && subjectiveTotal > 0)
            {
                growthAreas.Add("Cần trình bày câu trả lời tự luận/thoại mạch lạc hơn, nhấn mạnh vào các từ khóa kỹ thuật trọng tâm.");
            }

            if (growthAreas.Count == 0)
            {
                growthAreas.Add("Duy trì phong độ và tiếp tục tìm hiểu các mẫu thiết kế (Design Patterns) nâng cao.");
            }

            // Recommendations
            recommendations.Add("Tham khảo kho tài liệu giáo trình và thực hành Clean Code trong phân hệ Tài liệu (Books).");
            if (hardScorePercent < 70m)
            {
                recommendations.Add("Tập trung ôn luyện các chủ đề chuyên sâu: Quản lý bộ nhớ, Concurrency, Tối ưu hóa truy vấn Database và Security.");
            }
            recommendations.Add("Luyện tập thêm các bài phỏng vấn thoại để cải thiện phản xạ trả lời nhanh và phong thái tự tin.");

            return new CandidateInterviewReportDto
            {
                TotalScorePercentage = totalScorePercent,
                CompletionRate = completionRate,
                TotalQuestions = totalQuestions,
                AnsweredQuestions = answeredQuestions,
                ProficiencyRank = rank,
                ProficiencyRankTitle = rankTitle,
                ProficiencySummary = rankSummary,
                OjtRecommendation = ojtRecommendation,
                EasyCount = easyQuestions.Count,
                EasyScorePercent = easyScorePercent,
                MediumCount = mediumQuestions.Count,
                MediumScorePercent = mediumScorePercent,
                HardCount = hardQuestions.Count,
                HardScorePercent = hardScorePercent,
                McqTotal = mcqTotal,
                McqCorrect = mcqCorrect,
                McqAccuracyRate = mcqAccuracyRate,
                SubjectiveTotal = subjectiveTotal,
                SubjectiveScorePercent = subjectiveScorePercent,
                Strengths = strengths,
                AreasForImprovement = growthAreas,
                Recommendations = recommendations
            };
        }

        private IQueryable<InterviewAssignment> GetScopedAssignmentQuery()
        {
            var query = _assignmentRepository.QueryAsNoTracking()
                .Include(a => a.Topic)
                .Include(a => a.Class)
                .AsQueryable();

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                query = query.Where(a => a.Class != null && a.Class.TeacherId == _currentUserService.UserId.Value);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                query = query.Where(a => a.ClassId == null || (a.Class != null && a.Class.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value))));
            }

            return query;
        }

        public async Task<byte[]> ExportExcelAsync()
        {
            var assignments = await GetScopedAssignmentQuery()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var headers = new[] { "STT", "Tiêu đề đợt đánh giá", "Chủ đề", "Lớp đào tạo", "Thời lượng (phút)", "Tổng số câu", "Điểm đạt", "Trạng thái", "Ngày tạo" };
            var rows = assignments.Select((a, idx) => new object?[]
            {
                idx + 1,
                a.Title,
                a.Topic?.Name ?? "Chưa phân chủ đề",
                a.Class != null ? $"{a.Class.ClassCode} - {a.Class.ClassName}" : "Tất cả học viên",
                a.DurationMinutes,
                a.TotalQuestions,
                $"{a.PassingScore:F1}/10",
                a.Status,
                a.CreatedAt
            });

            return ExcelExportHelper.CreateWorkbook("Đợt phỏng vấn", "DANH SÁCH ĐỢT ĐÁNH GIÁ PHỎNG VẤN (INTERVIEW ASSIGNMENTS)", headers, rows);
        }

        public async Task<byte[]> ExportCsvAsync()
        {
            var assignments = await GetScopedAssignmentQuery()
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("STT,Tiêu đề đợt đánh giá,Chủ đề,Lớp đào tạo,Thời lượng (phút),Tổng số câu,Điểm đạt,Trạng thái,Ngày tạo");

            int index = 1;
            foreach (var a in assignments)
            {
                var topicName = (a.Topic?.Name ?? "Chưa phân chủ đề").Replace("\"", "\"\"");
                var className = (a.Class != null ? $"{a.Class.ClassCode} - {a.Class.ClassName}" : "Tất cả học viên").Replace("\"", "\"\"");
                var title = a.Title.Replace("\"", "\"\"");
                sb.AppendLine($"{index++},\"{title}\",\"{topicName}\",\"{className}\",{a.DurationMinutes},{a.TotalQuestions},{a.PassingScore:F1},{a.Status},{a.CreatedAt:dd/MM/yyyy HH:mm}");
            }

            return CsvExportHelper.ToUtf8BomBytes(sb.ToString());
        }
    }
}
