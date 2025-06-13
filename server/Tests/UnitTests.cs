using Xunit;
using server;
using server.Classes;
using server.Enums;

public class UnitTests
{
  [Fact]
  public void Message_Constructor_SetsProperties()
  {
    // Given
    // Input för testdatan
    string text = "message text";
    string sender = "message sender";
    string username = "message username";
    DateTime time = new DateTime(2025, 5, 31, 5, 10, 20);
    // When
    // Skapar ny instans av Message med testdatan
    Message message = new Message(text, sender, username, time);
    // Then
    // Kollar att den nya instansen har fått de korrekta värdena
    Assert.Equal(text, message.Text);
    Assert.Equal(sender, message.Sender);
    Assert.Equal(username, message.Username);
    Assert.Equal(time, message.Time);
  }

  [Fact]
  public void Employee_Constructor_SetsProperties()
  {
    // Given
    // Input för testdatan
    int id = 1;
    string username = "Employee";
    string firstname = "firstname";
    string lastname = "lastname";
    string email = "employee@email.com";
    Role role = Role.USER;
    // When
    // Skapar ny instans av Employee med testdatan
    Employee employee = new Employee(id, username, firstname, lastname, email, role);
    // Then
    // Kollar att den nya instansen har fått de korrekta värdena
    Assert.Equal(id, employee.Id);
    Assert.Equal(username, employee.Username);
    Assert.Equal(firstname, employee.Firstname);
    Assert.Equal(lastname, employee.Lastname);
    Assert.Equal(email, employee.Email);
    Assert.Equal(role, employee.Role);
  }



  [Theory]
  [InlineData(Role.USER)]
  [InlineData(Role.ADMIN)]
  [InlineData(Role.GUEST)]
  public void User_Constructor_SetsAllRolesCorrectly(Role role)
  {
    // Given
    // Nödvändig inmatningsdata för att skapa en user
    int id = 1;
    string username = "username";
    int companyId = 5;
    string company = "company";
    // When
    // Skapar en ny användare med inmatade rollen
    User user = new User(id, username, role, companyId, company);
    // Then
    // Kollar att rolen har assignats korrekt
    Assert.Equal(role, user.Role);
  }
}