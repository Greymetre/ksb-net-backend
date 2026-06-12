using Application.DTOs.CityAssignments;
using Shared.Responses;

namespace Application.Interfaces.Services;

public interface ICityAssignmentService
{
    Task<LaravelApiResponse> GetAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken);
    Task<LaravelApiResponse> GetOptionsAsync(ulong? actorUserId, CancellationToken cancellationToken);
    Task<LaravelApiResponse> SaveAssignmentAsync(CityAssignmentRequestDto request, CancellationToken cancellationToken);
    Task<LaravelApiResponse> DeleteAssignmentAsync(ulong id, CancellationToken cancellationToken);
    Task<CityAssignmentFileDto> ExportAssignmentsAsync(CityAssignmentFilterDto filter, CancellationToken cancellationToken);
    Task<CityAssignmentFileDto> GetTemplateAsync(CancellationToken cancellationToken);
    Task<LaravelApiResponse> UploadAssignmentsAsync(Stream fileStream, CancellationToken cancellationToken);
}
