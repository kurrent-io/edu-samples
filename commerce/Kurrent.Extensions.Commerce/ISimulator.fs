namespace Kurrent.Extensions.Commerce

open FSharp.Control
open NodaTime.Testing

type ISimulator<'Event> =
    abstract member Simulate : FakeClock -> Configuration -> TaskSeq<StreamName * 'Event>

