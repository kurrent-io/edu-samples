namespace Kurrent.Extensions.Commerce.Framework

open NodaTime
open NodaTime.Testing
open Bogus

module ClockExtensions =
    type FakeClock with
        member this.AdvanceTimeBetweenActions (faker: Faker) (minimum: Duration) (maximum: Duration) =
            let time =
                Duration.FromSeconds(faker.Random.Double(minimum.TotalSeconds, maximum.TotalSeconds))

            this.Advance time
