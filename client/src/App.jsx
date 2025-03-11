import { BrowserRouter, Route, Routes } from "react-router";
import Layout from "./Layout.jsx";
import Home from "./pages/Home.jsx";
import Register from "./pages/Register.jsx";
import Login from "./pages/Login.jsx";
import EmployeeView from "./pages/admin/EmployeeView.jsx";
import NewEmployee from "./pages/admin/NewEmployee.jsx";

export default function App() {
  return <>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />} >
          <Route index element={<Home />} />
          <Route path="/register" element={<Register />} />
          <Route path="/login" element={<Login />} />
          <Route path="/admin/employees" element={<EmployeeView />} />
          <Route path="/admin/employees/new" element={<NewEmployee />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </>
}
