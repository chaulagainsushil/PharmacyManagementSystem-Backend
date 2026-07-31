namespace PMS.Common.DTOs.Medicine;

/// <summary>
/// Result for a single row in a bulk-create request.
/// </summary>
public class BulkCreateMedicineItemResultDto
{
    /// <summary>0-based index of this item in the original request list.</summary>
    public int Index { get; set; }

    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>Populated only when Success = true.</summary>
    public MedicineResponseDto? Data { get; set; }
}

/// <summary>
/// Top-level response for POST /api/medicine/bulk.
/// </summary>
public class BulkCreateMedicineResultDto
{
    public int TotalRequested { get; set; }
    public int TotalCreated { get; set; }
    public int TotalFailed { get; set; }
    public IReadOnlyList<BulkCreateMedicineItemResultDto> Results { get; set; } = [];
}
