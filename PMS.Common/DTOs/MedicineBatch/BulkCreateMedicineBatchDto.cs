using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.MedicineBatch;

public class BulkCreateMedicineBatchDto
{
    [Required, MinLength(1, ErrorMessage = "At least one batch is required.")]
    public List<CreateMedicineBatchDto> Batches { get; set; } = new();
}

public class BulkBatchResultItemDto
{
    public int Index { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public MedicineBatchResponseDto? Data { get; set; }
}

public class BulkCreateMedicineBatchResultDto
{
    public int TotalRequested { get; set; }
    public int TotalCreated { get; set; }
    public int TotalFailed { get; set; }
    public List<BulkBatchResultItemDto> Results { get; set; } = new();
}
