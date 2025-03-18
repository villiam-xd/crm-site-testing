import { useState } from "react"

export default function SubjectCard({ number, subject, getSubjects }) {
    const [edit, setEdit] = useState(true)

    async function updateSubject(formData) {
        const newName = formData.get("newName")
        console.log(newName)
        if (subject.trim().toLowerCase() == newName.trim().toLowerCase()) {
            return alert("No change has been made.")
        }

        const response = await fetch(`/api/forms/updateSubject`, {
            method: "put",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                oldName: subject,
                newName: newName
            })
        })

        console.log(response.status)

        if (response.ok) {
            getSubjects()
        }
    }

    return <div className="subjectCard">
        <div className="attributes">
            <p>{number + 1}</p>
            {
                !edit ?
                    <form action={updateSubject} className="updateSubjectForm">
                        <input type="text" name="newName" defaultValue={subject} disabled={edit} />
                        <button type="submit" className="subjectUpdateButton">Save</button>
                    </form>
                    :
                    <p>{subject}</p>
            }
            <div className="subjectButtons">
                <button className="subjectEditButton" onClick={() => setEdit(!edit)}>&#9998;</button>
                <button className="removeButton" onClick={e => console.log(e.target.value)}>&#10006;</button>
            </div>
        </div>
    </div>
}
