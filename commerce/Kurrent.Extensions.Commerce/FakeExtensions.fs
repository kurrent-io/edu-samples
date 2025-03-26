namespace Bogus

open Kurrent.Extensions.Commerce

module FakerExtensions =
    type Faker with
        member this.FoodProducts() =
            let has_context: IHasContext = upcast this
            has_context.Context[$"__{(nameof FoodProducts).ToLowerInvariant()}"] :?> FoodProducts
