import { useState } from "react"
import { useParams } from "react-router"

export default function IssueForm() {
    const [subject, setSubject] = useState("Övrigt")
    const [message, setMessage] = useState("")
    const { company_name } = useParams()

    async function createIssue(formData) {
        const response = await fetch(`/api/issue/create/${company_name}`, {
            method: "post",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                email: formData.get("email"),
                title: formData.get("title"),
                subject: subject,
                message: message
            })
        })
    }

    return <form action={createIssue}>
        <label>
            Your email
            <input type="email" name="email" required />
        </label>
        <label>
            Title
            <input type="text" name="title" minLength={3} required />
        </label>
        <label>
            Subject
            <select defaultValue="Övrigt" onChange={e => setSubject(e.target.value)}>
                <option value="Övrigt">Övrigt</option>
                <option value="Skadda">Skadda</option>
                <option value="Reklamation">Reklamation</option>
            </select>
        </label>
        <label>
            Message
            <textarea name="message"
                onChange={e => setMessage(e.target.value)}
                rows={8}
                cols={50}
                placeholder="Describe your issue."
                maxLength={1000}
                minLength={10}
                wrap="hard"
                required />
        </label>


        <button type="submit">Create issue</button>
    </form>
}
