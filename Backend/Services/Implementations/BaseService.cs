using Backend.Repositories.Interfaces;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations
{
    public abstract class BaseService<TEntity, TResponseDto, TCreateRequest, TUpdateRequest> 
        : IService<TResponseDto, TCreateRequest, TUpdateRequest> where TEntity : class
    {
        protected readonly IRepository<TEntity> _repository;

        protected BaseService(IRepository<TEntity> repository)
        {
            _repository = repository;
        }

        protected abstract TResponseDto MapToResponseDto(TEntity entity);
        protected abstract TEntity MapToEntity(TCreateRequest request);
        protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

        public virtual async Task<TResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? default : MapToResponseDto(entity);
        }

        public virtual async Task<TResponseDto> CreateAsync(TCreateRequest request)
        {
            var entity = MapToEntity(request);
            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();
            return MapToResponseDto(entity);
        }

        public virtual async Task<TResponseDto?> UpdateAsync(long id, TUpdateRequest request)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return default;

            UpdateEntity(entity, request);
            // Entity đã được ChangeTracker theo dõi, chỉ cần SaveChangesAsync để update các property thay đổi
            await _repository.SaveChangesAsync();
            return MapToResponseDto(entity);
        }

        public virtual async Task<bool> DeleteAsync(long id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            _repository.Delete(entity);
            await _repository.SaveChangesAsync();
            return true;
        }
    }
}
