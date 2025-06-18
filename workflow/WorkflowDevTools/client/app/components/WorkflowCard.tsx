import { Card } from "@chakra-ui/react/card"
import styles from "./WorkflowCard.module.css"
import classNames from "classnames"
import { StepStatus, StepType } from "./Step"

interface WorkflowCardProps {
  stepType: StepType
  stepDescription: string
  status: StepStatus
}

const WorkflowCard = ({
  stepType,
  status,
  stepDescription,
}: WorkflowCardProps) => {
  return (
    <Card.Root
      className={classNames(styles.workflowCard, cardClassNames[stepType])}
    >
      <span className={styles.stepTypeLabel}>{stepType}</span>
      <Card.Header className={styles.stepDescription}>
        <div className={styles.stepDescriptionText}>{stepDescription}</div>
      </Card.Header>
      <span className={styles.stepStatus}>{status}</span>
    </Card.Root>
  )
}

const cardClassNames: Record<StepType, string> = {
  [StepType.Action]: styles.actionCard,
  [StepType.Event]: styles.eventCard,
  [StepType.Subscription]: styles.subscriptionCard,
}

export default WorkflowCard
