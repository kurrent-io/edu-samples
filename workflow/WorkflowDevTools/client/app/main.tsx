import "./index.css"

import * as React from "react"
import * as ReactDOM from "react-dom/client"

import App from "./App"
import { ChakraUIProvider } from "./components/chakra-ui/ChakraUIProvider"

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
  <React.StrictMode>
    <ChakraUIProvider>
      <App />
    </ChakraUIProvider>
  </React.StrictMode>,
)
