export type Region = string;
export type Category = string;
export type ReportDate = string;

// Root type for deserialization
export interface ReportReadModel {
  checkpoint: number;
  salesReports: Record<ReportDate, SalesReport>;
}

export interface SalesReport {
  categories: Record<Category, CategorySalesReport>;
}

export interface CategorySalesReport {
  regions: Record<Region, RegionSalesReport>;
}

export interface RegionSalesReport {
  dailySales: number;
  monthEndSalesTarget: number;
  totalMonthlySales: number;
  targetHitRate: number;
}
