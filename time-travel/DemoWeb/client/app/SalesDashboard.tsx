import styles from "./SalesDashboard.module.css"
import {
  Category,
  CategorySalesReport,
  Region,
  ReportReadModel,
  SalesReport,
} from "./ReportReadModel"
import { useEffect, useMemo, useState } from "react"
import _ from "lodash"
import SalesEvent from "./SalesEvent"
import classNames from "classnames"

const READ_MODEL_ENDPOINT = "/api/sales-data"
const EVENTS_ENDPOINT = "/api/events"
const EVENT_STREAM = "$et-order-placed"

enum SalesFigureType {
  DailySales = 0,
  TotalMonthlySales = 1,
}

const SalesDashboard = () => {
  const [reportReadModel, setReportReadModel] =
    useState<ReportReadModel | null>(null)

  const [selectedReportDate, setSelectedReportDate] = useState<string | null>(
    null,
  )

  const [selectedTableCell, setSelectedTableCell] =
    useState<SelectedTableCell | null>(null)

  const [error, setError] = useState<string | null>(null)

  const previousReportDate = selectedReportDate
    ? getPreviousDay(selectedReportDate)
    : null

  const previousReport =
    previousReportDate && reportReadModel
      ? reportReadModel.salesReports[previousReportDate]
      : null

  const selectedReport =
    selectedReportDate !== null && reportReadModel
      ? reportReadModel.salesReports[selectedReportDate]
      : null

  useEffect(() => {
    setError(null)
    fetch(READ_MODEL_ENDPOINT, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => response.json())
      .then((data: ReportReadModel) => {
        setReportReadModel(data)
        const earliestReportDate = getEarliestReportDate(data)
        if (earliestReportDate) setSelectedReportDate(earliestReportDate)
      })
      .catch((e) => {
        const errorMessage = "Error fetching sales data from the server"
        console.error(errorMessage, e)
        setError(`${errorMessage}: ${e.message}`)
      })
  }, [])

  return (
    <div className={styles.pageRoot}>
      <Header />
      {error && <ErrorAlert error={error} />}
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
            selectedTableCell={selectedTableCell}
            setSelectedTableCell={setSelectedTableCell}
            selectedDate={selectedReportDate}
            checkpoint={reportReadModel.checkpoint}
          />
        </>
      )}
    </div>
  )
}

const ErrorAlert = ({ error }: { error: string }) => (
  <div className={styles.errorAlert}>{error}</div>
)

const toDateString = (date: Date) => date.toISOString().split("T")[0]

const getPreviousDay = (dateString: string): string => {
  const date = new Date(dateString)
  date.setDate(date.getDate() - 1)
  return toDateString(date)
}

