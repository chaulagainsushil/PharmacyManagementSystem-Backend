namespace PMS.Common;

/// <summary>
/// Central definition of every ASP.NET Identity role used in PMS.
/// Use these constants in [Authorize(Roles = ...)] attributes and
/// anywhere a role name string is needed to avoid magic strings.
/// </summary>
public static class AppRoles
{
    /// <summary>Full system access — can create/manage all users and pharmacy accounts.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>Manages a single pharmacy: staff, medicines, settings.</summary>
    public const string PharmacyAdmin = "PharmacyAdmin";

    /// <summary>Can process sales, manage batches, and dispose of expired medicine.</summary>
    public const string Pharmacist = "Pharmacist";

    /// <summary>Front-desk staff: can create sales and manage customers only.</summary>
    public const string Receptionist = "Receptionist";

    /// <summary>All role names as an array — used for seeding.</summary>
    public static readonly string[] All =
    [
        SystemAdmin,
        PharmacyAdmin,
        Pharmacist,
        Receptionist
    ];
}
