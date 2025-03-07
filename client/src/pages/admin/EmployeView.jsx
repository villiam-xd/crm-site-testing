import { useContext, useEffect, useState } from "react"
import { GlobalContext } from "../../GlobalContext.jsx"

export default function EmployeView() {
    const [employes, setEmployes] = useState([])
    const { user } = useContext(GlobalContext)

    async function getEmployes() {
        const response = await fetch(`/api/users/bycompany/${user.company}`, { credentials: "include" })
        const result = await response.json()

        if (response.ok) {
            setEmployes(result)
        } else {
            alert("No employes found.")
        }
    }

    useEffect(() => {
        getEmployes()
    }, [])

    return <>
        <h1>Employes</h1>
        {
            employes.map(employe => {
                <p key={employe.id}>{employe.username}</p>
            })
        }
    </>
}
