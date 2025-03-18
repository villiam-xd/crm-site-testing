import { useEffect, useState } from "react"
import { useParams } from "react-router"

export default function Chat() {
    const { issue_id } = useParams()
    const [issue, setIssue] = useState(null)
    const [messages, setMessages] = useState([])

    async function sendMessage(formData) {
        const message = formData.get("message")
        if (message.trim().length == 0) {
            return alert("Message is to short.")
        }

        const response = await fetch(`/api/issues/${issue_id}/messages`, {
            method: "post",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                message: message,
                username: issue.customerEmail
            })
        })

        const result = await response.json()
        console.log(result)

        if (response.ok) {
            getMessages()
        } else {
            alert(result.message)
        }
    }

    async function getIssueInfo() {
        const response = await fetch(`/api/issues/${issue_id}`, { credentials: "include" })
        const result = await response.json()

        console.log(result)
        if (response.ok) {
            setIssue(result)
            getMessages()
        }

    }

    async function getMessages() {
        const response = await fetch(`/api/issues/${issue_id}/messages`, { credentials: "include" })
        const result = await response.json()

        console.log(result)
        if (response.ok) {
            setMessages(result.messages)
        }
    }

    useEffect(() => {
        getIssueInfo()
    }, [])

    return !issue ?
        <h1>No issue was found</h1>
        :
        <div id="issueChat">
            <h1>{issue.companyName} - Chat</h1>
            <div id="issueInfo">
                <p id="issueTitle">Title: {issue.title}</p>
                <p id="issueState">State: {issue.state}</p>
            </div>
            <div id="chat">
                <div id="messageList">
                    {
                        messages.map((message, index) => <div key={index} className="message">
                            <p className="messageText">{message.text}</p>
                            <p className="messageInfo"><span className="messageUsername">{message.username}</span> - <span className="messageTime">10 mars 2025 23:12</span></p>
                        </div>)
                    }
                </div>
                <form action={sendMessage} id="sendMessage">
                    <input type="text" name="message" placeholder="Type message" required />
                    <button type="submit">Send</button>
                </form>
            </div>
        </div>
}


