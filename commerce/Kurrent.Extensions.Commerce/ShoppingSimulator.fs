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
                let checkout_stream = StreamName.ofString $"checkout-for-{cart_id}"
                let mutable shopper_identified = false
                let mutable cart_version = 0L

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CustomerStartedShopping
                            { CartId = cart_id
                              CustomerId = generate_customer_id faker
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    cart_version <- cart_version + 1L
                    shopper_identified <- true

                else
                    yield
                        cart_stream,
                        Shopping.VisitorStartedShopping
                            { CartId = cart_id
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    cart_version <- cart_version + 1L

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
                            Shopping.ItemGotRemovedFromCart
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
                            Shopping.ItemGotAddedToCart
                                { CartId = cart_id
                                  ProductId = selected_product.Id
                                  ProductName = selected_product.Name
                                  Quantity = quantity
                                  PricePerUnit = selected_product.Price
                                  TaxRate = selected_product.TaxRate
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    cart_version <- cart_version + 1L

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCartActions.Minimum
                        configuration.Shopping.TimeBetweenCartActions.Maximum

                if not shopper_identified && faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CartShopperGotIdentified
                            { CartId = cart_id
                              CustomerId = generate_customer_id faker
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    cart_version <- cart_version + 1L

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCartActions.Minimum
                        configuration.Shopping.TimeBetweenCartActions.Maximum

                if faker.Random.Bool() then
                    yield
                        checkout_stream,
                        Shopping.CheckoutStarted
                            { Cart = $"{cart_id}@{cart_version}"
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    let recipient: Shopping.Checkout.Recipient =
                        { Title = faker.Name.Prefix()
                          FullName = faker.Person.FullName
                          EmailAddress = faker.Person.Email
                          PhoneNumber = faker.Phone.PhoneNumber() }

                    let address: Shopping.Checkout.Address =
                        { Lines =
                            [ yield faker.Address.StreetName() + " " + faker.Address.BuildingNumber()
                              yield faker.Address.ZipCode() + " " + faker.Address.City()
                              yield faker.Address.County() ]
                          Country = faker.Address.CountryCode() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                    yield
                        checkout_stream,
                        Shopping.ShippingInformationCollected
                            { Cart = $"{cart_id}@{cart_version}"
                              Recipient = recipient
                              Address = address
                              Instructions = faker.Lorem.Lines(2)
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }
                    
                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum
                    
                    let shipping_method = faker.PickRandom<Shopping.Checkout.ShippingMethod>()
                    
                    yield
                        checkout_stream,
                        Shopping.ShippingMethodSelected
                            { Cart = $"{cart_id}@{cart_version}"
                              Method = shipping_method
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    yield
                        checkout_stream,
                        Shopping.ShippingCostCalculated
                            { Cart = $"{cart_id}@{cart_version}"
                              ForMethod = shipping_method
                              Cost =
                                  match shipping_method with
                                  | Shopping.Checkout.ShippingMethod.Express ->
                                        faker.Commerce.Price(5.0m, 20.00m, 2, "USD")
                                  | Shopping.Checkout.ShippingMethod.Overnight ->
                                        faker.Commerce.Price(10.0m, 30.00m, 2, "USD")
                                  | Shopping.Checkout.ShippingMethod.SameDay ->
                                        faker.Commerce.Price(15.0m, 40.00m, 2, "USD")
                                  | _ ->
                                        faker.Commerce.Price(0.0m, 10.00m, 2, "USD")
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                    if faker.Random.Bool() then
                        yield
                            checkout_stream,
                            Shopping.BillingInformationCollected
                                { Cart = $"{cart_id}@{cart_version}"
                                  Recipient =
                                    { Title = faker.Name.Prefix()
                                      FullName = faker.Person.FullName
                                      EmailAddress = faker.Person.Email
                                      PhoneNumber = faker.Phone.PhoneNumber() }
                                  Address =
                                    { Lines =
                                        [ yield faker.Address.StreetName() + " " + faker.Address.BuildingNumber()
                                          yield faker.Address.ZipCode() + " " + faker.Address.City()
                                          yield faker.Address.County() ]
                                      Country = faker.Address.CountryCode() }
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }
                    else
                        yield
                            checkout_stream,
                            Shopping.BillingInformationCopiedFromShippingInformation
                                { Cart = $"{cart_id}@{cart_version}"
                                  Recipient = recipient
                                  Address = address
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum
                    
                    yield
                        checkout_stream,
                        Shopping.PaymentMethodSelected
                            { Cart = $"{cart_id}@{cart_version}"
                              Method = faker.PickRandom<Shopping.Checkout.PaymentMethod>()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                    yield
                        checkout_stream,
                        Shopping.CheckoutCompleted
                            { Cart = $"{cart_id}@{cart_version}"
                              OrderId = generate_order_id()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    yield
                        cart_stream,
                        Shopping.CartGotCheckedOut
                            { CartId = cart_id
                              OrderId = generate_order_id ()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }
                else
                    clock.Advance configuration.Shopping.AbandonCartAfterTime

                    yield
                        cart_stream,
                        Shopping.CartGotAbandoned
                            { CartId = cart_id
                              AfterBeingIdleFor = configuration.Shopping.AbandonCartAfterTime.ToTimeSpan()
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }
        }
