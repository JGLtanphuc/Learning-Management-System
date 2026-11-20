using Backend.Common.Configurations;
using Backend.Data;
using Backend.Entities;
using Backend.Middlewares;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString;
                options.JsonSerializerOptions.Converters.Add(new Backend.Common.Configurations.NullableDateTimeConverter());
            });
            
            // Đăng ký FluentValidation & Tự động kích hoạt (Auto-Validation)
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Program>();
            
            // Đăng ký Global Exception Handling & RFC 7807 ProblemDetails (.NET 8)
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "FAS API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập token JWT theo định dạng: Bearer {your_token}",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Cấu hình JWT Bearer Authentication qua Strongly-typed Options
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
            builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection(GeminiSettings.SectionName));
            var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Cấu hình 'JwtSettings' bị thiếu trong appsettings.json.");

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
            
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            builder.Services.AddSingleton<Backend.Data.Interceptors.AuditableEntityInterceptor>();
            builder.Services.AddDbContext<AppDbContext>((sp, option) =>
            {
                var interceptor = sp.GetRequiredService<Backend.Data.Interceptors.AuditableEntityInterceptor>();
                option.UseSqlServer(builder.Configuration.GetConnectionString("StarterProjectDb"), sqlOptions =>
                {
                    sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                })
                .AddInterceptors(interceptor);
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(origin => true) 
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("Content-Disposition");               
                });
            });

            // Đăng ký Common Helpers & Security
            builder.Services.AddScoped<Backend.Common.Security.JwtTokenHelper>();
            builder.Services.AddScoped<Backend.Common.Security.AutoPermissionFilter>();

            // Đăng ký Tầng Repositories (Data Access Layer)
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IBookRepository, Backend.Repositories.Implementations.BookRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IStudentScoreRepository, Backend.Repositories.Implementations.StudentScoreRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IUserRepository, Backend.Repositories.Implementations.UserRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IRoleRepository, Backend.Repositories.Implementations.RoleRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IPermissionRepository, Backend.Repositories.Implementations.PermissionRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IModuleRepository, Backend.Repositories.Implementations.ModuleRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IModuleGroupRepository, Backend.Repositories.Implementations.ModuleGroupRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IClassRepository, Backend.Repositories.Implementations.ClassRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IStudentRepository, Backend.Repositories.Implementations.StudentRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IAttendanceRepository, Backend.Repositories.Implementations.AttendanceRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IMockProjectRepository, Backend.Repositories.Implementations.MockProjectRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IAllocationRepository, Backend.Repositories.Implementations.AllocationRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IInterviewTopicRepository, Backend.Repositories.Implementations.InterviewTopicRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IInterviewQuestionRepository, Backend.Repositories.Implementations.InterviewQuestionRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IInterviewAnswerRepository, Backend.Repositories.Implementations.InterviewAnswerRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IInterviewAssignmentRepository, Backend.Repositories.Implementations.InterviewAssignmentRepository>();
            builder.Services.AddScoped<Backend.Repositories.Interfaces.IInterviewSessionRepository, Backend.Repositories.Implementations.InterviewSessionRepository>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<Backend.Services.Interfaces.ICurrentUserService, Backend.Services.Implementations.CurrentUserService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IInterviewAiEvaluator, Backend.Services.Implementations.InterviewAiEvaluator>();

            // Đăng ký Tầng Services (Business Logic Layer)
            builder.Services.AddScoped<Backend.Services.Interfaces.IAuthService, Backend.Services.Implementations.AuthService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IBookService, Backend.Services.Implementations.BookService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IStudentScoreService, Backend.Services.Implementations.StudentScoreService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IUserService, Backend.Services.Implementations.UserService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IRoleService, Backend.Services.Implementations.RoleService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IPermissionService, Backend.Services.Implementations.PermissionService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IModuleService, Backend.Services.Implementations.ModuleService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IModuleGroupService, Backend.Services.Implementations.ModuleGroupService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IDashboardService, Backend.Services.Implementations.DashboardService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IClassService, Backend.Services.Implementations.ClassService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.ITeacherService, Backend.Services.Implementations.TeacherService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IStudentService, Backend.Services.Implementations.StudentService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IAttendanceService, Backend.Services.Implementations.AttendanceService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IMockProjectService, Backend.Services.Implementations.MockProjectService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IAllocationService, Backend.Services.Implementations.AllocationService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IInterviewTopicService, Backend.Services.Implementations.InterviewTopicService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IInterviewQuestionService, Backend.Services.Implementations.InterviewQuestionService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IInterviewAssignmentService, Backend.Services.Implementations.InterviewAssignmentService>();
            builder.Services.AddScoped<Backend.Services.Interfaces.IImportExportService, Backend.Services.Implementations.ImportExportService>();

            var app = builder.Build();

            // Pipeline xử lý lỗi tập trung
            app.UseExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseCors("AllowAll");

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
