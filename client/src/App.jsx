import { BrowserRouter, Route } from "react-router";
import Layout from "./Layout.jsx";
import Home from "./pages/Home";

export default function App() {
  return <>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />} >
          <Route index element={<Home />} />
        </Route>
      </Routes>
    </BrowserRouter>
    </>
}
