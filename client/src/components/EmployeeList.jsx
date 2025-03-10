import EmployeeCard from "./EmployeeCard.jsx";

export default function EmployeeList({ employeeList }) {
    return <div className="list">
        <div className="employeeListHeaders">
            <p>USERNAME</p>
            <p>ROLE</p>
        </div>
        {employeeList.map(employee => <EmployeeCard key={employee.id} employee={employee} />)}
    </div>
}
