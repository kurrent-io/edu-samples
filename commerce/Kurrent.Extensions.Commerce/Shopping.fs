namespace Kurrent.Extensions.Commerce

open System
open System.ComponentModel
open Minerals.StringCases

[<RequireQualifiedAccess>]
module Shopping =
    module ProductCatalog =
        type Product = {
          Id: string
          Name: string
          Price: string
          TaxRate: decimal
        }

        type Catalog = {
            Version: string
            Products: Product list
        }

    module CustomerProfile =
        type CustomerSignedUp =
            { CustomerId: string
              Email: string
              Name: string
              At: DateTimeOffset }

    module Cart =
        [<Description("Whenever an anonymous visitor starts shopping")>]
        type VisitorStartedShopping = { CartId: string; At: DateTimeOffset }

        [<Description("Whenever a customer signs in or signs up, the cart's shopper becomes known")>]
        type CartShopperGotIdentified =
            { CartId: string
              CustomerId: string
              At: DateTimeOffset }

        [<Description("Whenever a signed-in customer starts shopping")>]
        type CustomerStartedShopping =
            { CartId: string
              CustomerId: string
              At: DateTimeOffset }

        [<Description("Whenever the shopper adds items to the cart")>]
        type ItemGotAddedToCart =
            { CartId: string
              ProductId: string
              ProductName: string
              Quantity: int
              PricePerUnit: string
              TaxRate: decimal
              At: DateTimeOffset }

        [<Description("Whenever the shopper removes items from the cart")>]
        type ItemGotRemovedFromCart =
            { CartId: string
              ProductId: string
              Quantity: int
              At: DateTimeOffset }

        [<Description("Whenever the shopper completes a checkout by placing an order for its contents")>]
        type CartGotCheckedOut =
            { CartId: string
              OrderId: string
              At: DateTimeOffset }

        [<Description("Whenever the shopper becomes idle in their interaction with the cart")>]
        type CartGotAbandoned =
            { CartId: string
              AfterBeingIdleFor: TimeSpan
              At: DateTimeOffset }

        type Event =
            | VisitorStartedShopping of VisitorStartedShopping
            | CartShopperGotIdentified of CartShopperGotIdentified
            | CustomerStartedShopping of CustomerStartedShopping
            | ItemGotAddedToCart of ItemGotAddedToCart
            | ItemGotRemovedFromCart of ItemGotRemovedFromCart
            | CartGotCheckedOut of CartGotCheckedOut
            | CartGotAbandoned of CartGotAbandoned

            member this.ToEventType() =
                match this with
                | VisitorStartedShopping _ -> nameof(VisitorStartedShopping).ToKebabCase()
                | CartShopperGotIdentified _ -> nameof(CartShopperGotIdentified).ToKebabCase()
                | CustomerStartedShopping _ -> nameof(CustomerStartedShopping).ToKebabCase()
                | ItemGotAddedToCart _ -> nameof(ItemGotAddedToCart).ToKebabCase()
                | ItemGotRemovedFromCart _ -> nameof(ItemGotRemovedFromCart).ToKebabCase()
                | CartGotCheckedOut _ -> nameof(CartGotCheckedOut).ToKebabCase()
                | CartGotAbandoned _ -> nameof(CartGotAbandoned).ToKebabCase()

    module Checkout =
        [<Description("Whenever the checkout process started")>]
        type CheckoutStarted =
            { CheckoutId: string
              At: DateTimeOffset }

        type ShippingAddress = { Country: string; Lines: string list }

        [<Description("Whenever the shipping information was collected")>]
        type ShippingInformationCollected =
            { CheckoutId: string
              Address: ShippingAddress
              At: DateTimeOffset }

        type ShippingMethod =
            | Standard = 0
            | Express = 1
            | Overnight = 2
            | SameDay = 3

        [<Description("Whenever the shipping information was collected")>]
        type ShippingMethodSelected =
            { CheckoutId: string
              Method: ShippingMethod
              At: DateTimeOffset }
