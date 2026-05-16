using MoteaManageAPI.Models;

namespace MoteaManageAPI.Services;

public interface IProjectRepository
{
    Task<IReadOnlyList<ProjectDto>> GetAllAsync(string userId, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(string userId, CreateProjectRequest request, CancellationToken cancellationToken = default);
}
