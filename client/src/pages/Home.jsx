import { useContext, useEffect, useState } from "react"
import { GlobalContext } from "../GlobalContext.jsx"
import { NavLink } from "react-router"

export default function Home() {
    const { user } = useContext(GlobalContext)
    const [companies, setCompanies] = useState([])

    async function getCompanies() {
        const response = await fetch(`/api/companies`, { credentials: "include" })
        const result = await response.json()

        if (response.ok) {
            setCompanies(result.companies)
        } else {
            alert(result.message)
        }

    }

    useEffect(() => {
        getCompanies()
    }, [])

    return <div id="startpage">
        <h2>All our partners</h2>
        <div id="companiesList">
            {
                companies.map(company => <NavLink to={`/${company}/issueform`}>{company}</NavLink>)
            }
        </div>
    </div>
}
