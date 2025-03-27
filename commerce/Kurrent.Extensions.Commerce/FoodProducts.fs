namespace Kurrent.Extensions.Commerce

open Bogus

type FoodProducts(products: PIM.Product[], weights: float32[]) =
    inherit DataSet()

    member this.Product() =
        this.Random.WeightedRandom(products, weights)
