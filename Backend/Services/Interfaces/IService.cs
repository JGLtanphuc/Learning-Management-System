namespace Backend.Services.Interfaces
{
    public interface IService<TResponseDto, in TCreateRequest, in TUpdateRequest>
    {
        Task<TResponseDto?> GetByIdAsync(long id);
        Task<TResponseDto> CreateAsync(TCreateRequest request);
        Task<TResponseDto?> UpdateAsync(long id, TUpdateRequest request);
        Task<bool> DeleteAsync(long id);
    }
}

