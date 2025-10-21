namespace DrawTogether.Email;

public class EmailSettings
{
    public required string MailgunDomain { get; set; }
    
    public required string MailgunApiKey { get; set; }
    
    public required string FromAddress { get; set; }
    
    public required string FromName { get; set; }
}