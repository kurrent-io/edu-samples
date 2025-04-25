namespace Kurrent.Extensions.Commerce

open System
open NodaTime

type PeriodConfiguration = { From: Instant; To: Instant }

type CountConfiguration = { Minimum: int; Maximum: int }

type DurationConfiguration =
    { Minimum: Duration; Maximum: Duration }

type ShoppingConfiguration =
    { ShoppingPeriod: PeriodConfiguration
      CartCount: CountConfiguration
      ConcurrentCartCount: CountConfiguration
      CartActionCount: CountConfiguration
      TimeBetweenCartActions: DurationConfiguration
      TimeBetweenCheckoutActions: DurationConfiguration
      AbandonCartAfterTime: Duration }

    static member Default =
        { ShoppingPeriod =
            { From = Instant.FromUtc(2020, 1, 1, 0, 0, 0)
              To = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow) }
          CartCount = { Minimum = 500; Maximum = 1000 }
          ConcurrentCartCount =
            { Minimum = 1
              Maximum = Environment.ProcessorCount }
          CartActionCount = { Minimum = 1; Maximum = 10 }
          TimeBetweenCartActions =
            { Minimum = Duration.FromSeconds 5.0
              Maximum = Duration.FromMinutes 15.0 }
          TimeBetweenCheckoutActions =
            { Minimum = Duration.FromSeconds 30.0
              Maximum = Duration.FromMinutes 2.0 }
          AbandonCartAfterTime = Duration.FromHours 1.0 }

type ProductSource =
    | OpenFoodFacts = 0
    | Amazon = 1
    | Walmart = 2

type PIMConfiguration =
    { ProductCount: CountConfiguration
      ProductSource: ProductSource }

    static member Default =
        { ProductCount = { Minimum = 1000; Maximum = 5000 }
          ProductSource = ProductSource.Amazon }

type Configuration =
    { Shopping: ShoppingConfiguration
      PIM: PIMConfiguration }

    static member Default =
        { Shopping = ShoppingConfiguration.Default
          PIM = PIMConfiguration.Default }
