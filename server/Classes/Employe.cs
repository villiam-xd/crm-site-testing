using server.Enums;
using System.Text.Json.Serialization;

namespace server.Classes;

public class Employe
{
    public int Id { get; set; }
    public string Username { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))] 
    public Role Role { get; set; }

    public Employe(int id, string username, Role role)
    {
        Id = id;
        Username = username;
        Role = role;
    }
}