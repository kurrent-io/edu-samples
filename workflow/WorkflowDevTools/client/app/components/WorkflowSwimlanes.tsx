import { Step, StepStatus, StepType } from "./Step"
import {
  ReactFlow,
  Node as ReactFlowNode,
  Edge,
  MarkerType,
  Handle,
  Position,
} from "@xyflow/react"
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
    id: "0",
    stepType: StepType.Action,
    stepDescription: "Click Checkout",
    swimlaneName: "Store UI",
  },
  {
    id: "1",
    stepType: StepType.Action,
    stepDescription: "Reserve Inventory",
    swimlaneName: "Reservation",
    causationId: "0",
  },
  {
    id: "2",
    stepType: StepType.Event,
    stepDescription: "Inventory Reserved",
    swimlaneName: "Reservation",
    causationId: "1",
  },
  {
    id: "3",
    stepType: StepType.Action,
    stepDescription: "Place Order",
    swimlaneName: "Order",
    causationId: "0",
  },
  {
    id: "4",
    stepType: StepType.Event,
    stepDescription: "Order Placed",
    swimlaneName: "Order",
    causationId: "3",
  },
]

interface WorkflowSwimlanesProps {
  steps: Step[]
}

const WorkflowCardNode = ({ data }: { data: Step }) => (
  <>
    <Handle
      type="target"
      style={{ visibility: "hidden" }}
      className="node-handle"
      position={Position.Left}
    />
    <WorkflowCard
      stepType={data.stepType}
      stepDescription={data.stepDescription}
      // TODO: Use status from state
      status={StepStatus.Started}
    />
    <Handle
      type="source"
      style={{ visibility: "hidden" }}
      position={Position.Right}
    />
  </>
)

const nodeTypes = {
  [FlowNodeType.Swimlane]: SwimlaneNode,
  [FlowNodeType.Card]: WorkflowCardNode,
}

const WorkflowSwimlanes = ({ steps }: WorkflowSwimlanesProps) => {
  const { nodes, edges } = useMemo(
    () => ({
      nodes: getNodesFromSteps(steps),
      edges: getEdgesFromSteps(steps),
    }),
    [steps],
  )

  console.log(edges)

  return (
    <div style={{ height: "100vh", width: "100vw" }}>
      <ReactFlow nodes={nodes} edges={edges} nodeTypes={nodeTypes} />
    </div>
  )
}

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

  const workflowNodes: ReactFlowNode<Step>[] = steps.map((step, index) => ({
    id: step.id,
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
      display: "flex",
      flexDirection: "column",
    },
  }))

  return [...swimlaneNodes, ...workflowNodes]
}

const getEdgesFromSteps = (steps: Step[]): Edge[] =>
  steps
    .filter((s) => !!s.causationId)
    .map((s) => ({
      id: `edge-${s.causationId}-${s.id}`,
      source: s.causationId!,
      target: s.id,
      type: "smoothstep",
      markerEnd: {
        type: MarkerType.ArrowClosed,
        width: styleConstants.arrow.width,
        height: styleConstants.arrow.height,
      },
    }))

export default WorkflowSwimlanes
