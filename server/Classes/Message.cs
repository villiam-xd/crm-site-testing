namespace server.Classes;

public class Message
{
    public String Text { get; set; }
    public String Sender { get; set; }
    public String Username { get; set; }

    public Message(string text, string sender, string username)
    {
        Text = text;
        Sender = sender;
        Username = username;
    }
}