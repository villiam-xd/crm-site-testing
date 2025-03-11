import { useState } from "react"

export default function EmployeeCard({ employee }) {
    const [hide, setHide] = useState(false)
    const [editForm, setEditForm] = useState(false)

    function toggleMenus() {
        if (hide) {
            setEditForm(false)
        }
        setHide(!hide)
    }

    async function updateEmployee(formdata) {
        const response = await fetch(`/api/users/${employee.id}`, {
            method: "put",
            headers: { "Content-Type": "application/json" },
            credentials: "included",
            body: JSON.stringify({
                firstname: formdata("firstname"),
                lastname: formdata("lastname"),
                email: formdata("email"),
                role: formdata("role"),
            })
        })

        if (response.ok) {
            alert("Good")
        } else {
            alert("Bad")
        }
    }

    return <div className="employeeCard">
        <div className={"employeeAttributes" + (hide ? " isActive" : "")} onClick={toggleMenus}>
            <p>{employee.firstname}</p>
            <p>{employee.lastname}</p>
            <p>{employee.email}</p>
            <p>{employee.role}</p>
            <p>{employee.username}</p>
        </div>
        {
            editForm ?
                <form action={updateEmployee} className="editForm">
                    <input type="text" name="firstname" placeholder="New Firstname" />
                    <input type="text" name="lastname" placeholder="New Lastname" />
                    <input type="email" name="email" placeholder="New Email" />
                    <select defaultValue={employee.role}>
                        <option value="USER">USER</option>
                        <option value="ADMIN">ADMIN</option>
                    </select>
                    <button type="submit">Update Employee</button>
                </form>
                :
                null
        }
        {
            hide ?
                <div className="employeeMenu">
                    <button onClick={() => setEditForm(!editForm)}>Edit information</button>
                </div>
                :
                null
        }
    </div>
}
