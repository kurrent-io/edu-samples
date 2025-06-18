export type Step = {
  id: string
  stepType: StepType
  stepDescription: string
  swimlaneName: string
  causationId?: string
}

export enum StepType {
  Action = "Action",
  Event = "Event",
  Subscription = "Subscription",
}

export enum StepStatus {
  Started = "Started",
  Paused = "Paused",
  Success = "Success",
  Failed = "Failed",
}
