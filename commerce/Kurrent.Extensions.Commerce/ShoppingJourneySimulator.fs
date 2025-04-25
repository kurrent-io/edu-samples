namespace Kurrent.Extensions.Commerce

open System
open Bogus
open Bogus.FakerExtensions
open FSharp.Control
open Kurrent.Extensions.Commerce.Framework.ClockExtensions

type ShoppingJourneySimulator(faker: Faker) =
    let generate_customer_id () = $"customer-%d{faker.Random.Int(0)}"

    let generate_cart_id () = $"cart-{Guid.NewGuid():N}"

    let generate_order_id () = $"order-{Guid.NewGuid():N}"

    interface ISimulator<Shopping.Event> with
        member this.Simulate clock configuration =
            taskSeq {
                let customer_id = generate_customer_id ()
                let cart_id = generate_cart_id ()
                let order_id = generate_order_id ()
                let cart_stream = StreamName.ofString cart_id
                let order_stream = StreamName.ofString order_id
                let checkout_stream = StreamName.ofString $"checkout-for-{cart_id}"
                let mutable shopper_identified = false
                let mutable cart_version = 0L

                if faker.Random.Bool() then
                    yield
                        cart_stream,
                        Shopping.CustomerStartedShopping
                            { CartId = cart_id
                              CustomerId = customer_id
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

                let products_in_cart = ResizeArray<Shopping.OrderFulfillment.OrderLineItem>()

                for _ in
                    [ 1 .. faker.Random.Int(
                          configuration.Shopping.CartActionCount.Minimum,
                          configuration.Shopping.CartActionCount.Maximum
                      ) ] do

                    if products_in_cart.Count > 1 && faker.Random.Bool() then
                        let remove_at = faker.Random.Int(0, products_in_cart.Count - 1)
                        let product_in_cart = products_in_cart[remove_at]
                        products_in_cart.RemoveAt remove_at

                        yield
                            cart_stream,
                            Shopping.ItemGotRemovedFromCart
                                { CartId = cart_id
                                  ProductId = product_in_cart.ProductId
                                  Quantity = product_in_cart.Quantity
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    else
                        let selected_product = faker.FoodProducts().Product()

                        let quantity = faker.Random.Int(1, 5)

                        products_in_cart.Add(
                            { ProductId = selected_product.Id
                              ProductName = selected_product.Name
                              Quantity = quantity
                              PricePerUnit = selected_product.Price
                              TaxRate = selected_product.TaxRate }
                        )

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
                              CustomerId = customer_id
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

                    let mutable billing_recipient: Shopping.Checkout.Recipient =
                        { Title = faker.Name.Prefix()
                          FullName = faker.Person.FullName
                          EmailAddress = faker.Person.Email
                          PhoneNumber = faker.Phone.PhoneNumber() }

                    let mutable billing_address: Shopping.Checkout.Address =
                        { Lines =
                            [ yield faker.Address.StreetName() + " " + faker.Address.BuildingNumber()
                              yield faker.Address.ZipCode() + " " + faker.Address.City()
                              yield faker.Address.County() ]
                          Country = faker.Address.CountryCode() }

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
                                | _ -> faker.Commerce.Price(0.0m, 10.00m, 2, "USD")
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
                                  Recipient = billing_recipient
                                  Address = billing_address
                                  At = clock.GetCurrentInstant().ToDateTimeOffset() }
                    else
                        billing_recipient <- recipient
                        billing_address <- address

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

                    let payment_method = faker.PickRandom<Shopping.Checkout.PaymentMethod>()

                    yield
                        checkout_stream,
                        Shopping.PaymentMethodSelected
                            { Cart = $"{cart_id}@{cart_version}"
                              Method = payment_method
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    clock.AdvanceTimeBetweenActions
                        faker
                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                    yield
                        checkout_stream,
                        Shopping.CheckoutCompleted
                            { Cart = $"{cart_id}@{cart_version}"
                              OrderId = order_id
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    yield
                        cart_stream,
                        Shopping.CartGotCheckedOut
                            { CartId = cart_id
                              OrderId = order_id
                              At = clock.GetCurrentInstant().ToDateTimeOffset() }

                    yield
                        order_stream,
                        Shopping.OrderPlaced
                            { OrderId = order_id
                              CustomerId = customer_id
                              CheckoutOfCart = $"{cart_id}@{cart_version}"
                              LineItems = products_in_cart.ToArray() |> List.ofArray
                              Shipping =
                                { Recipient =
                                    { Title = recipient.Title
                                      FullName = recipient.FullName
                                      EmailAddress = recipient.EmailAddress
                                      PhoneNumber = recipient.PhoneNumber }
                                  Address =
                                    { Lines = address.Lines
                                      Country = address.Country }
                                  Instructions = ""
                                  Method =
                                    match shipping_method with
                                    | Shopping.Checkout.ShippingMethod.Express ->
                                        Shopping.OrderFulfillment.OrderShippingMethod.Express
                                    | Shopping.Checkout.ShippingMethod.Overnight ->
                                        Shopping.OrderFulfillment.OrderShippingMethod.Overnight
                                    | Shopping.Checkout.ShippingMethod.SameDay ->
                                        Shopping.OrderFulfillment.OrderShippingMethod.SameDay
                                    | Shopping.Checkout.ShippingMethod.Standard ->
                                        Shopping.OrderFulfillment.OrderShippingMethod.Standard
                                    | _ -> failwith "Unknown shipping method" }
                              Billing =
                                { Recipient =
                                    { Title = billing_recipient.Title
                                      FullName = billing_recipient.FullName
                                      EmailAddress = billing_recipient.EmailAddress
                                      PhoneNumber = billing_recipient.PhoneNumber }
                                  Address =
                                    { Lines = billing_address.Lines
                                      Country = billing_address.Country }
                                  PaymentMethod =
                                    match payment_method with
                                    | Shopping.Checkout.PaymentMethod.CreditCard ->
                                        Shopping.OrderFulfillment.OrderPaymentMethod.CreditCard
                                    | Shopping.Checkout.PaymentMethod.DebitCard ->
                                        Shopping.OrderFulfillment.OrderPaymentMethod.DebitCard
                                    | Shopping.Checkout.PaymentMethod.WireTransfer ->
                                        Shopping.OrderFulfillment.OrderPaymentMethod.WireTransfer
                                    | _ -> failwith "Unknown payment method" }
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
