import express from "express"
import ViteExpress from "vite-express"
import { readFileSync } from "fs"

const SALES_DATA_FILEPATH =
  process.env["SALES_DATA_FILEPATH"] || "/opt/sales-dashboard/sales-data.json"

const app = express()

app.get("/hello", (_, res) => {
  res.send("Hello Vite + React + TypeScript!")
})

app.get("/api/sales-data", (_, res) => {
  const salesData = JSON.parse(readFileSync(SALES_DATA_FILEPATH, "utf8"))
  res.send(JSON.stringify({ salesData }))
})

ViteExpress.listen(app, 3000, () =>
  console.log("Server is listening on port 3000..."),
)
