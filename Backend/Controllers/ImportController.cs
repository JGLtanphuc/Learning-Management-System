using Backend.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/import")]
    public class ImportController : ControllerBase
    {
        [HttpGet("template")]
        public IActionResult GetTemplate([FromQuery] string? entity)
        {
            var normalizedEntity = (entity ?? "").Trim().ToLower().Replace("-", "_");
            var (filename, headers, sampleRow1, sampleRow2) = GetTemplateDefinition(normalizedEntity);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));
            if (!string.IsNullOrEmpty(sampleRow1)) sb.AppendLine(sampleRow1);
            if (!string.IsNullOrEmpty(sampleRow2)) sb.AppendLine(sampleRow2);

            return File(CsvExportHelper.ToUtf8BomBytes(sb.ToString()), "text/csv; charset=utf-8", filename);
        }

        private static (string Filename, string[] Headers, string Sample1, string Sample2) GetTemplateDefinition(string entity)
        {
            return entity switch
            {
                "student" => (
                    "student_template.csv",
                    new[] { "studentCode", "fullName", "email", "phone", "className", "university", "major", "gpa", "status" },
                    "\"SE160101\",\"Nguyen Van An\",\"an.nv@fams.edu.vn\",\"0901234567\",\"HN26_FR_JAVA_01\",\"Bach Khoa University\",\"Software Engineering\",8.50,\"ACTIVE\"",
                    "\"SE160102\",\"Tran Thi Binh\",\"binh.tt@fams.edu.vn\",\"0912345678\",\"HN26_FR_JAVA_01\",\"FPT University\",\"Information Assurance\",8.20,\"ACTIVE\""
                ),
                "class" or "classes" => (
                    "class_template.csv",
                    new[] { "classCode", "className", "programName", "startDate", "endDate", "capacity", "location", "room", "status" },
                    "\"HN26_FR_JAVA_01\",\"Fresher Java Spring Boot 01\",\"Java Backend Professional\",\"2026-06-01\",\"2026-09-30\",25,\"Hanoi Campus\",\"Lab 301\",\"ONGOING\"",
                    "\"HCM26_FR_DOTNET_02\",\"Fresher .NET Fullstack 02\",\".NET Enterprise Development\",\"2026-07-01\",\"2026-10-31\",20,\"HCMC F-Town\",\"Room 402\",\"PLANNING\""
                ),
                "attendance" or "attendances" => (
                    "attendance_template.csv",
                    new[] { "studentCode", "studentName", "className", "attendanceDate", "slot", "status", "note" },
                    "\"SE160101\",\"Nguyen Van An\",\"HN26_FR_JAVA_01\",\"2026-09-01\",\"MORNING\",\"PRESENT\",\"On time\"",
                    "\"SE160102\",\"Tran Thi Binh\",\"HN26_FR_JAVA_01\",\"2026-09-01\",\"AFTERNOON\",\"EXCUSED\",\"Nghi om co phep\""
                ),
                "mock_project" or "mockproject" or "mock_projects" => (
                    "mock_project_template.csv",
                    new[] { "projectName", "studentCodes", "mentorName", "techStack", "repoUrl", "status", "finalScore" },
                    "\"FAS Learning Hub Portal\",\"SE160101, SE160102\",\"Nguyen Huu Dat\",\"Java 17, Spring Boot, React, PostgreSQL\",\"https://github.com/fams/fas-learning-hub\",\"ACTIVE\",8.80",
                    "\"Enterprise Asset Management\",\"\",\"Le Quoc Tuan\",\"C# .NET 8, EF Core, Next.js, SQL Server\",\"https://github.com/fams/asset-mgmt\",\"PLANNING\","
                ),
                "allocation" or "allocations" => (
                    "allocation_template.csv",
                    new[] { "studentCode", "studentName", "targetUnit", "projectName", "roleTitle", "matchScore", "status", "interviewDate", "note" },
                    "\"SE160101\",\"Nguyen Van An\",\"FSU1.BU9\",\"Banking Core Modernization\",\"Backend Java Developer\",92.50,\"ALLOCATED\",\"2026-09-05\",\"Passed client interview\"",
                    "\"SE160102\",\"Tran Thi Binh\",\"DXL.BU2\",\"Smart Factory IoT Platform\",\"Fullstack Developer\",88.00,\"PROPOSED\",\"2026-09-10\",\"\""
                ),
                "permission" or "permissions" => (
                    "permission_template.csv",
                    new[] { "name", "resource", "action", "description" },
                    "\"CLASS_CREATE\",\"CLASS\",\"CREATE\",\"Tao moi lop hoc\"",
                    "\"CLASS_READ\",\"CLASS\",\"READ\",\"Xem danh sach lop hoc\""
                ),
                "role" or "roles" => (
                    "role_template.csv",
                    new[] { "name", "description" },
                    "\"TRAINER\",\"Giang vien dao tao Fresher\"",
                    "\"MENTOR\",\"Mentor huong dan Mock Project\""
                ),
                "user" or "users" => (
                    "user_template.csv",
                    new[] { "email", "firstName", "lastName", "role" },
                    "\"instructor@example.com\",\"Van An\",\"Nguyen\",\"TEACHER\"",
                    "\"mentor@example.com\",\"Quoc Tuan\",\"Le\",\"TEACHER\""
                ),
                "module" or "modules" or "menu" or "menus" => (
                    "module_template.csv",
                    new[] { "title", "url", "icon", "requiredPermission", "displayOrder" },
                    "\"Exam Schedule\",\"/exams\",\"calendar\",\"CLASS_READ\",5",
                    "\"Certificates\",\"/certificates\",\"award\",\"STUDENT_READ\",6"
                ),
                "module_group" or "modulegroup" or "menu_group" or "menu_groups" => (
                    "module_group_template.csv",
                    new[] { "name", "description", "displayOrder" },
                    "\"Academic Affairs\",\"Managing classes and cohorts\",3",
                    "\"Graduation Affairs\",\"Mock projects and allocations\",4"
                ),
                "book" or "books" => (
                    "book_template.csv",
                    new[] { "name", "description" },
                    "\"Clean Code: A Handbook of Agile Software Craftsmanship\",\"Robert C. Martin\"",
                    "\"Design Patterns: Elements of Reusable Object-Oriented Software\",\"Gang of Four\""
                ),
                "student_score" or "studentscore" or "student_scores" => (
                    "student_score_template.csv",
                    new[] { "studentCode", "studentName", "className", "attendance", "quiz1", "quiz2", "assignment", "practice", "midterm", "final", "project", "bonus", "overallScore" },
                    "\"STU001\",\"Nguyen Van An\",\"SE-01\",10.0,8.5,9.0,8.0,8.5,8.0,9.0,8.5,0.5,8.75",
                    "\"STU002\",\"Tran Thi Binh\",\"SE-01\",9.5,8.0,8.5,9.0,8.0,7.5,8.5,9.0,0.0,8.35"
                ),
                "interview_topic" or "interviewtopic" or "interview_topics" => (
                    "interview_topic_template.csv",
                    new[] { "name", "description", "displayOrder" },
                    "\"Java Core & OOP Fundamentals\",\"Kien thuc lap trinh huong doi tuong va Java co ban\",1",
                    "\"Spring Boot & Microservices\",\"Spring Framework, Dependency Injection, RESTful APIs\",2"
                ),
                "interview_question" or "interviewquestion" or "interview_questions" => (
                    "interview_question_template.csv",
                    new[] { "topicName", "questionText", "questionType", "difficulty", "sampleAnswer", "keywords", "correctOption", "displayOrder" },
                    "\"Java Core & OOP Fundamentals\",\"Phan biet == va equals() trong Java khi so sanh doi tuong?\",\"WRITTEN\",\"MEDIUM\",\"Toan tu == so sanh dia chi o nho tren Heap, equals() so sanh noi dung ben trong.\",\"heap, memory, reference, value\",\"\",1",
                    "\"Spring Boot & Microservices\",\"Annotation @SpringBootApplication ket hop 3 annotation nao?\",\"MCQ\",\"EASY\",\"Ket hop tu @Configuration, @EnableAutoConfiguration va @ComponentScan.\",\"configuration, scan, auto\",\"A\",2"
                ),
                _ => (
                    $"{entity}_template.csv",
                    new[] { "name", "description", "status" },
                    "\"Sample Item 1\",\"Sample Description 1\",\"ACTIVE\"",
                    "\"Sample Item 2\",\"Sample Description 2\",\"ACTIVE\""
                )
            };
        }
    }
}

