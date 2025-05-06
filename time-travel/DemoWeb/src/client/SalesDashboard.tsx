import styles from "./SalesDashboard.module.css"
import {
  SalesDataEntry,
  SalesEvent,
  SalesReadModel,
  SalesReport,
} from "./SalesDataReadModel"
import { useEffect, useState } from "react"
import _ from "lodash"

const READ_MODEL_ENDPOINT = "/api/sales-data"

const SalesDashboard = () => {
  const [salesData, setSalesData] = useState<SalesReadModel>([])
  const [selectedReport, setSelectedReport] = useState<SalesReport | null>(null)

  useEffect(() => {
    fetch(READ_MODEL_ENDPOINT, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => response.json())
      .then((data) => {
        setSalesData(data.salesData)
        setSelectedReport(data.salesData[0])
      })
      .catch((error) =>
        console.error("Error fetching sales data from the server", error),
      )
  }, [])

  return (
    <div className={styles.pageRoot}>
      <Header />
      {!!selectedReport && (
        <>
          <TimeSlider />
          <DashboardContent salesReport={selectedReport} />
        </>
      )}
    </div>
  )
}

const Header = () => (
  <div className={styles.header}>
    <h3>E-Commerce Sales Report</h3>
  </div>
)

const TimeSlider = () => <></>

const DashboardContent = ({ salesReport }: { salesReport: SalesReport }) => (
  <div className={styles.dashboardContent}>
    <SalesDataDashboard salesReport={salesReport} />
    <EventStream events={salesReport.events || []} />
  </div>
)

const SalesDataDashboard = ({ salesReport }: { salesReport: SalesReport }) => (
  <div className={styles.salesData}>
    <h4>Sales Data</h4>
    <SalesTable salesReport={salesReport} />
  </div>
)

const SalesTable = ({ salesReport }: { salesReport: SalesReport }) => {
  const { category, ...regionalReports } = salesReport.salesData[0]
  return (
    <table className={styles.salesTable}>
      <thead>
        <tr>
          <th scope="col">Category</th>
          <th scope="col">Region</th>
          <th scope="col">Daily Sales</th>
          <th scope="col">Target Sales</th>
          <th scope="col">Total Sales</th>
        </tr>
      </thead>
      <tbody>
        {salesReport.salesData.map((salesEntry) => (
          <SalesCategory salesEntry={salesEntry} />
        ))}
      </tbody>
    </table>
  )
}

const SalesCategory = ({ salesEntry }: { salesEntry: SalesDataEntry }) => {
  const { category, ...regionalReports } = salesEntry
  const regionPairs = Object.entries(regionalReports)

  return regionPairs.map(([region, regionalSalesData], i) => (
    <tr key={region}>
      {i === 0 && (
        <th scope="row" rowSpan={regionPairs.length}>
          {category}
        </th>
      )}
      <td>{region}</td>
      <td>{regionalSalesData.dailySales}</td>
      <td>{regionalSalesData.targetSales}</td>
      <td>{regionalSalesData.totalSales}</td>
    </tr>
  ))
}

const EventStream = ({ events }: { events: SalesEvent[] }) => (
  <div className={styles.eventStream}>
    <h4>Event Stream</h4>
    {events.map((event) => (
      <EventCard key={event.eventId} event={event} />
    ))}
  </div>
)

const EventCard = ({ event }: { event: SalesEvent }) => {
  const {
    eventType,
    timestamp,
    aggregateId,
    eventId,
    version,
    category,
    ...eventData
  } = event

  const eventDataPairs = Object.entries(eventData)

  return (
    <div className={styles.eventCard}>
      <span className={styles.eventType}>{event.eventType}</span>
      <span className={styles.eventTimestamp}>{event.timestamp}</span>
      {eventDataPairs.map(([key, value]) => (
        <span className={styles.eventField}>
          {_.startCase(key)}: {value}
        </span>
      ))}
    </div>
  )
}

export default SalesDashboard
