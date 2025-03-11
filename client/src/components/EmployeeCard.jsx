export default function EmployeeCard({ employee }) {
    return <div className="employeeCard">
        <p>{employee.email}</p>
        <p>{employee.username}</p>
        <p>{employee.firstname}</p>
        <p>{employee.lastname}</p>
        <p>{employee.role}</p>
    </div>
}
