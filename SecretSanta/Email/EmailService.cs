using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using SecretSanta.Configuration;

namespace SecretSanta.Email;

public class EmailService
{
    private GmailService? gmailService;
    private readonly string email;
    public EmailService(string email)
    {
        this.email = email;
    }

    public async Task Authenticate()
    {
        using var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read);
        var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.Load(stream).Secrets,
            new[] { GmailService.Scope.MailGoogleCom },
            email, CancellationToken.None);

        gmailService = new GmailService(new BaseClientService.Initializer
        {
            HttpClientInitializer = userCredential
        });
    }
    
    public async Task Send((Participant, Participant) pair)
    {
        string messageText =
            $"To: {pair.Item1.Email}\r\nSubject: Secret santa\r\nContent-Type: text/html;charset=utf-8\r\n\r\n{ConstructMessage(pair.Item2.Name, pair.Item1.SecretKey)}";
        
        Message message = new Message
        {
            Raw = Base64UrlEncode(messageText)
        };
        var request = gmailService!.Users.Messages.Send(message, "me");
        
        message = await request.ExecuteAsync()
            .ConfigureAwait(false);

        await Task.Delay(1000)
            .ConfigureAwait(false);

        await this.gmailService.Users.Messages.Delete("me", message.Id)
            .ExecuteAsync()
            .ConfigureAwait(false);
    }
    
    private string Base64UrlEncode(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(inputBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
    }

    private string ConstructMessage(string name, string key)
    {
        return "<h3>Привет!</h3>"
               + "<span>Ты стал тайным сантой!<span>"
               + "<br/>"
               + $"<span>Имя участника, которому ты вручишь подарок: <b>{name}</b></span>"
               + "<br/>"
               + $"<span>Твой секретный ключ: <b>{key}</b></span>";
    }
}