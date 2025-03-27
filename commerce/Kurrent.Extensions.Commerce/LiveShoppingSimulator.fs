namespace Kurrent.Extensions.Commerce

open System
open System.Collections.Concurrent
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Bogus
open Bogus.FakerExtensions
open EventStore.Client
open FSharp.Control
open Kurrent.Extensions.Commerce.Framework.ClockExtensions
open Microsoft.Extensions.Logging
open NodaTime

[<RequireQualifiedAccess>]
module LiveShoppingSimulator =
    let private generate_customer_id (faker: Faker) = $"customer-%d{faker.Random.Int(0)}"

    let private generate_cart_id () = $"cart-{Guid.NewGuid():N}"

    let private generate_order_id () = $"order-{Guid.NewGuid():N}"

    let private append
        (client: EventStoreClient)
        (revisions: ConcurrentDictionary<StreamName, StreamRevision>)
        (stream: StreamName)
        (events: Shopping.Event array)
        =
        task {
            let expected =
                match revisions.TryGetValue stream with
                | true, revision -> revision
                | false, _ -> StreamRevision.None

            let data =
                events
                |> Array.map (fun event ->
                    EventData(Uuid.NewUuid(), event.ToEventType(), JsonSerializer.SerializeToUtf8Bytes(event)))

            let! append_result = client.AppendToStreamAsync(StreamName.toString stream, expected, data)
            revisions[stream] <- append_result.NextExpectedStreamRevision
        }

    let simulate (client: EventStoreClient) (faker: Faker) (configuration: Configuration) (logger: ILogger) =
        task {
            let revisions = ConcurrentDictionary<StreamName, StreamRevision>()

            let cart_count =
                faker.Random.Int(configuration.Shopping.CartCount.Minimum, configuration.Shopping.CartCount.Maximum)

            let options =
                ParallelOptions(
                    MaxDegreeOfParallelism =
                        faker.Random.Int(
                            configuration.Shopping.ConcurrentCartCount.Minimum,
                            configuration.Shopping.ConcurrentCartCount.Maximum
                        )
                )

            let clock = SystemClock.Instance

            let initial_delay_in_seconds =
                int (
                    (double configuration.Shopping.CartActionCount.Minimum)
                    * configuration.Shopping.TimeBetweenCartActions.Minimum.TotalSeconds
                )

            do!
                Parallel.ForAsync(
                    0,
                    cart_count,
                    options,
                    fun (cart: int) (ct: CancellationToken) ->
                        task {
                            do! Task.Delay(faker.Random.Int(0, initial_delay_in_seconds), ct)

                            let cart_id = generate_cart_id ()
                            let cart_stream = StreamName.ofString cart_id
                            let checkout_stream = StreamName.ofString $"checkout-for-{cart_id}"
                            let mutable shopper_identified = false
                            let mutable cart_version = 0L

                            if faker.Random.Bool() then
                                do!
                                    append
                                        client
                                        revisions
                                        cart_stream
                                        [| Shopping.CustomerStartedShopping
                                               { CartId = cart_id
                                                 CustomerId = generate_customer_id faker
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                cart_version <- cart_version + 1L
                                shopper_identified <- true

                            else
                                do!
                                    append
                                        client
                                        revisions
                                        cart_stream
                                        [| Shopping.VisitorStartedShopping
                                               { CartId = cart_id
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                cart_version <- cart_version + 1L

                            do!
                                clock.AwaitTimeBetweenActions
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

                                    do!
                                        append
                                            client
                                            revisions
                                            cart_stream
                                            [| Shopping.ItemGotRemovedFromCart
                                                   { CartId = cart_id
                                                     ProductId = product_id
                                                     Quantity = quantity
                                                     At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                else
                                    let selected_product = faker.FoodProducts().Product()

                                    let quantity = faker.Random.Int(1, 5)
                                    removable_products.Add(selected_product.Id, quantity)

                                    do!
                                        append
                                            client
                                            revisions
                                            cart_stream
                                            [| Shopping.ItemGotAddedToCart
                                                   { CartId = cart_id
                                                     ProductId = selected_product.Id
                                                     ProductName = selected_product.Name
                                                     Quantity = quantity
                                                     PricePerUnit = selected_product.Price
                                                     TaxRate = selected_product.TaxRate
                                                     At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                cart_version <- cart_version + 1L

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCartActions.Minimum
                                        configuration.Shopping.TimeBetweenCartActions.Maximum

                            if not shopper_identified && faker.Random.Bool() then
                                do!
                                    append
                                        client
                                        revisions
                                        cart_stream
                                        [| Shopping.CartShopperGotIdentified
                                               { CartId = cart_id
                                                 CustomerId = generate_customer_id faker
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                cart_version <- cart_version + 1L

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCartActions.Minimum
                                        configuration.Shopping.TimeBetweenCartActions.Maximum

                            if faker.Random.Bool() then
                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.CheckoutStarted
                                               { Cart = $"{cart_id}@{cart_version}"
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

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

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.ShippingInformationCollected
                                               { Cart = $"{cart_id}@{cart_version}"
                                                 Recipient = recipient
                                                 Address = address
                                                 Instructions = faker.Lorem.Lines(2)
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                                let shipping_method = faker.PickRandom<Shopping.Checkout.ShippingMethod>()

                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.ShippingMethodSelected
                                               { Cart = $"{cart_id}@{cart_version}"
                                                 Method = shipping_method
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.ShippingCostCalculated
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
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                                if faker.Random.Bool() then
                                    do!
                                        append
                                            client
                                            revisions
                                            checkout_stream
                                            [| Shopping.BillingInformationCollected
                                                   { Cart = $"{cart_id}@{cart_version}"
                                                     Recipient =
                                                       { Title = faker.Name.Prefix()
                                                         FullName = faker.Person.FullName
                                                         EmailAddress = faker.Person.Email
                                                         PhoneNumber = faker.Phone.PhoneNumber() }
                                                     Address =
                                                       { Lines =
                                                           [ yield
                                                                 faker.Address.StreetName()
                                                                 + " "
                                                                 + faker.Address.BuildingNumber()
                                                             yield faker.Address.ZipCode() + " " + faker.Address.City()
                                                             yield faker.Address.County() ]
                                                         Country = faker.Address.CountryCode() }
                                                     At = clock.GetCurrentInstant().ToDateTimeOffset() } |]
                                else
                                    do!
                                        append
                                            client
                                            revisions
                                            checkout_stream
                                            [| Shopping.BillingInformationCopiedFromShippingInformation
                                                   { Cart = $"{cart_id}@{cart_version}"
                                                     Recipient = recipient
                                                     Address = address
                                                     At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.PaymentMethodSelected
                                               { Cart = $"{cart_id}@{cart_version}"
                                                 Method = faker.PickRandom<Shopping.Checkout.PaymentMethod>()
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    clock.AwaitTimeBetweenActions
                                        faker
                                        configuration.Shopping.TimeBetweenCheckoutActions.Minimum
                                        configuration.Shopping.TimeBetweenCheckoutActions.Maximum

                                do!
                                    append
                                        client
                                        revisions
                                        checkout_stream
                                        [| Shopping.CheckoutCompleted
                                               { Cart = $"{cart_id}@{cart_version}"
                                                 OrderId = generate_order_id ()
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]

                                do!
                                    append
                                        client
                                        revisions
                                        cart_stream
                                        [| Shopping.CartGotCheckedOut
                                               { CartId = cart_id
                                                 OrderId = generate_order_id ()
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]
                            else
                                do! clock.AwaitTime configuration.Shopping.AbandonCartAfterTime

                                do!
                                    append
                                        client
                                        revisions
                                        cart_stream
                                        [| Shopping.CartGotAbandoned
                                               { CartId = cart_id
                                                 AfterBeingIdleFor =
                                                   configuration.Shopping.AbandonCartAfterTime.ToTimeSpan()
                                                 At = clock.GetCurrentInstant().ToDateTimeOffset() } |]
                        }
                        |> ValueTask
                )
        } :> Task
