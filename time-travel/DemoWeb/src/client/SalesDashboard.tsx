import styles from "./SalesDashboard.module.css"
import {
  SalesDataEntry,
  SalesEvent,
  SalesReadModel,
  SalesReport,
} from "./SalesDataReadModel"
import { useEffect, useMemo, useState } from "react"
import _, { first } from "lodash"

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
      {!!selectedReport && !!salesData && (
        <>
          <TimeSelector
            salesData={salesData}
            selectedReport={selectedReport}
            setSelectedReport={setSelectedReport}
          />
          <DashboardContent salesReport={selectedReport} />
        </>
      )}
    </div>
  )
}

const Header = () => (
  <div className={styles.header}>
    <span className={styles.headerTitle}>E-Commerce Sales Report</span>
  </div>
)

interface TimeSelectorProps {
  salesData: SalesReadModel
  selectedReport: SalesReport | null
  setSelectedReport: (report: SalesReport | null) => void
}

const TimeSelector = ({
  salesData,
  selectedReport,
  setSelectedReport,
}: TimeSelectorProps) => (
  <div className={styles.timeSelector}>
    <ReportSelector
      salesData={salesData}
      selectedReport={selectedReport}
      setSelectedReport={setSelectedReport}
    />
    {selectedReport && <TimeSlider selectedReport={selectedReport} />}
  </div>
)

interface TimeSliderProps {
  selectedReport: SalesReport
}

const lastDayOfMonth = (date: Date) => {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0)
}

const dateToString = (date: Date) => date.toLocaleDateString("en-CA")

const TimeSlider = ({ selectedReport }: TimeSliderProps) => {
  const firstDate = new Date(selectedReport.reportDate)
  const lastDate = lastDayOfMonth(firstDate)

  return (
    <div className={styles.timeSliderContainer}>
      <span className={styles.timeSliderLabel}>{dateToString(firstDate)}</span>
      <input type="range" min={0} max={lastDate.getDay()} step={1} />
      <span className={styles.timeSliderLabel}>{dateToString(lastDate)}</span>
    </div>
  )
}

interface ReportSelectorProps {
  salesData: SalesReadModel
  selectedReport: SalesReport | null
  setSelectedReport: (report: SalesReport | null) => void
}

const ReportSelector = ({
  salesData,
  selectedReport,
  setSelectedReport,
}: ReportSelectorProps) => (
  <div className={styles.reportSelector}>
    <label className={styles.reportSelectorLabel} htmlFor="report-selector">
      Select a report:{" "}
    </label>
    <select
      name="report-selector"
      id="report-selector"
      onChange={(e) => {
        setSelectedReport(salesData[e.target.value as any])
      }}
    >
      {salesData.map((report, i) => (
        <option key={i} value={i}>
          {report.reportDate}
        </option>
      ))}
    </select>
  </div>
)

const DashboardContent = ({ salesReport }: { salesReport: SalesReport }) => (
  <div className={styles.dashboardContent}>
    <SalesDataDashboard salesReport={salesReport} />
    <EventStream events={salesReport.events || []} />
  </div>
)

const SalesDataDashboard = ({ salesReport }: { salesReport: SalesReport }) => (
  <div className={styles.salesData}>
    <span className={styles.sectionTitle}>Sales Data</span>
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
          <SalesCategory salesDataEntry={salesEntry} />
        ))}
      </tbody>
    </table>
  )
}

const SalesCategory = ({
  salesDataEntry,
}: {
  salesDataEntry: SalesDataEntry
}) => {
  const { category, ...regionalReports } = salesDataEntry
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
    <span className={styles.sectionTitle}>Event Stream</span>
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
      <span className={styles.eventTimestamp}>
        {new Date(event.timestamp).toLocaleString()}
      </span>
      {eventDataPairs.map(([key, value]) => (
        <span className={styles.eventField}>
          {_.startCase(key)}: {value}
        </span>
      ))}
    </div>
  )
}

export default SalesDashboard
