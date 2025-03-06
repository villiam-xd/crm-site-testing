import { useContext } from "react"
import { GlobalContext } from "../GlobalContext.jsx"

export default function Home() {
    const { user } = useContext(GlobalContext)
    
    return !user ?
        <h1>CRM Home</h1>
        :
        <h1>Welcome {user.username}!</h1>
}
