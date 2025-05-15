export type Region = string;
export type Category = string;
export type ReportDate = string;

// Root type for deserialization
export interface ReportReadModel {
  checkpoint: number;
  salesReports: Record<ReportDate, SalesReport>;
}

export interface SalesReport {
  categorySalesReports: Record<Category, CategorySalesReport>;
}

export interface CategorySalesReport {
  regionSalesReports: Record<Region, RegionSalesReport>;
}

export interface RegionSalesReport {
  dailySales: number;
  targetSales: number;
  totalMonthlySales: number;
  targetHitRate: number;
}

export enum SalesRegion {
  Asia = "Asia",
  Europe = "Europe",
  NorthAmerica = "North America",
  MiddleEast = "Middle East",
}