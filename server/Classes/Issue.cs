using server.Enums;
using System.Text.Json.Serialization;
namespace server.Classes;

public class Issue
{
    public int Id { get; set; }
    public String CompanyName { get; set; }
    public String CustomerEmail { get; set; }
    public String Subject { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public IssueState State { get; set; }
    public String Title { get; set; }

    public Issue(int id, String companyName, string customerEmail, string subject, IssueState state, string title)
    {
        Id = id;
        CompanyName = companyName;
        CustomerEmail = customerEmail;
        Subject = subject;
        State = state;
        Title = title;
    }
}