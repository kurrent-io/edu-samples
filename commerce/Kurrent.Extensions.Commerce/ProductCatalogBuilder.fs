namespace Kurrent.Extensions.Commerce

open System.IO
open Bogus
open Flurl
open Flurl.Http
open FSharp.Control
open Kurrent.Extensions.Commerce.Framework
open Microsoft.Extensions.Logging
open NodaTime.Testing

module ProductCatalogBuilder =
    let private download_open_food_facts_data () =
        task {
            let url =
                Url(
                    "https://huggingface.co/datasets/openfoodfacts/product-database/resolve/main/food.parquet?download=true"
                )

            use! response_stream = url.GetStreamAsync()
            use file_stream = File.Create("open_food_facts.parquet")
            do! response_stream.CopyToAsync(file_stream)
        }
        
    let private download_amazon_data () =
        task {
            let url =
                Url(
                    "https://github.com/luminati-io/eCommerce-dataset-samples/raw/refs/heads/main/amazon-products.csv"
                )

            use! response_stream = url.GetStreamAsync()
            use file_stream = File.Create("amazon.csv")
            do! response_stream.CopyToAsync(file_stream)
        }
        
    let private download_walmart_data () =
        task {
            let url =
                Url(
                    "https://github.com/luminati-io/eCommerce-dataset-samples/raw/refs/heads/main/walmart-products.csv"
                )

            use! response_stream = url.GetStreamAsync()
            use file_stream = File.Create("walmart.csv")
            do! response_stream.CopyToAsync(file_stream)
        }

    let build (faker: Faker) (configuration: Configuration) (log: ILogger) =
        task {
            // Ensure the product data is downloaded
            match configuration.PIM.ProductSource with
            | ProductSource.OpenFoodFacts ->
                if not (File.Exists "open_food_facts.parquet") then
                    log.LogInformation("Downloading Open Food Facts product data ... this can take some time")
                    do! download_open_food_facts_data ()
            | ProductSource.Amazon ->
                if not (File.Exists "amazon.csv") then
                    log.LogInformation("Downloading Amazon product data ... this can take some time")
                    do! download_amazon_data ()
            | ProductSource.Walmart ->
                if not (File.Exists "walmart.csv") then
                    log.LogInformation("Downloading Walmart product data ... this can take some time")
                    do! download_walmart_data ()
            | _ -> ()
                    
            let product_count =
                faker.Random.Int(configuration.PIM.ProductCount.Minimum, configuration.PIM.ProductCount.Maximum)
            
            log.LogInformation("Generating {ProductCount} products", product_count)
            
            let! weighted_products =
                match configuration.PIM.ProductSource with
                | ProductSource.OpenFoodFacts ->
                    Sql.connect_with_defaults ()
                    |> Sql.query
                        $"SELECT DISTINCT(product_name[1].text) FROM read_parquet('open_food_facts.parquet') LIMIT {product_count}"
                        _.GetString(0)
                    |> TaskSeq.mapi (fun index product_name ->
                        { Weight = (float32 index)
                          Product =
                            { Id = faker.Commerce.Ean13()
                              Name = product_name
                              Price = faker.Commerce.Price(0.01m, 1000.00m, 2, "USD")
                              TaxRate = faker.Random.ArrayElement [| 0.06m; 0.12m; 0.21m |] } }
                        : PIM.WeightedProduct)
                    |> TaskSeq.toArrayAsync
                | ProductSource.Amazon ->
                    Sql.connect_with_defaults ()
                    |> Sql.query
                        $"SELECT DISTINCT(title) FROM read_csv('amazon.csv') LIMIT {product_count}"
                        _.GetString(0)
                    |> TaskSeq.mapi (fun index product_name ->
                        { Weight = (float32 index)
                          Product =
                            { Id = faker.Commerce.Ean13()
                              Name = product_name
                              Price = faker.Commerce.Price(0.01m, 1000.00m, 2, "USD")
                              TaxRate = faker.Random.ArrayElement [| 0.06m; 0.12m; 0.21m |] } }
                        : PIM.WeightedProduct)
                    |> TaskSeq.toArrayAsync
                | ProductSource.Walmart ->
                    Sql.connect_with_defaults ()
                    |> Sql.query
                        $"SELECT DISTINCT(product_name) FROM read_csv('walmart.csv') LIMIT {product_count}"
                        _.GetString(0)
                    |> TaskSeq.mapi (fun index product_name ->
                        { Weight = (float32 index)
                          Product =
                            { Id = faker.Commerce.Ean13()
                              Name = product_name
                              Price = faker.Commerce.Price(0.01m, 1000.00m, 2, "USD")
                              TaxRate = faker.Random.ArrayElement [| 0.06m; 0.12m; 0.21m |] } }
                        : PIM.WeightedProduct)
                    |> TaskSeq.toArrayAsync
                | _ ->
                    [
                        for index in 1 .. product_count do
                            let product_name = faker.Commerce.ProductName()
                            { Weight = (float32 index)
                              Product =
                                { Id = faker.Commerce.Ean13()
                                  Name = product_name
                                  Price = faker.Commerce.Price(0.01m, 1000.00m, 2, "USD")
                                  TaxRate = faker.Random.ArrayElement [| 0.06m; 0.12m; 0.21m |] } }
                            : PIM.WeightedProduct
                    ]
                    |> TaskSeq.ofList
                    |> TaskSeq.toArrayAsync
                        
            let total_weight = weighted_products |> Array.sumBy _.Weight

            let normalized_products =
                weighted_products
                |> Array.map (fun weighted_product ->
                    { weighted_product with
                        Weight = weighted_product.Weight / total_weight })

            let products = normalized_products |> Array.map _.Product
            let weights = normalized_products |> Array.map _.Weight

            let dataset = FoodProducts(products, weights)
            let has_randomizer: IHasRandomizer = upcast faker
            has_randomizer.GetNotifier().Flow(dataset) |> ignore
            let has_context: IHasContext = upcast faker
            has_context.Context[$"__{(nameof FoodProducts).ToLowerInvariant()}"] <- dataset
        }
