using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext()
        {
        }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Book> Books { get; set; } = null!;
        public virtual DbSet<Module> Modules { get; set; } = null!;
        public virtual DbSet<ModuleGroup> ModuleGroups { get; set; } = null!;
        public virtual DbSet<OneTimeToken> OneTimeTokens { get; set; } = null!;
        public virtual DbSet<Permission> Permissions { get; set; } = null!;
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public virtual DbSet<Role> Roles { get; set; } = null!;
        public virtual DbSet<StudentScore> StudentScores { get; set; } = null!;
        public virtual DbSet<StudentScoreColumn> StudentScoreColumns { get; set; } = null!;
        public virtual DbSet<User> Users { get; set; } = null!;
        public virtual DbSet<TrainingClass> Classes { get; set; } = null!;
        public virtual DbSet<Student> Students { get; set; } = null!;
        public virtual DbSet<Attendance> Attendances { get; set; } = null!;
        public virtual DbSet<MockProject> MockProjects { get; set; } = null!;
        public virtual DbSet<Allocation> Allocations { get; set; } = null!;
        public virtual DbSet<InterviewTopic> InterviewTopics { get; set; } = null!;
        public virtual DbSet<InterviewQuestion> InterviewQuestions { get; set; } = null!;
        public virtual DbSet<InterviewAnswer> InterviewAnswers { get; set; } = null!;
        public virtual DbSet<InterviewAssignment> InterviewAssignments { get; set; } = null!;
        public virtual DbSet<InterviewSession> InterviewSessions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
