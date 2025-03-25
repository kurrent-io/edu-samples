namespace Kurrent.Extensions.Commerce

[<RequireQualifiedAccess>]
module PIM =
    type Product =
        { Id: string
          Name: string
          Price: string
          TaxRate: decimal }

    type WeightedProduct = { Weight: float32; Product: Product }
