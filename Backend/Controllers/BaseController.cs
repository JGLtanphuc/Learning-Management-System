using Backend.Common.Security;
using Backend.DTOs.Common;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [TypeFilter(typeof(AutoPermissionFilter))]
    public abstract class BaseController<TResponseDto, TCreateRequest, TUpdateRequest> : ControllerBase
        where TResponseDto : IHasId
    {
        protected readonly IService<TResponseDto, TCreateRequest, TUpdateRequest> _service;

        protected BaseController(IService<TResponseDto, TCreateRequest, TUpdateRequest> service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(long id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi với ID {id}." });
            }
            return Ok(item);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] TCreateRequest request)
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public virtual async Task<IActionResult> Update(long id, [FromBody] TUpdateRequest request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi với ID {id}." });
            }
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> Delete(long id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy bản ghi với ID {id}." });
            }
            return NoContent();
        }
    }
}
