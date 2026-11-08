using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
namespace EncriptionTest
{

    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }




    public class Program
    {

        public static string Encrypt (byte[] key, string plainText)
        {
            var aes =  Aes.Create();
            aes.Key = key;
            aes.GenerateIV();
            byte[] iv = aes.IV;
            aes.Mode= CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
           
            byte[] plainTextByte= Encoding.UTF8.GetBytes(plainText);

          using var encryter=  aes.CreateEncryptor(key, iv);
            using MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(iv,0,iv.Length);

            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryter, CryptoStreamMode.Write))
            {
                cryptoStream.Write(plainTextByte, 0, plainTextByte.Length);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string Dencrypt(byte[] key, string ciherText)
        { 
        var ciher = Convert.FromBase64String(ciherText);
          using  var aes = Aes.Create();
            aes.Key = key;      
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte [] iv= new byte[16];
            Array.Copy(ciher, 0, iv,0,iv.Length);

            aes.IV = iv;
            using var dencryter = aes.CreateDecryptor(key, iv);

            using MemoryStream memoryStream = new MemoryStream();

            using (CryptoStream cryptoStream = new CryptoStream(memoryStream, dencryter, CryptoStreamMode.Write))
            {
                cryptoStream.Write(ciher, iv.Length, ciher.Length-iv.Length);
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());

        }
        

            static void Main(string[] args)
        {
            //string key = Environment.GetEnvironmentVariable("SecretKey") ??
            //    throw new Exception("There is not Env varibale with key");
            //byte[] byteKey = Encoding.UTF8.GetBytes(key);

            byte[] byteKey = RandomNumberGenerator.GetBytes(32);

            string fileName = "transavtionData.dat";

            //byte[] key2 = RandomNumberGenerator.GetBytes(32) ??
            //throw new Exception("There is not Env varibale with key");

            var transaction = new Transaction
            {
                Id = 1,
                Amount = 1_500_000,
                CardNumber = "6037991234567890",
                Description = "خرید کالا"
            };

            var j= JsonSerializer.Serialize(transaction);
            var enc = Encrypt(byteKey, j!);
            File.WriteAllText(fileName, enc);

            Console.WriteLine(enc);
           string eccc= File.ReadAllText(fileName);
            var dec = Dencrypt(byteKey, eccc);

           var dj= JsonSerializer.Deserialize<Transaction>(dec);
            Console.WriteLine(dj.CardNumber);

            
        }
    }
}
