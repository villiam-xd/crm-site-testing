import { useEffect, useState } from "react"
import SubjectList from "../../components/SubjectList.jsx"

export default function EditForm() {
    const [subjects, setSubjects] = useState([])

    async function getSubjects() {
        const response = await fetch(`/api/forms/subjects`, { credentials: "include" })
        const results = await response.json()

        console.log(results)

        if (response.ok) {
            setSubjects(results.subjects)
        } else {
            alert(result.message)
        }
    }

    useEffect(() => {
        getSubjects()
    }, [])

    return <div className="subjectView">
        <h1>Form Subjects</h1>
        <SubjectList subjectList={subjects} getSubjects={getSubjects} />
    </div>
}
