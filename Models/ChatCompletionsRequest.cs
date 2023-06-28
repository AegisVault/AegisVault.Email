using System.Collections.Generic;

namespace AegisVault.Email.Models;

public class OpenAIAPIRequest
{
    public string model { get; set; }
    public List<Message> messages { get; set; }
    public double temperature { get; set; }
    public int max_tokens { get; set; }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}