using System.Text;
using Microsoft.Extensions.Configuration;
using SecretSanta.Configuration;
using SecretSanta.Email;
using SecretSanta.Extensions;
using SecretSanta.Encryption;

var settings = GetSettings();
List<Participant> participants = settings.Participants;
var encryptionProvider = new EncryptionProvider();
EmailService emailService = new EmailService(settings.Email);

if (participants.TrueForAll(participant => participant.SecretKey == null))
{
    await SecretSanta(participants)
        .ConfigureAwait(false);
}
else
{
    await RevealSantas(participants)
        .ConfigureAwait(false);
}

async Task SecretSanta(List<Participant> participants)
{
    encryptionProvider.CreateSecretKeys(participants);
    string secretKey = encryptionProvider.CreateGlobalSecretKey(participants);
    participants.Shuffle(secretKey.GetHashCode());
    var pairs = CreatePairs(participants);
    string encryptedList = await encryptionProvider.CreateEncryptedListAsync(pairs, secretKey);
    
    await File.WriteAllTextAsync("encrypted.txt", encryptedList)
        .ConfigureAwait(false);

    await SendEmailsAsync(pairs)
        .ConfigureAwait(false);
}

async Task RevealSantas(List<Participant> participants)
{
    string encryptedList = await File.ReadAllTextAsync("encrypted.txt")
        .ConfigureAwait(false);
    
    string secretKey = encryptionProvider.CreateGlobalSecretKey(participants);
    
    string decryptedList = await encryptionProvider.DecryptListAsync(encryptedList, secretKey)
        .ConfigureAwait(false);
    
    Console.OutputEncoding = Encoding.UTF8;
    Console.WriteLine(decryptedList);
}

static Settings GetSettings()
{
    IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", true);
    
    IConfiguration configuration = builder.Build();
    var settings = new Settings();
    configuration.Bind(settings);

    return settings;
}

static IEnumerable<(Participant, Participant)> CreatePairs(List<Participant> participants)
{
    for (int i = 0; i < participants.Count - 1; i++)
    {
        yield return (participants[i], participants[i + 1]);
    }

    yield return (participants.Last(), participants.First());
}

async Task SendEmailsAsync(IEnumerable<(Participant, Participant)> pairs)
{
    await emailService.Authenticate()
        .ConfigureAwait(false);
    
    foreach (var pair in pairs)
    {
        await emailService.Send(pair)
            .ConfigureAwait(false);
    }
}

