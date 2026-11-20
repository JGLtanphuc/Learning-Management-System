using Backend.Data;
using Backend.Entities;
using Backend.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Backend.Repositories.Implementations
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailWithRolesAsync(string email)
        {
            var normalized = email.Trim().ToLower();
            var user = await _context.Users
                .Include(u => u.Roles)
                    .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized
                    || (u.TeacherCode != null && u.TeacherCode.ToLower() == normalized));

            if (user != null) return user;

            // Kiểm tra theo StudentCode hoặc Email của học viên liên kết
            var student = await _context.Students
                .Include(s => s.User)
                    .ThenInclude(u => u!.Roles)
                        .ThenInclude(r => r.Permissions)
                .FirstOrDefaultAsync(s => s.StudentCode.ToLower() == normalized || s.Email.ToLower() == normalized);

            return student?.User;
        }

        public async Task<User?> GetByIdWithRolesAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public IQueryable<User> QueryWithRoles()
        {
            return _context.Users
                .Include(u => u.Roles)
                .AsQueryable();
        }
    }
}


