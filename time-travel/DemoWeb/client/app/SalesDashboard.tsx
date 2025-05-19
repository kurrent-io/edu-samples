import styles from "./SalesDashboard.module.css"
import {
  Category,
  CategorySalesReport,
  ReportReadModel,
  SalesRegion,
  SalesReport,
} from "./ReportReadModel"
import { useEffect, useMemo, useState } from "react"
import _ from "lodash"
import SalesEvent from "./SalesEvent"

const READ_MODEL_ENDPOINT = "/api/sales-data"

const SalesDashboard = () => {
  const [reportReadModel, setReportReadModel] = useState<ReportReadModel | null>(null)
  const [selectedReportDate, setSelectedReportDate] = useState<string | null>(
    null,
  )

  const previousReportDate = selectedReportDate
    ? getPreviousDay(selectedReportDate)
    : null

  const previousReport = previousReportDate
    ? reportReadModel.salesReports[previousReportDate]
    : null

  const selectedReport =
    selectedReportDate !== null
      ? reportReadModel.salesReports[selectedReportDate]
      : null

  useEffect(() => {
    fetch(READ_MODEL_ENDPOINT, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => response.json())
      .then((data: ReportReadModel) => {
        setReportReadModel(data)

        setSelectedReportDate(getEarliestReportDate(data))
      })
      .catch((error) =>
        console.error("Error fetching sales data from the server", error),
      )
  }, [])

  return (
    <div className={styles.pageRoot}>
      <Header />
      {!!selectedReport && !!reportReadModel && (
        <>
          <TimeSliderSection
            selectedReportDate={selectedReportDate}
            reportReadModel={reportReadModel}
            setSelectedReportDate={setSelectedReportDate}
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

const toDateString = (date: Date) => date.toISOString().split("T")[0]

const getPreviousDay = (dateString: string): string => {
  const date = new Date(dateString)
  date.setDate(date.getDate() - 1)
  return toDateString(date)
}

const getEarliestReportDate = (readModel: ReportReadModel): string => {
  const dates = Object.keys(readModel.salesReports)

  return _.minBy(dates, (date) => new Date(date).getTime())
}

const Header = () => (
  <div className={styles.header}>
    <span className={styles.headerTitle}>E-Commerce Sales Report</span>
  </div>
)

interface TimeSelectorProps {
  reportReadModel: ReportReadModel
  selectedReportDate: string | null
  setSelectedReportDate: (dateString: string | null) => void
}

const TimeSliderSection = ({
  reportReadModel,
  setSelectedReportDate,
  selectedReportDate,
}: TimeSelectorProps) => {
  const selectedReport = reportReadModel.salesReports[selectedReportDate]

  return (
    <div className={styles.timeSliderSection}>
      {selectedReport && selectedReportDate && (
        <span className={styles.timeSliderSectionHeader}>
          Viewing sales report from {selectedReportDate}
        </span>
      )}
      <TimeSlider
        reportReadModel={reportReadModel}
        setSelectedReportDate={setSelectedReportDate}
      />
    </div>
  )
}

interface TimeSliderProps {
  reportReadModel: ReportReadModel
  setSelectedReportDate: (dateString: string | null) => void
}

const TimeSlider = ({
  setSelectedReportDate,
  reportReadModel,
}: TimeSliderProps) => {
  const { orderedDates } = useMemo(() => {
    const dates = Object.keys(reportReadModel.salesReports)
    const orderedDates = _.orderBy(dates, (date) => new Date(date).getTime())
    return { orderedDates }
  }, [reportReadModel])

  const firstReportDate = orderedDates[0]
  const lastReportDate = orderedDates[orderedDates.length - 1]

  return (
    <div className={styles.timeSliderContainer}>
      <span className={styles.timeSliderLabel}>{firstReportDate}</span>
      <input
        className={styles.timeSliderInput}
        type="range"
        min={0}
        max={orderedDates.length - 1}
        step={1}
        defaultValue={0}
        onChange={(e) =>
          setSelectedReportDate(orderedDates[Number.parseInt(e.target.value)])
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
    <EventStream />
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
  return (
    <table className={styles.salesTable}>
      <thead>
        <tr>
          <th scope="col">Category</th>
          <th scope="col">Region</th>
          <th className={styles.dailySalesCol} scope="col">Daily Sales</th>
          <th className={styles.targetSalesCol} scope="col">Target Sales</th>
          <th scope="col">Total Monthly Sales</th>
        </tr>
      </thead>
      <tbody>
        {_.map(
          salesReport.categories,
          (categorySalesReport: CategorySalesReport, category: Category) => (
            <SalesCategory
              key={category}
              category={category}
              categorySalesReport={categorySalesReport}
              previousCategorySalesReport={
                previousReport?.categories[category]
              }
            />
          ),
        )}
      </tbody>
    </table>
  )
}

interface SalesCategoryProps {
  category: Category
  categorySalesReport: CategorySalesReport
  previousCategorySalesReport?: CategorySalesReport
}

const SalesCategory = ({
  category,
  categorySalesReport,
  previousCategorySalesReport,
}: SalesCategoryProps) => {
  const { regions } = categorySalesReport
  const regionPairs = Object.entries(regions)

  return regionPairs.map(([region, regionalSalesData], i) => {
    const { dailySales, monthEndSalesTarget, totalMonthlySales } = regionalSalesData
    const previousRegionalSales =
      previousCategorySalesReport?.[region as SalesRegion]

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
        <td className={styles.dailySalesCol}>
          <span>${dailySales}</span>
          <span className={arrowClassName}>{arrow}</span>
        </td>
        <td className={styles.targetSalesCol}>${monthEndSalesTarget}</td>
        <td>
          <SalesProgressBar
            totalMonthlySales={totalMonthlySales}
            targetSales={monthEndSalesTarget}
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
  const percentage = targetSales === 0 ? 100 : (totalMonthlySales / targetSales) * 100
  const innerClassName =
    percentage >= 100 ? styles.progressBarSuccess : styles.progressBarFailure

  return (
    <div className={styles.salesProgressBar}>
      <div className={innerClassName} style={{ width: `${percentage}%` }}/>
      <span className={styles.progressBarLabel}>${totalMonthlySales} ({percentage.toFixed(2)}%)</span>
    </div>
  )
}

const EventStream = () => {
  // TODO: Fetch events from API
  const events = []

  return (
    <div className={styles.eventStream}>
      <span className={styles.sectionTitle}>Event Stream</span>
      {events.map((event) => (
        <EventCard key={event.eventId} event={event} />
      ))}
    </div>
  )
}

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
