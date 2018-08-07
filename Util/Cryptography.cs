using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace DataPlane.Util
{
    public class Cryptography
    {
        public struct PasswordSet
        {
            public PasswordSet(string hash, string salt)
            {
                Hash = hash; Salt = salt;
            }

            public string Hash { get; set; }
            public string Salt { get; set; }
        }

        public static PasswordSet GetPasswordHash(string password)
        {
            // generate a 128-bit salt using a secure PRNG
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return GetPasswordHash(password, salt);
        }

        public static PasswordSet GetPasswordHash(string password, string salt)
        {
            // get byte array from string
            var bytes =Convert.FromBase64String(salt);

            return GetPasswordHash(password, bytes);
        }

        public static PasswordSet GetPasswordHash(string password, byte[] salt) 
        {

            // derive a 256-bit subkey (use HMACSHA1 with 10,000 iterations)
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return new PasswordSet(
                hashed, 
                Convert.ToBase64String(salt)
            );

        }

        public static string Base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}