import { Outlet } from "react-router";
import Navbar from "./components/Navbar.jsx";

export default function Layout() {
    return <>
        <header>
            <Navbar />
        </header>
        <main>
            <Outlet />
        </main>
    </>
}
