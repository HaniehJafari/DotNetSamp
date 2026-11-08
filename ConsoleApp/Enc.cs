using System.Text;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        
        using var aes = Aes.Create();
        byte[] key = aes.Key; 

        Console.WriteLine("Enter sensitive data:");
        var input = Console.ReadLine();

        
        var encrypted = Encrypt(input, key);
        Console.WriteLine($"Encrypted (IV + Ciphertext): {encrypted}");

        var decrypted = Decrypt(encrypted, key);
        Console.WriteLine($"Decrypted Data: {decrypted}");
    }

    static string Encrypt(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;

      
        aes.Mode = CipherMode.CBC; 
        aes.Padding = PaddingMode.PKCS7;
        byte[] iv = aes.IV;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var memoryStream = new MemoryStream();
        
      
        memoryStream.Write(iv, 0, iv.Length);
        
        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            cryptoStream.Write(plainBytes, 0, plainBytes.Length);
        }

       
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    static string Decrypt(string encryptedText, byte[] key)
    {
       
        var encryptedBytes = Convert.FromBase64String(encryptedText);
        using var aes = Aes.Create();

       
        byte[] iv = new byte[16];
        Array.Copy(encryptedBytes, 0, iv, 0, iv.Length);

        using var memoryStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(key, iv), CryptoStreamMode.Write))
        {
           
            cryptoStream.Write(encryptedBytes, iv.Length, encryptedBytes.Length - iv.Length);
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }
}
