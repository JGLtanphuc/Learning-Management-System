using Backend.DTOs.Dashboard;
using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IModuleGroupRepository _moduleGroupRepository;
        private readonly IModuleRepository _moduleRepository;
        private readonly IClassRepository _classRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IMockProjectRepository _mockProjectRepository;
        private readonly IAllocationRepository _allocationRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ICurrentUserService _currentUserService;

        public DashboardService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IModuleGroupRepository moduleGroupRepository,
            IModuleRepository moduleRepository,
            IClassRepository classRepository,
            IStudentRepository studentRepository,
            IMockProjectRepository mockProjectRepository,
            IAllocationRepository allocationRepository,
            IAttendanceRepository attendanceRepository,
            ICurrentUserService currentUserService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _moduleGroupRepository = moduleGroupRepository;
            _moduleRepository = moduleRepository;
            _classRepository = classRepository;
            _studentRepository = studentRepository;
            _mockProjectRepository = mockProjectRepository;
            _allocationRepository = allocationRepository;
            _attendanceRepository = attendanceRepository;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardStatsDto> GetStatsAsync()
        {
            var totalUsers = await _userRepository.QueryAsNoTracking().CountAsync();
            var activeUsers = await _userRepository.QueryAsNoTracking().Where(u => u.IsActive).CountAsync();
            var totalRoles = await _roleRepository.QueryAsNoTracking().CountAsync();
            var activeRoles = await _roleRepository.QueryAsNoTracking().Where(r => r.IsActive).CountAsync();
            var totalMenus = await _moduleGroupRepository.QueryAsNoTracking().CountAsync();
            var activeMenus = await _moduleGroupRepository.QueryAsNoTracking().Where(g => g.IsActive).CountAsync();
            var totalMenuItems = await _moduleRepository.QueryAsNoTracking().CountAsync();

            int totalClasses;
            int totalStudents;
            int activeMockProjects;
            int totalAllocations;

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var uid = _currentUserService.UserId.Value;
                totalClasses = await _classRepository.QueryAsNoTracking().Where(c => c.TeacherId == uid).CountAsync();
                totalStudents = await _studentRepository.QueryAsNoTracking().Where(s => s.Class != null && s.Class.TeacherId == uid).CountAsync();
                activeMockProjects = await _mockProjectRepository.QueryAsNoTracking()
                    .Where(p => (p.Status == "ACTIVE" || p.Status == "PLANNING") && p.Students.Any(s => s.Class != null && s.Class.TeacherId == uid))
                    .CountAsync();
                totalAllocations = await _allocationRepository.QueryAsNoTracking()
                    .Where(a => a.Student.Class != null && a.Student.Class.TeacherId == uid)
                    .CountAsync();
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                totalClasses = await _classRepository.QueryAsNoTracking()
                    .Where(c => c.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)))
                    .CountAsync();
                totalStudents = 1;
                activeMockProjects = await _mockProjectRepository.QueryAsNoTracking()
                    .Where(p => p.Students.Any(s => s.Email == email || (uid.HasValue && s.UserId == uid.Value)))
                    .CountAsync();
                totalAllocations = await _allocationRepository.QueryAsNoTracking()
                    .Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value))
                    .CountAsync();
            }
            else
            {
                // Admin gets overall total counts
                totalClasses = await _classRepository.QueryAsNoTracking().CountAsync();
                totalStudents = await _studentRepository.QueryAsNoTracking().CountAsync();
                activeMockProjects = await _mockProjectRepository.QueryAsNoTracking().Where(p => p.Status == "ACTIVE" || p.Status == "PLANNING").CountAsync();
                totalAllocations = await _allocationRepository.QueryAsNoTracking().CountAsync();
            }

            // Real attendance & On-track calculations from Database
            var attendanceQuery = _attendanceRepository.QueryAsNoTracking();
            var classQuery = _classRepository.QueryAsNoTracking();

            if (_currentUserService.IsTeacher && _currentUserService.UserId.HasValue)
            {
                var uid = _currentUserService.UserId.Value;
                attendanceQuery = attendanceQuery.Where(a => a.Class != null && a.Class.TeacherId == uid);
                classQuery = classQuery.Where(c => c.TeacherId == uid);
            }
            else if (_currentUserService.IsStudent)
            {
                var email = _currentUserService.Email ?? "";
                var uid = _currentUserService.UserId;
                attendanceQuery = attendanceQuery.Where(a => a.Student.Email == email || (uid.HasValue && a.Student.UserId == uid.Value));
            }

            var totalAttendances = await attendanceQuery.CountAsync();
            var presentAttendances = await attendanceQuery.Where(a => a.Status == "PRESENT").CountAsync();
            double avgAttendanceRate = totalAttendances > 0
                ? Math.Round((double)presentAttendances / totalAttendances * 100.0, 1)
                : 100.0;

            var totalClassCount = await classQuery.CountAsync();
            var onTrackClassCount = await classQuery.Where(c => c.Status == "ONGOING" || c.Status == "COMPLETED").CountAsync();
            double onTrackRate = totalClassCount > 0
                ? Math.Round((double)onTrackClassCount / totalClassCount * 100.0, 1)
                : 100.0;

            var classAttendanceGroups = await attendanceQuery
                .GroupBy(a => a.ClassId)
                .Select(g => new
                {
                    ClassId = g.Key,
                    Total = g.Count(),
                    Present = g.Count(a => a.Status == "PRESENT")
                })
                .ToListAsync();

            var classRates = classAttendanceGroups.ToDictionary(
                x => x.ClassId,
                x => x.Total > 0 ? Math.Round((double)x.Present / x.Total * 100.0, 1) : 100.0
            );

            return new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalRoles = totalRoles,
                ActiveRoles = activeRoles,
                TotalMenus = totalMenus,
                ActiveMenus = activeMenus,
                TotalMenuItems = totalMenuItems,
                TotalClasses = totalClasses,
                TotalStudents = totalStudents,
                ActiveMockProjects = activeMockProjects,
                TotalAllocations = totalAllocations,
                AverageAttendanceRate = avgAttendanceRate,
                OnTrackRate = onTrackRate,
                ClassAttendanceRates = classRates
            };
        }
    }
}

