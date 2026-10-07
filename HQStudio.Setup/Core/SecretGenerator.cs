using System.Security.Cryptography;

namespace HQStudio.Setup.Core;

public static class SecretGenerator
{
    // Without look-alike characters and without anything that needs quoting in a .env file.
    public const string Alphabet = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        return new string(chars);
    }
}
