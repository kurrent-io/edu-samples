export default interface SalesEvent {
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