const getEarliestReportDate = (
  readModel: ReportReadModel,
): string | undefined => {
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
  return (
    <div className={styles.timeSliderSection}>
      {selectedReportDate && (
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
  selectedTableCell: SelectedTableCell | null
  setSelectedTableCell: (selectedTableCell: SelectedTableCell | null) => void
  checkpoint: number
  selectedDate: string | null
}

const DashboardContent = ({
  salesReport,
  previousReport,
  selectedTableCell,
  setSelectedTableCell,
  checkpoint,
  selectedDate,
}: DashboardContentProps) => (
  <div className={styles.dashboardContent}>
    <SalesDataDashboard
      salesReport={salesReport}
      previousReport={previousReport}
      selectedTableCell={selectedTableCell}
      setSelectedTableCell={setSelectedTableCell}
    />
    <EventStream
      selectedTableCell={selectedTableCell}
      checkpoint={checkpoint}
      selectedDate={selectedDate}
    />
  </div>
)

interface SalesDataDashboardProps {
  salesReport: SalesReport
  previousReport: SalesReport | null
  selectedTableCell: SelectedTableCell | null
  setSelectedTableCell: (selectedTableCell: SelectedTableCell | null) => void
}

const SalesDataDashboard = ({
  salesReport,
  previousReport,
  selectedTableCell,
  setSelectedTableCell,
}: SalesDataDashboardProps) => (
  <div className={styles.salesData}>
    <span className={styles.sectionTitle}>Sales Data</span>
    <SalesTable
      salesReport={salesReport}
      previousReport={previousReport}
      selectedTableCell={selectedTableCell}
      setSelectedTableCell={setSelectedTableCell}
    />
  </div>
)

interface SalesTableProps {
  salesReport: SalesReport
  previousReport: SalesReport | null
  selectedTableCell: SelectedTableCell | null
  setSelectedTableCell: (selectedTableCell: SelectedTableCell | null) => void
}

const SalesTable = ({
  salesReport,
  previousReport,
  selectedTableCell,
  setSelectedTableCell,
}: SalesTableProps) => {
  return (
    <table className={styles.salesTable}>
      <thead>
        <tr>
          <th scope="col">Category</th>
          <th scope="col">Region</th>
          <th className={styles.dailySalesCol} scope="col">
            Daily Sales
          </th>
          <th className={styles.targetSalesCol} scope="col">
            Target Sales
          </th>
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
              previousCategorySalesReport={previousReport?.categories[category]}
              selectedTableCell={selectedTableCell}
              setSelectedTableCell={setSelectedTableCell}
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
  selectedTableCell: SelectedTableCell | null
  setSelectedTableCell: (selectedTableCell: SelectedTableCell | null) => void
}

const SalesCategory = ({
  category,
  categorySalesReport,
  previousCategorySalesReport,
  selectedTableCell,
  setSelectedTableCell,
}: SalesCategoryProps) => {
  const { regions } = categorySalesReport
  const regionPairs = Object.entries(regions)

  return regionPairs.map(([region, regionalSalesData], i) => {
    const { dailySales, monthEndSalesTarget, totalMonthlySales } =
      regionalSalesData
    const previousRegionalSales = previousCategorySalesReport?.regions[region]

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

    const rowClassName =
      selectedTableCell?.category === category &&
      selectedTableCell?.region === region
        ? styles.selectedRow
        : undefined

    const getOnSalesFigureClick = (salesFigureType: SalesFigureType) => () => {
      if (
        selectedTableCell?.salesFigureType === salesFigureType &&
        selectedTableCell?.region === region &&
        selectedTableCell?.category === category
      ) {
        setSelectedTableCell(null)
      } else {
        setSelectedTableCell({
          category,
          region,
          salesFigureType,
        })
      }

      return false
    }

    const categoryAndRegionSelected =
      selectedTableCell?.region === region &&
      selectedTableCell?.category === category

    return (
      <tr key={region} className={rowClassName}>
        {i === 0 && (
          <td scope="row" rowSpan={regionPairs.length}>
            {category}
          </td>
        )}
        <td>{region}</td>
        <td
          className={classNames(styles.dailySalesCol, {
            [styles.selectedCell]:
              categoryAndRegionSelected &&
              selectedTableCell?.salesFigureType === SalesFigureType.DailySales,
          })}
        >
          <span
            className={styles.buttonLink}
            onClick={getOnSalesFigureClick(SalesFigureType.DailySales)}
          >
            ${dailySales}
          </span>
          <span className={arrowClassName}>{arrow}</span>
        </td>
        <td className={styles.targetSalesCol}>${monthEndSalesTarget}</td>
        <td
          className={classNames(styles.salesProgressBarCell, {
            [styles.selectedCell]:
              categoryAndRegionSelected &&
              selectedTableCell?.salesFigureType ===
                SalesFigureType.TotalMonthlySales,
          })}
          onClick={getOnSalesFigureClick(SalesFigureType.TotalMonthlySales)}
        >
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
  const percentage =
    targetSales === 0 ? 100 : (totalMonthlySales / targetSales) * 100
  const innerClassName =
    percentage >= 100 ? styles.progressBarSuccess : styles.progressBarFailure

  return (
    <div className={styles.salesProgressBar}>
      <div className={innerClassName} style={{ width: `${percentage}%` }} />
      <span className={styles.progressBarLabel}>
        ${totalMonthlySales} ({percentage.toFixed(2)}%)
      </span>
    </div>
  )
}

interface EventStreamProps {
  selectedTableCell: SelectedTableCell | null
  checkpoint: number
  selectedDate: string | null
}

const EventStream = ({
  selectedTableCell,
  selectedDate,
  checkpoint,
}: EventStreamProps) => {
  const [events, setEvents] = useState<SalesEvent[]>([])

  useEffect(() => {
    if (!selectedTableCell || !selectedDate) {
      setEvents([])
      return
    }

    const params: EventQueryParams = {
      ...selectedTableCell,
      checkpoint,
      date: selectedDate,
    }

    const queryString = Object.entries(params)
      .map(([key, value]) => `${key}=${encodeURIComponent(value)}`)
      .join("&")

    fetch(`${EVENTS_ENDPOINT}?${queryString}`, {
      method: "GET",
      headers: {
        "Content-Type": "application/json",
      },
    })
      .then((response) => response.json())
      .then((data: SalesEvent[]) => {
        setEvents(data)
      })
      .catch((error) =>
        console.error("Error fetching events from the server", error),
      )
  }, [selectedTableCell, selectedDate, checkpoint])

  return (
    <div className={styles.eventStream}>
      <span className={styles.sectionTitle}>Event Stream</span>
      {events.map((event) => (
        <EventCard key={event.eventNumber} event={event} />
      ))}
    </div>
  )
}

const EventCard = ({ event }: { event: SalesEvent }) => {
  const { category, region, eventNumber, at, totalSalesForCategory } = event

  const eventLink = `http://${window.location.hostname}:2113/web/index.html#/streams/$et-order-placed/${eventNumber}`

  return (
    <div className={styles.eventCard}>
      <a
        className={classNames(styles.eventCardHeader, styles.buttonLink)}
        href={eventLink}
        target="_blank"
      >
        Event #{eventNumber} in {EVENT_STREAM}
      </a>
      <span>
        <EventCardPair label="Date" value={new Date(at).toISOString()} />
        &nbsp;|&nbsp;
        <EventCardPair label="Region" value={region} />
      </span>
      <EventCardPair label="Category" value={category} />
      <EventCardPair
        label="Total Sales"
        value={`USD ${totalSalesForCategory}`}
      />
    </div>
  )
}

interface EventCardPairProps {
  label: string
  value: string
}

const EventCardPair = ({ label, value }: EventCardPairProps) => {
  return (
    <span className={styles.eventCardPair}>
      <span className={styles.eventCardPairLabel}>{label}: </span>
      <span className={styles.eventCardPairValue}>{value}</span>
    </span>
  )
}

interface EventQueryParams {
  checkpoint: number
  category: string
  region: string
  date: string
  salesFigureType: SalesFigureType
}

interface SelectedTableCell {
  category: Category
  region: Region
  salesFigureType: SalesFigureType
}

export default SalesDashboard
