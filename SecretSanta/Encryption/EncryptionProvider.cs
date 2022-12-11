using System.Security.Cryptography;
using System.Text;
using SecretSanta.Configuration;

namespace SecretSanta.Encryption;

internal sealed class EncryptionProvider
{
    public void CreateSecretKeys(IEnumerable<Participant> participants)
    {
        var random = new Random();
        foreach (var participant in participants)
        {
            participant.SecretKey = random.Next(100, 1000).ToString();
        }
    }

    public string CreateGlobalSecretKey(IEnumerable<Participant> participants)
    {
        return string.Concat(participants.OrderBy(participant => participant.Email)
            .Select(participant => participant.SecretKey));
    }
    
    public async Task<string> CreateEncryptedListAsync(IEnumerable<(Participant, Participant)> pairs, string secretKey)
    {
        using var aes = Aes.Create();
        aes.Key = CreateAesKey(secretKey);
        aes.IV = new byte[16];
        using var memoryStream = new MemoryStream();
        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        using var streamWriter = new StreamWriter(cryptoStream);
        foreach (var pair in pairs)
        {
            await streamWriter.WriteLineAsync($"{pair.Item1.Name} -> {pair.Item2.Name}")
                .ConfigureAwait(false);
        }

        await streamWriter.FlushAsync()
            .ConfigureAwait(false);

        await cryptoStream.FlushFinalBlockAsync()
            .ConfigureAwait(false);

        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public async Task<string> DecryptListAsync(string list, string secretKey)
    {
        var aes = Aes.Create();
        aes.Key = CreateAesKey(secretKey);
        aes.IV = new byte[16];
        byte[] bytes = Convert.FromBase64String(list);
        using var memoryStream = new MemoryStream(bytes);
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        using var streamReader = new StreamReader(cryptoStream);

        return await streamReader.ReadToEndAsync()
            .ConfigureAwait(false);
    }

    private byte[] CreateAesKey(string key)
    {
        byte[] trimmed = new byte[32];
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        Array.Copy(keyBytes, trimmed, Math.Min(keyBytes.Length, 32));
    
        return trimmed;
    }
}