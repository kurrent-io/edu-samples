namespace Kurrent.Extensions.Commerce

open System
open Bogus
open FSharp.Control
open Kurrent.Extensions.Commerce.Framework.FakeClockExtensions
open NodaTime
open NodaTime.Testing

[<RequireQualifiedAccess>]
module ShoppingSimulator =
    type CartActionCountConfiguration = { Minimum: int; Maximum: int }

    type TimeBetweenCartActionsConfiguration =
        { Minimum: Duration; Maximum: Duration }

    type ShoppingPeriodConfiguration = { From: Instant; To: Instant }

    type Configuration =
        { ShoppingPeriod: ShoppingPeriodConfiguration
          CartCount: int
          CartActionCount: CartActionCountConfiguration
          TimeBetweenCartActions: TimeBetweenCartActionsConfiguration
          AbandonCartAfterTime: Duration }

    let private faker = Faker()

    let private generate_product_id (year: int) =
        $"""catalog-%d{year}@%d{faker.Random.Int(1, 12)}/{faker.Random.Replace("*****")}"""

    let private generate_customer_id () = $"customer-%d{faker.Random.Int(0)}"

    let private generate_cart_id () = $"cart-{Guid.NewGuid():N}"

    let private generate_order_id () = $"order-{Guid.NewGuid():N}"

    let simulate (configuration: Configuration) =
        taskSeq {
            for _ in [ 1 .. configuration.CartCount ] do
                let instant =
                    faker.Random.Long(
                        configuration.ShoppingPeriod.From.ToUnixTimeMilliseconds(),
                        configuration.ShoppingPeriod.To.ToUnixTimeMilliseconds()
                    )
                    |> Instant.FromUnixTimeMilliseconds

                let clock = FakeClock(instant)

                let cart_id = generate_cart_id ()
                let cart_stream = StreamName.ofString cart_id
                let mutable shopper_identified = false

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CustomerStartedShopping
                            { CartId = cart_id
                              CustomerId = generate_customer_id ()
                              When = clock.GetCurrentInstant().ToDateTimeOffset() }

                    shopper_identified <- true

                else
                    yield
                        cart_stream,
                        Shopping.VisitorStartedShopping
                            { CartId = cart_id
                              When = clock.GetCurrentInstant().ToDateTimeOffset() }

                clock.TimeBetweenActions
                    faker
                    configuration.TimeBetweenCartActions.Minimum
                    configuration.TimeBetweenCartActions.Maximum

                let removable_products = ResizeArray<_>()

                for _ in
                    [ 1 .. faker.Random.Int(
                          configuration.CartActionCount.Minimum,
                          configuration.CartActionCount.Maximum
                      ) ] do

                    if removable_products.Count > 1 && faker.Random.Bool() then
                        let remove_at = faker.Random.Int(0, removable_products.Count - 1)
                        let product_id, quantity = removable_products[remove_at]
                        removable_products.RemoveAt remove_at

                        yield
                            cart_stream,
                            Shopping.ItemGotRemovedFromCart
                                { CartId = cart_id
                                  ProductId = product_id
                                  Quantity = quantity
                                  When = clock.GetCurrentInstant().ToDateTimeOffset() }
                    else
                        let product_id =
                            generate_product_id (clock.GetCurrentInstant().ToDateTimeOffset().Year)

                        let quantity = faker.Random.Int(1, 5)
                        removable_products.Add(product_id, quantity)

                        yield
                            cart_stream,
                            Shopping.ItemGotAddedToCart
                                { CartId = cart_id
                                  ProductId = product_id
                                  ProductName = faker.Commerce.ProductName()
                                  Quantity = quantity
                                  PricePerUnit = faker.Commerce.Price(0.01m, 1000.00m, 2, "$")
                                  TaxRate = faker.Random.ArrayElement [| 0.06m; 0.12m; 0.21m |]
                                  When = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.TimeBetweenActions
                        faker
                        configuration.TimeBetweenCartActions.Minimum
                        configuration.TimeBetweenCartActions.Maximum

                if not shopper_identified && faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CartShopperGotIdentified
                            { CartId = cart_id
                              CustomerId = generate_customer_id ()
                              When = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.TimeBetweenActions
                        faker
                        configuration.TimeBetweenCartActions.Minimum
                        configuration.TimeBetweenCartActions.Maximum

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CartGotCheckedOut
                            { CartId = cart_id
                              OrderId = generate_order_id ()
                              When = clock.GetCurrentInstant().ToDateTimeOffset() }
                else
                    clock.Advance configuration.AbandonCartAfterTime

                    yield
                        cart_stream,
                        Shopping.CartGotAbandoned
                            { CartId = cart_id
                              AfterBeingIdleFor = configuration.AbandonCartAfterTime.ToTimeSpan()
                              When = clock.GetCurrentInstant().ToDateTimeOffset() }
        }
