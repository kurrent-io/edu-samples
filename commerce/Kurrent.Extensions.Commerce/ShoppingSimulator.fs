namespace Kurrent.Extensions.Commerce

open System
open Bogus
open Bogus.FakerExtensions
open FSharp.Control
open Kurrent.Extensions.Commerce.Framework.FakeClockExtensions
open Microsoft.Extensions.Logging
open NodaTime
open NodaTime.Testing

[<RequireQualifiedAccess>]
module ShoppingSimulator =
    let private generate_customer_id (faker: Faker) = $"customer-%d{faker.Random.Int(0)}"

    let private generate_cart_id () = $"cart-{Guid.NewGuid():N}"

    let private generate_order_id () = $"order-{Guid.NewGuid():N}"

    let simulate (faker: Faker) (configuration: Configuration) (logger: ILogger) =
        taskSeq {
            let cart_count =
                faker.Random.Int(configuration.Shopping.CartCount.Minimum, configuration.Shopping.CartCount.Maximum)

            for _ in [ 1..cart_count ] do
                let instant =
                    faker.Random.Long(
                        configuration.Shopping.ShoppingPeriod.From.ToUnixTimeMilliseconds(),
                        configuration.Shopping.ShoppingPeriod.To.ToUnixTimeMilliseconds()
                    )
                    |> Instant.FromUnixTimeMilliseconds

                let clock = FakeClock(instant)

                let cart_id = generate_cart_id ()
                let cart_stream = StreamName.ofString cart_id
                let mutable shopper_identified = false

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.Cart.CustomerStartedShopping
                            { CartId = cart_id
                              CustomerId = generate_customer_id faker
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    shopper_identified <- true

                else
                    yield
                        cart_stream,
                        Shopping.Cart.VisitorStartedShopping
                            { CartId = cart_id
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                clock.AdvanceTimeBetweenActions
                    faker
                    configuration.Shopping.TimeBetweenCartActions.Minimum
                    configuration.Shopping.TimeBetweenCartActions.Maximum

                let removable_products = ResizeArray<_>()

                for _ in
                    [ 1 .. faker.Random.Int(
                          configuration.Shopping.CartActionCount.Minimum,
                          configuration.Shopping.CartActionCount.Maximum
                      ) ] do

                    if removable_products.Count > 1 && faker.Random.Bool() then
                        let remove_at = faker.Random.Int(0, removable_products.Count - 1)
                        let product_id, quantity = removable_products[remove_at]
                        removable_products.RemoveAt remove_at

                        yield
                            cart_stream,
                            Shopping.Cart.ItemGotRemovedFromCart
                                { CartId = cart_id
                                  ProductId = product_id
                                  Quantity = quantity
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }
                    else
                        let selected_product = faker.FoodProducts().Product()

                        let quantity = faker.Random.Int(1, 5)
                        removable_products.Add(selected_product.Id, quantity)

                        yield
                            cart_stream,
                            Shopping.Cart.ItemGotAddedToCart
                                { CartId = cart_id
                                  ProductId = selected_product.Id
                                  ProductName = selected_product.Name
                                  Quantity = quantity
                                  PricePerUnit = selected_product.Price
                                  TaxRate = selected_product.TaxRate
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCartActions.Minimum
                        configuration.Shopping.TimeBetweenCartActions.Maximum

                if not shopper_identified && faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.Cart.CartShopperGotIdentified
                            { CartId = cart_id
                              CustomerId = generate_customer_id faker
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCartActions.Minimum
                        configuration.Shopping.TimeBetweenCartActions.Maximum

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.Cart.CartGotCheckedOut
                            { CartId = cart_id
                              OrderId = generate_order_id ()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }
                else
                    clock.Advance configuration.Shopping.AbandonCartAfterTime

                    yield
                        cart_stream,
                        Shopping.Cart.CartGotAbandoned
                            { CartId = cart_id
                              AfterBeingIdleFor = configuration.Shopping.AbandonCartAfterTime.ToTimeSpan()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }
        }
