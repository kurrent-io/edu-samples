export type Step = {
  stepType: StepType
  stepDescription: string
  swimlaneName: string
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
