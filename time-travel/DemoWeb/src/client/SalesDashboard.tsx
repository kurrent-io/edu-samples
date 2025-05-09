import styles from "./SalesDashboard.module.css"
import {
  SalesRegion,
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
  const [selectedReportIndex, setSelectedReportIndex] = useState<number | null>(
    null,
  )

  const previousReport =
    selectedReportIndex !== null && selectedReportIndex > 0
      ? salesData[selectedReportIndex - 1]
      : null

  const selectedReport =
    selectedReportIndex !== null ? salesData[selectedReportIndex] : null

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
        setSelectedReportIndex(0)
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
          <TimeSliderSection
            salesData={salesData}
            selectedReport={selectedReport}
            setSelectedReportIndex={setSelectedReportIndex}
          />
          <DashboardContent
            previousReport={previousReport}
            salesReport={selectedReport}
          />
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
  setSelectedReportIndex: (index: number | null) => void
}

const TimeSliderSection = ({
  salesData,
  selectedReport,
  setSelectedReportIndex,
}: TimeSelectorProps) => (
  <div className={styles.timeSliderSection}>
    {selectedReport && (
      <span className={styles.timeSliderSectionHeader}>
        Viewing sales report from {selectedReport.reportDate}
      </span>
    )}
    <TimeSlider
      salesData={salesData}
      setSelectedReportIndex={setSelectedReportIndex}
    />
  </div>
)

interface TimeSliderProps {
  salesData: SalesReadModel
  setSelectedReportIndex: (index: number | null) => void
}

const TimeSlider = ({ setSelectedReportIndex, salesData }: TimeSliderProps) => {
  const firstReportDate = salesData[0].reportDate
  const lastReportDate = salesData[salesData.length - 1].reportDate

  return (
    <div className={styles.timeSliderContainer}>
      <span className={styles.timeSliderLabel}>{firstReportDate}</span>
      <input
        type="range"
        min={0}
        max={salesData.length - 1}
        step={1}
        defaultValue={0}
        onChange={(e) =>
          setSelectedReportIndex(Number.parseInt(e.target.value))
        }
      />
      <span className={styles.timeSliderLabel}>{lastReportDate}</span>
    </div>
  )
}

interface DashboardContentProps {
  salesReport: SalesReport
  previousReport: SalesReport | null
}

const DashboardContent = ({
  salesReport,
  previousReport,
}: DashboardContentProps) => (
  <div className={styles.dashboardContent}>
    <SalesDataDashboard
      salesReport={salesReport}
      previousReport={previousReport}
    />
    <EventStream events={salesReport.events || []} />
  </div>
)

interface SalesDataDashboardProps {
  salesReport: SalesReport
  previousReport: SalesReport | null
}

const SalesDataDashboard = ({
  salesReport,
  previousReport,
}: SalesDataDashboardProps) => (
  <div className={styles.salesData}>
    <span className={styles.sectionTitle}>Sales Data</span>
    <SalesTable salesReport={salesReport} previousReport={previousReport} />
  </div>
)

interface SalesTableProps {
  salesReport: SalesReport
  previousReport: SalesReport | null
}

const SalesTable = ({ salesReport, previousReport }: SalesTableProps) => {
  const previousReportDataByCategory = previousReport
    ? _.keyBy(previousReport.salesData, (e) => e.category)
    : {}

  return (
    <table className={styles.salesTable}>
      <thead>
        <tr>
          <th scope="col">Category</th>
          <th scope="col">Region</th>
          <th scope="col">Daily Sales</th>
          <th scope="col">Target Sales</th>
          <th scope="col">Total Monthly Sales</th>
        </tr>
      </thead>
      <tbody>
        {salesReport.salesData.map((salesEntry) => (
          <SalesCategory
            key={salesEntry.category}
            salesDataEntry={salesEntry}
            previousEntry={previousReportDataByCategory[salesEntry.category]}
          />
        ))}
      </tbody>
    </table>
  )
}

interface SalesCategoryProps {
  salesDataEntry: SalesDataEntry
  previousEntry?: SalesDataEntry
}

const SalesCategory = ({
  salesDataEntry,
  previousEntry,
}: SalesCategoryProps) => {
  const { category, ...regionalReports } = salesDataEntry
  const regionPairs = Object.entries(regionalReports)

  return regionPairs.map(([region, regionalSalesData], i) => {
    const { dailySales, targetSales, totalMonthlySales } = regionalSalesData
    const previousRegionalSales = previousEntry?.[region as SalesRegion]

    const salesIncreased =
      previousRegionalSales && dailySales > previousRegionalSales.dailySales

    const salesDecreased =
      previousRegionalSales && dailySales < previousRegionalSales.dailySales

    const arrow = salesIncreased ? "↑" : salesDecreased ? "↓" : ""

    const arrowClassName = salesIncreased
      ? styles.arrowSalesIncreased
      : salesDecreased
        ? styles.arrowSalesDecreased
        : undefined

    return (
      <tr key={region}>
        {i === 0 && (
          <td scope="row" rowSpan={regionPairs.length}>
            {category}
          </td>
        )}
        <td>{region}</td>
        <td>
          <span>${dailySales}</span>
          <span className={arrowClassName}>{arrow}</span>
        </td>
        <td>${targetSales}</td>
        <td>
          <SalesProgressBar
            totalMonthlySales={totalMonthlySales}
            targetSales={targetSales}
          />
        </td>
      </tr>
    )
  })
}

interface SalesProgressBarProps {
  totalMonthlySales: number
  targetSales: number
}

const SalesProgressBar = ({
  totalMonthlySales,
  targetSales,
}: SalesProgressBarProps) => {
  const percentage = (totalMonthlySales / targetSales) * 100
  const innerClassName =
    percentage >= 100 ? styles.progressBarSuccess : styles.progressBarFailure

  return (
    <div className={styles.salesProgressBar}>
      <div className={innerClassName} style={{ width: `${percentage}%` }}>
        ${totalMonthlySales} ({percentage.toFixed(2)}%)
      </div>
    </div>
  )
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
