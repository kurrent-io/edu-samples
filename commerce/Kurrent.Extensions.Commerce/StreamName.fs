namespace Kurrent.Extensions.Commerce

type StreamName = private StreamName of string

module StreamName =
    let ofString value = StreamName value
    let prefix value (StreamName suffix) = $"{value}-{suffix}"
    let suffix value (StreamName prefix) = $"{prefix}-{value}"
    let toString (StreamName name) = name
