namespace server.Records;

public record LoginGuestRequest()
{
    public string Email { get; set; }
    public int ChatId { get; set; }
};