using System.ComponentModel.DataAnnotations;

namespace PMS.Common.DTOs.MedicineBatch;

/// <summary>
/// Request DTO for receiving a new medicine batch (purchase / GRN).
/// <para>
/// Two entry modes are supported:
/// <list type="bullet">
///   <item>
///     <b>Unit-aware mode</b> (recommended): set <see cref="PurchaseMedicineUnitId"/> to a
///     MedicineUnit with <c>CanPurchase = true</c> and provide the quantity in that unit via
///     <see cref="PurchaseQuantity"/>.  The service will convert to base units automatically.
///     Example: PurchaseMedicineUnitId = Strip (1 Strip = 10 Tablets), PurchaseQuantity = 50 →
///     QuantityInBaseUnit stored = 500.
///   </item>
///   <item>
///     <b>Base-unit mode</b> (legacy / direct entry): leave <see cref="PurchaseMedicineUnitId"/>
///     null and supply <see cref="PurchaseQuantity"/> directly in base units.
///   </item>
/// </list>
/// </para>
/// <para>
/// <see cref="CostPricePerBaseUnit"/> is always stored per base unit.
/// When entering in unit-aware mode pass the cost per purchase unit in
/// <see cref="CostPricePerPurchaseUnit"/> — the service will convert to cost per base unit.
/// If you already know the cost per base unit pass it in <see cref="CostPricePerBaseUnit"/>
/// directly (and leave <see cref="CostPricePerPurchaseUnit"/> = 0).
/// </para>
/// </summary>
public class CreateMedicineBatchDto
{
    [Required(ErrorMessage = "MedicineId is required.")]
    public int MedicineId { get; set; }

    [Required(ErrorMessage = "Batch number is required.")]
    [MaxLength(100, ErrorMessage = "Batch number must not exceed 100 characters.")]
    public string BatchNumber { get; set; } = string.Empty;

    public int? SupplierId { get; set; }

    public DateTime? ManufactureDate { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// The MedicineUnit in which stock is being received (must have CanPurchase = true).
    /// When null the quantity is interpreted as base units.
    /// </summary>
    public int? PurchaseMedicineUnitId { get; set; }

    /// <summary>
    /// Quantity in the purchase unit (or base unit when PurchaseMedicineUnitId is null).
    /// Must be at least 1.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int PurchaseQuantity { get; set; }

    /// <summary>
    /// Cost per one purchase unit (e.g. cost per Strip).
    /// The service converts this to CostPricePerBaseUnit when PurchaseMedicineUnitId is set.
    /// Ignored when PurchaseMedicineUnitId is null — use <see cref="CostPricePerBaseUnit"/> instead.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPricePerPurchaseUnit must be non-negative.")]
    public decimal CostPricePerPurchaseUnit { get; set; } = 0m;

    /// <summary>
    /// Cost per one base-unit item (e.g. cost per Tablet).
    /// Used directly when PurchaseMedicineUnitId is null.
    /// When PurchaseMedicineUnitId is set this value is ignored and the service
    /// calculates it from CostPricePerPurchaseUnit / ConversionFactorToBase.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "CostPricePerBaseUnit must be non-negative.")]
    public decimal CostPricePerBaseUnit { get; set; } = 0m;

    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
}
