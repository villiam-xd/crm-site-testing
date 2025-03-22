import { BrowserRouter, Route, Routes } from "react-router";
import Layout from "./Layout.jsx";
import Home from "./pages/Home.jsx";
import Register from "./pages/Register.jsx";
import Login from "./pages/Login.jsx";
import EmployeeView from "./pages/admin/EmployeeView.jsx";
import NewEmployee from "./pages/admin/NewEmployee.jsx";
import IssueForm from "./pages/issue/IssueForm.jsx";
import EditForm from "./pages/admin/EditForm.jsx";
import IssueView from "./pages/employee/IssueView.jsx";
import CustomerChat from "./pages/customer/CustomerChat.jsx";
import EmployeeChat from "./pages/employee/EmployeeChat.jsx";

export default function Router() {
  return <>
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Layout />} >
          <Route index element={<Home />} />
          <Route path="/register" element={<Register />} />
          <Route path="/login" element={<Login />} />
          <Route path="/:company_name/issueform" element={<IssueForm />} />
          <Route path="/chat/:issue_id" element={<CustomerChat />} />
          <Route path="/employee/issues" element={<IssueView />} />
          <Route path="/employee/chat/:issue_id" element={<EmployeeChat />} />
          <Route path="/admin/employees" element={<EmployeeView />} />
          <Route path="/admin/employees/new" element={<NewEmployee />} />
          <Route path="/admin/form/edit" element={<EditForm />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </>
}
