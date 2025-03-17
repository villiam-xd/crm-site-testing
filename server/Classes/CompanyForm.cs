namespace server.Classes;

public class CompanyForm
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; }
    public List<string> Subjects { get; set; }

    public CompanyForm(int companyId, string companyName, List<string> subjects)
    {
        CompanyId = companyId;
        CompanyName = companyName;
        Subjects = subjects;
    }
}