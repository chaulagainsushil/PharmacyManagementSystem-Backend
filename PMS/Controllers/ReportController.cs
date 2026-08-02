using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMS.Application.Application.Interface.Service;
using PMS.Common.DTOs.Report;

namespace PMS.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _service;

    public ReportController(IReportService service) => _service = service;

    /// <summary>Full dashboard report: today, this month, top medicines/manufacturers/suppliers.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard()
        => Ok(await _service.GetDashboardReportAsync());

    /// <summary>Sales summary for today.</summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(SalesSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Today()
        => Ok(await _service.GetTodaySalesAsync());

    /// <summary>Sales summary for the current calendar month.</summary>
    [HttpGet("this-month")]
    [ProducesResponseType(typeof(SalesSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ThisMonth()
        => Ok(await _service.GetMonthSalesAsync());

    /// <summary>Top-selling medicines (all-time, up to 10).</summary>
    [HttpGet("top-medicines")]
    [ProducesResponseType(typeof(List<TopMedicineDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopMedicines([FromQuery] int top = 10)
        => Ok(await _service.GetTopMedicinesAsync(top));

    /// <summary>Top manufacturers by revenue (all-time, up to 10).</summary>
    [HttpGet("top-manufacturers")]
    [ProducesResponseType(typeof(List<TopManufacturerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopManufacturers([FromQuery] int top = 10)
        => Ok(await _service.GetTopManufacturersAsync(top));

    /// <summary>Top suppliers by tablets supplied (all-time, up to 10).</summary>
    [HttpGet("top-suppliers")]
    [ProducesResponseType(typeof(List<TopSupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TopSuppliers([FromQuery] int top = 10)
        => Ok(await _service.GetTopSuppliersAsync(top));
}
