namespace ChatApi.Models;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Content { get; set; }
    public DateTime TimeStamp { get; set; }
}
