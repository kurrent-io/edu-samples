export type SalesReadModel = SalesReport[]

export interface SalesReport {
  reportDate: string
  events?: SalesEvent[]
  salesData: SalesDataEntry[]
}

export interface RegionalSalesData {
  dailySales: number
  targetSales: number
  totalSales: number
  targetHitRate: number
}

export interface SalesEvent {
  eventType: string
  eventId: string
  timestamp: string
  region: string
  category: string
  aggregateId: string
  version: number
  orderAmount?: number
  reason?: string
  quantity?: number
}

export interface SalesDataEntry {
  category: string
  Asia: RegionalSalesData
  Europe: RegionalSalesData
  "North America": RegionalSalesData
  "Middle East": RegionalSalesData
}
