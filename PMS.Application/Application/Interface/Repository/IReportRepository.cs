using PMS.Common.DTOs.Report;

namespace PMS.Application.Application.Interface.Repository;

public interface IReportRepository
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime from, DateTime to);
    Task<List<TopMedicineDto>> GetTopMedicinesAsync(DateTime from, DateTime to, int top = 10);
    Task<List<TopManufacturerDto>> GetTopManufacturersAsync(DateTime from, DateTime to, int top = 10);
    Task<List<TopSupplierDto>> GetTopSuppliersAsync(int top = 10);
}
