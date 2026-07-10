using CLS.Budget.Api.Auth;
using CLS.Budget.Application.Abstractions.Services;
using CLS.Budget.Application.Common;
using CLS.Budget.Application.TransactionImports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLS.Budget.Api.Controllers;

[ApiController]
[Route("api/v1/transaction-imports")]
[Authorize(Policy = AuthorizationPolicies.TenantMember)]
public class TransactionImportsController(ITransactionImportService transactionImportService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TransactionImportSummaryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await transactionImportService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await transactionImportService.GetByIdAsync(id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest(ApiResponse<TransactionImportDetailResponse>.Fail("An empty file was uploaded."));
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<TransactionImportDetailResponse>.Fail("Only CSV files are supported."));
        }

        await using var stream = file.OpenReadStream();
        var result = await transactionImportService.UploadCsvAsync(stream, file.FileName, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Data!.TransactionImportId },
            result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<TransactionImportDetailResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateImport(
        int id,
        [FromBody] UpdateTransactionImportRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transactionImportService.UpdateImportAsync(id, request, cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("transactions/{importedTransactionId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ImportedTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ImportedTransactionResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<ImportedTransactionResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateImportedTransaction(
        int importedTransactionId,
        [FromBody] UpdateImportedTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transactionImportService.UpdateImportedTransactionAsync(
            importedTransactionId,
            request,
            cancellationToken);
        if (!result.Success)
        {
            return result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase))
                ? NotFound(result)
                : BadRequest(result);
        }

        return Ok(result);
    }
}
