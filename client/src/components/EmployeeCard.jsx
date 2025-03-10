export default function EmployeeCard({ employee, key }) {
    return <div key={key} className="employeeCard">
        <p>{employee.username}</p>
        <p>{employee.role}</p>
    </div>
}
