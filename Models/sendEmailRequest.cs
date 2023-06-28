namespace AegisVault.Email.Models;

public class sendEmailRequest
{
    public Brand brand { get; set; }
    public string documentType { get; set; }
    public string requiredContent { get; set; }
    public string aegisLink { get; set; }
    public string name { get; set; }
    public string accountNumber { get; set; }
    public string email { get; set; }
}
public class Brand
{
    public string brandname { get; set; }
    public string brandlogoURL { get; set; }
    public string brandPrimaryColor { get; set; }
    public string brandSecondaryColor { get; set; }
}