using Backend.Common.Security;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/topic-marks")]
    [TypeFilter(typeof(AutoPermissionFilter))]
    public class TopicMarksController : ControllerBase
    {
        private readonly IStudentScoreService _scoreService;

        public TopicMarksController(IStudentScoreService scoreService)
        {
            _scoreService = scoreService;
        }

        [HttpGet("gradebook/search")]
        public async Task<IActionResult> SearchGradebook(
            [FromQuery] int page = 0,
            [FromQuery] int size = 10,
            [FromQuery] string? sort = null,
            [FromQuery] string? keyword = null)
        {
            var response = await _scoreService.SearchGradebookAsync(page, size, sort, keyword);
            return Ok(response);
        }

        [HttpGet("students/{id}")]
        public async Task<IActionResult> GetStudentScore(long id)
        {
            var student = await _scoreService.GetStudentGradebookAsync(id);
            if (student == null)
            {
                return NotFound(new { message = $"Student score with ID {id} not found." });
            }
            return Ok(student);
        }
    }
}
