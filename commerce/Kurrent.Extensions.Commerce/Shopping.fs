namespace Kurrent.Extensions.Commerce

open System
open System.ComponentModel
open Minerals.StringCases

[<RequireQualifiedAccess>]
module Shopping =
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

    module Checkout =
        [<Description("Whenever the checkout process started")>]
        type CheckoutStarted = { Cart: string; At: DateTimeOffset }

        type Recipient =
            { Title: string
              FullName: string
              EmailAddress: string
              PhoneNumber: string }

        type Address = { Country: string; Lines: string list }

        [<Description("Whenever the shipping information was collected")>]
        type ShippingInformationCollected =
            { Cart: string
              Recipient: Recipient
              Address: Address
              Instructions: string
              At: DateTimeOffset }

        type ShippingMethod =
            | Standard = 0
            | Express = 1
            | Overnight = 2
            | SameDay = 3

        [<Description("Whenever the shipping method is selected")>]
        type ShippingMethodSelected =
            { Cart: string
              Method: ShippingMethod
              At: DateTimeOffset }

        [<Description("Whenever the shipping method is selected, the cost is calculated")>]
        type ShippingCostCalculated =
            { Cart: string
              ForMethod: ShippingMethod
              Cost: string
              At: DateTimeOffset }

        type PaymentMethod =
            | CreditCard = 0
            | DebitCard = 1
            | WireTransfer = 2

        [<Description("Whenever the billing information was collected")>]
        type BillingInformationCollected =
            { Cart: string
              Recipient: Recipient
              Address: Address
              At: DateTimeOffset }

        [<Description("Whenever the billing information was to be copied from the shipping information")>]
        type BillingInformationCopiedFromShippingInformation =
            { Cart: string
              Recipient: Recipient
              Address: Address
              At: DateTimeOffset }

        [<Description("Whenever the payment method is selected")>]
        type PaymentMethodSelected =
            { Cart: string
              Method: PaymentMethod
              At: DateTimeOffset }

        [<Description("Whenever the checkout completed")>]
        type CheckoutCompleted =
            { Cart: string
              OrderId: string
              At: DateTimeOffset }

    type Event =
        | VisitorStartedShopping of Cart.VisitorStartedShopping
        | CartShopperGotIdentified of Cart.CartShopperGotIdentified
        | CustomerStartedShopping of Cart.CustomerStartedShopping
        | ItemGotAddedToCart of Cart.ItemGotAddedToCart
        | ItemGotRemovedFromCart of Cart.ItemGotRemovedFromCart
        | CartGotCheckedOut of Cart.CartGotCheckedOut
        | CartGotAbandoned of Cart.CartGotAbandoned
        | CheckoutStarted of Checkout.CheckoutStarted
        | ShippingInformationCollected of Checkout.ShippingInformationCollected
        | ShippingMethodSelected of Checkout.ShippingMethodSelected
        | ShippingCostCalculated of Checkout.ShippingCostCalculated
        | BillingInformationCollected of Checkout.BillingInformationCollected
        | BillingInformationCopiedFromShippingInformation of Checkout.BillingInformationCopiedFromShippingInformation
        | PaymentMethodSelected of Checkout.PaymentMethodSelected
        | CheckoutCompleted of Checkout.CheckoutCompleted

        member this.ToEventType() =
            match this with
            | VisitorStartedShopping _ -> nameof(VisitorStartedShopping).ToKebabCase()
            | CartShopperGotIdentified _ -> nameof(CartShopperGotIdentified).ToKebabCase()
            | CustomerStartedShopping _ -> nameof(CustomerStartedShopping).ToKebabCase()
            | ItemGotAddedToCart _ -> nameof(ItemGotAddedToCart).ToKebabCase()
            | ItemGotRemovedFromCart _ -> nameof(ItemGotRemovedFromCart).ToKebabCase()
            | CartGotCheckedOut _ -> nameof(CartGotCheckedOut).ToKebabCase()
            | CartGotAbandoned _ -> nameof(CartGotAbandoned).ToKebabCase()
            | CheckoutStarted _ -> nameof(CheckoutStarted).ToKebabCase()
            | ShippingInformationCollected _ -> nameof(ShippingInformationCollected).ToKebabCase()
            | ShippingMethodSelected _ -> nameof(ShippingMethodSelected).ToKebabCase()
            | ShippingCostCalculated _ -> nameof(ShippingCostCalculated).ToKebabCase()
            | BillingInformationCollected _ -> nameof(BillingInformationCollected).ToKebabCase()
            | BillingInformationCopiedFromShippingInformation _ ->
                nameof(BillingInformationCopiedFromShippingInformation).ToKebabCase()
            | PaymentMethodSelected _ -> nameof(PaymentMethodSelected).ToKebabCase()
            | CheckoutCompleted _ -> nameof(CheckoutCompleted).ToKebabCase()
