import { useContext, useEffect, useState } from "react"
import { GlobalContext } from "../../GlobalContext.jsx"
import EmployeeList from "../../components/EmployeeList.jsx"

export default function EmployeeView() {
    const [employees, setEmployees] = useState([])
    const { user } = useContext(GlobalContext)

    async function getEmployees() {
        const response = await fetch(`/api/users/bycompany/${user.company}`, { credentials: "include" })
        const result = await response.json()

        if (response.ok) {
            console.log(result)
            setEmployees(result.employees)
        } else {
            alert("No employes found.")
        }
    }

    useEffect(() => {
        getEmployees()
    }, [user])

    return <div className="EmployeeView">
        <h1>Employees</h1>
        <EmployeeList employeeList={employees} />
    </div>
}
