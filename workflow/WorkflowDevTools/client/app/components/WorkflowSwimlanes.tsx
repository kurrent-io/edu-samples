import { Step, StepStatus, StepType } from "./Step"
import { ReactFlow } from "@xyflow/react"
import "@xyflow/react/dist/style.css"
import { useMemo } from "react"
import SwimlaneNode from "./SwimlaneNode"
import WorkflowCard from "./WorkflowCard"
import { styleConstants } from "./styleConstants"

interface WorkflowSwimlanesProps {}

enum FlowNodeType {
  Swimlane = "swimlane",
  Card = "card",
}

export const hardcodedSteps: Step[] = [
  {
    stepType: StepType.Action,
    stepDescription: "Click Checkout",
    swimlaneName: "Store UI",
  },
  {
    stepType: StepType.Action,
    stepDescription: "Reserve Inventory",
    swimlaneName: "Reservation",
  },
  {
    stepType: StepType.Event,
    stepDescription: "Inventory Reserved",
    swimlaneName: "Reservation",
  },
  {
    stepType: StepType.Action,
    stepDescription: "Place Order",
    swimlaneName: "Order",
  },
  {
    stepType: StepType.Event,
    stepDescription: "Order Placed",
    swimlaneName: "Order",
  },
]

interface WorkflowSwimlanesProps {
  steps: Step[]
}

const WorkflowSwimlanes = ({ steps }: WorkflowSwimlanesProps) => {
  const nodeTypes = useMemo(
    () => ({
      [FlowNodeType.Swimlane]: SwimlaneNode,
      [FlowNodeType.Card]: WorkflowCardNode,
    }),
    [],
  )

  const nodes = getNodesFromSteps(steps)

  return (
    <div style={{ height: "100vh", width: "100vw" }}>
      <ReactFlow nodes={nodes} nodeTypes={nodeTypes} />
    </div>
  )
}

const WorkflowCardNode = ({ data }: { data: Step }) => (
  <WorkflowCard
    stepType={data.stepType}
    stepDescription={data.stepDescription}
    // TODO: Use status from state
    status={StepStatus.Started}
  />
)

const getNodesFromSteps = (steps: Step[]) => {
  const swimlaneNames = Array.from(new Set(steps.map((s) => s.swimlaneName)))

  const swimlaneWidth =
    styleConstants.swimlane.swimlaneNameContainerWidth +
    steps.length * styleConstants.workflowCard.minHeight +
    (steps.length - 1) * styleConstants.workflowCard.horizontalSpacing

  const swimlaneNodes = swimlaneNames.map((name, index) => ({
    id: name,
    type: FlowNodeType.Swimlane,
    data: { swimlaneName: name },
    position: { x: 0, y: index * styleConstants.swimlane.height },
    styles: { width: swimlaneWidth, height: styleConstants.swimlane.height },
  }))

  // TODO: Add step ID to allow for multiple instances of the same step
  const workflowNodes = steps.map((step, index) => ({
    id: step.stepDescription,
    type: FlowNodeType.Card,
    data: step,
    parentId: step.swimlaneName,
    position: {
      x:
        styleConstants.swimlane.swimlaneNameContainerWidth +
        (index + 1) * styleConstants.workflowCard.horizontalSpacing +
        index * styleConstants.workflowCard.width,
      y: styleConstants.workflowCard.paddingTop,
    },
    style: {
      minHeight: styleConstants.workflowCard.minHeight,
      width: styleConstants.workflowCard.width,
    },
  }))

  return [...swimlaneNodes, ...workflowNodes]
}

export default WorkflowSwimlanes
