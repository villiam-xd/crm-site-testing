namespace server.Records;

public record CreateMessageRequest()
{
    public string Message { get; set; }
    public string Username { get; set; }
};