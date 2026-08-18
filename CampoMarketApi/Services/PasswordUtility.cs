using System.Security.Cryptography;

namespace CampoMarketApi.Services;

public static class PasswordUtility
{
    public static string Hash(string password)
    {
        var salt=RandomNumberGenerator.GetBytes(16);
        var hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,120_000,HashAlgorithmName.SHA256,32);
        return $"PBKDF2$120000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
    public static bool Verify(string password,string stored)
    {
        try { var p=stored.Split('$'); if(p.Length!=4||!int.TryParse(p[1],out var n)) return false;
            var salt=Convert.FromBase64String(p[2]); var expected=Convert.FromBase64String(p[3]);
            return CryptographicOperations.FixedTimeEquals(Rfc2898DeriveBytes.Pbkdf2(password,salt,n,HashAlgorithmName.SHA256,expected.Length),expected); }
        catch { return false; }
    }
}
