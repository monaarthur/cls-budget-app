using CLS.Budget.Application.AutoPayoff.Dtos;
using CLS.Budget.Application.Common;

namespace CLS.Budget.Application.Abstractions.Services;

public interface IAutoPayoffService
{
    Task<ApiResponse<IReadOnlyList<AutoPayoffConfigResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AutoPayoffConfigResponse>> GetConfigAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AutoPayoffConfigResponse>> GetConfigAsync(
        int autoPayoffConfigId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AutoPayoffConfigResponse>> SaveConfigAsync(
        SaveAutoPayoffConfigRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AutoPayoffPreviewResponse>> PreviewAsync(
        SaveAutoPayoffConfigRequest? request,
        CancellationToken cancellationToken = default);
}
