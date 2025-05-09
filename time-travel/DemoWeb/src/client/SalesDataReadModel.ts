export type SalesReadModel = SalesReport[]

export interface SalesReport {
  reportDate: string
  events?: SalesEvent[]
  salesData: SalesDataEntry[]
}

export interface RegionalSalesData {
  dailySales: number
  targetSales: number
  totalMonthlySales: number
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

export enum SalesRegion {
  Asia = "Asia",
  Europe = "Europe",
  "North America" = "North America",
  "Middle East" = "Middle East",
}

export type SalesDataEntry = {
  [region in SalesRegion]: RegionalSalesData
} & {
  category: string
}
