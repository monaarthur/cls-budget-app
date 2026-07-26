using CLS.Budget.Application.Common;
using CLS.Budget.Application.CreditCardEngine.Dtos;

namespace CLS.Budget.Application.Abstractions.Services;

public interface ICreditCardDecisionService
{
    Task<ApiResponse<CalculationEnvelope<LoanScheduleResultDto>>> BuildLoanScheduleAsync(
        LoanScheduleRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<CompareLoanSavingsResultDto>>> CompareLoanSavingsAsync(
        CompareLoanSavingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<ComparePayoffPlansResultDto>>> ComparePayoffPlansAsync(
        ComparePayoffPlansRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<SavedPayoffPlanDto>>> ListSavedPayoffPlansAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SavedPayoffPlanDto>> CreateSavedPayoffPlanAsync(
        SavePayoffPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<SavedPayoffPlanDto>> UpdateSavedPayoffPlanAsync(
        int savedPayoffPlanId,
        UpdateSavedPayoffPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object?>> DeleteSavedPayoffPlanAsync(
        int savedPayoffPlanId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<CompareSavedPayoffPlansResultDto>>> CompareSavedPayoffPlansAsync(
        CompareSavedPayoffPlansRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<InterestAnalysisResultDto>>> AnalyzeInterestAsync(
        int creditCardId,
        InterestAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<UtilizationSummaryResultDto>>> GetUtilizationSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<BalanceTransferAnalysisResultDto>>> AnalyzeBalanceTransferAsync(
        AnalyzeBalanceTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<CashFlowAnalysisResultDto>>> AnalyzeCashFlowAsync(
        AnalyzeCashFlowRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<ForecastResultDto>>> CreateForecastAsync(
        CreateForecastRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CalculationEnvelope<ForecastResultDto>>> GetForecastAsync(
        int forecastId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object?>> DeleteForecastAsync(
        int forecastId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanDto>> ActivatePayoffPlanAsync(
        ActivatePayoffPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanDto>> GetActivePayoffPlanAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanDto>> ReviseActivePayoffPlanAsync(
        ReviseActivePayoffPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PayoffPlanPaymentDto>> RecordActivePayoffPlanPaymentAsync(
        RecordPayoffPlanPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object?>> VoidActivePayoffPlanPaymentAsync(
        int payoffPlanPaymentId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanDto>> CompleteActivePayoffPlanAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanDto>> AbandonActivePayoffPlanAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanHistoryDto>> GetActivePayoffPlanHistoryAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<ActivePayoffPlanProgressDto>> GetActivePayoffPlanProgressAsync(
        CancellationToken cancellationToken = default);
}
