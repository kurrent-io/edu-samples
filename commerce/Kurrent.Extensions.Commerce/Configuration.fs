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
      CartActionCount: CountConfiguration
      TimeBetweenCartActions: DurationConfiguration
      AbandonCartAfterTime: Duration }

    static member Default =
        { ShoppingPeriod =
            { From = Instant.FromUtc(2020, 1, 1, 0, 0, 0)
              To = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow) }
          CartCount = { Minimum = 500; Maximum = 1000 }
          CartActionCount = { Minimum = 1; Maximum = 10 }
          TimeBetweenCartActions =
            { Minimum = Duration.FromSeconds 5.0
              Maximum = Duration.FromMinutes 15.0 }
          AbandonCartAfterTime = Duration.FromHours 1.0 }

type PIMConfiguration =
    { CatalogPeriod: PeriodConfiguration
      CatalogRevisionsPerYear: CountConfiguration
      ProductCount: CountConfiguration }

    static member Default =
        { CatalogPeriod =
            { From = Instant.FromUtc(2020, 1, 1, 0, 0, 0)
              To = Instant.FromDateTimeOffset(DateTimeOffset.UtcNow) }
          CatalogRevisionsPerYear = { Minimum = 1; Maximum = 10 }
          ProductCount = { Minimum = 1000; Maximum = 5000 } }

type Configuration =
    { Shopping: ShoppingConfiguration
      PIM: PIMConfiguration }

    static member Default =
        { Shopping = ShoppingConfiguration.Default
          PIM = PIMConfiguration.Default }
