namespace Kurrent.Extensions.Commerce.Framework

open System.Threading.Tasks
open NodaTime
open NodaTime.Testing
open Bogus

module ClockExtensions =
    type FakeClock with
        member this.AdvanceTimeBetweenActions (faker: Faker) (minimum: Duration) (maximum: Duration) =
            let time =
                Duration.FromSeconds(faker.Random.Double(minimum.TotalSeconds, maximum.TotalSeconds))

            this.Advance time

    type SystemClock with
        member this.AwaitTimeBetweenActions (faker: Faker) (minimum: Duration) (maximum: Duration) =
            let time =
                Duration.FromSeconds(faker.Random.Double(minimum.TotalSeconds, maximum.TotalSeconds))

            Task.Delay(time.ToTimeSpan())

        member this.AwaitTime(time: Duration) = Task.Delay(time.ToTimeSpan())
