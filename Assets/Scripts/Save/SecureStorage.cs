using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using bison.core.Common;
using UnityEngine;

namespace bison.core.Save
{
    internal static class SecureStorage
    {
        private const string AppSecret = "ITS_SECRET_GAME";
        private const string KeySeparator = "|";
        private const string EncryptionLabel = "enc";
        private const string SigningLabel = "mac";
        private const byte FormatVersion = 1;
        private const int InitializationVectorSize = 16;
        private const int SignatureSize = 32;
        private const int HeaderSize = 1 + InitializationVectorSize + SignatureSize;

        private static byte[] encryptionKey;
        private static byte[] signingKey;

        public enum ReadResult { Ok, Missing, Invalid }

        // Keys

        private static void EnsureKeysCreated()
        {
            if (encryptionKey != null) return;

            using (SHA256 hashAlgorithm = SHA256.Create())
            {
                string secretText = AppSecret + KeySeparator + SystemInfo.deviceUniqueIdentifier;
                byte[] masterKey = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(secretText));

                encryptionKey = hashAlgorithm.ComputeHash(Concatenate(Encoding.UTF8.GetBytes(EncryptionLabel), masterKey));
                signingKey = hashAlgorithm.ComputeHash(Concatenate(Encoding.UTF8.GetBytes(SigningLabel), masterKey));
            }
        }

        // Write

        public static void Write(string path, string json)
        {
            EnsureKeysCreated();

            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] initializationVector;
            byte[] cipherBytes;

            using (Aes aesProvider = Aes.Create())
            {
                aesProvider.Key = encryptionKey;
                aesProvider.GenerateIV();
                initializationVector = aesProvider.IV;

                using (ICryptoTransform encryptor = aesProvider.CreateEncryptor())
                    cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            byte[] signature = ComputeSignature(initializationVector, cipherBytes);

            byte[] fileBytes = new byte[HeaderSize + cipherBytes.Length];
            fileBytes[0] = FormatVersion;
            Buffer.BlockCopy(initializationVector, 0, fileBytes, 1, InitializationVectorSize);
            Buffer.BlockCopy(signature, 0, fileBytes, 1 + InitializationVectorSize, SignatureSize);
            Buffer.BlockCopy(cipherBytes, 0, fileBytes, HeaderSize, cipherBytes.Length);

            string temporaryPath = path + SaveFiles.TemporarySuffix;
            File.WriteAllBytes(temporaryPath, fileBytes);

            if (File.Exists(path)) File.Copy(path, path + SaveFiles.BackupSuffix, true);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }

        // Read

        public static ReadResult TryRead(string path, out string json)
        {
            json = null;

            try
            {
                if (!File.Exists(path)) return ReadResult.Missing;

                byte[] fileBytes = File.ReadAllBytes(path);
                if (fileBytes.Length <= HeaderSize || fileBytes[0] != FormatVersion) return ReadResult.Invalid;

                EnsureKeysCreated();

                byte[] initializationVector = new byte[InitializationVectorSize];
                byte[] storedSignature = new byte[SignatureSize];
                byte[] cipherBytes = new byte[fileBytes.Length - HeaderSize];

                Buffer.BlockCopy(fileBytes, 1, initializationVector, 0, InitializationVectorSize);
                Buffer.BlockCopy(fileBytes, 1 + InitializationVectorSize, storedSignature, 0, SignatureSize);
                Buffer.BlockCopy(fileBytes, HeaderSize, cipherBytes, 0, cipherBytes.Length);

                byte[] expectedSignature = ComputeSignature(initializationVector, cipherBytes);
                if (!AreEqualInConstantTime(storedSignature, expectedSignature)) return ReadResult.Invalid;

                using (Aes aesProvider = Aes.Create())
                {
                    aesProvider.Key = encryptionKey;
                    aesProvider.IV = initializationVector;

                    using (ICryptoTransform decryptor = aesProvider.CreateDecryptor())
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        json = Encoding.UTF8.GetString(plainBytes);
                    }
                }

                return ReadResult.Ok;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogMessages.SaveReadFailed(exception.Message));
                return ReadResult.Invalid;
            }
        }

        // Helpers

        private static byte[] ComputeSignature(byte[] initializationVector, byte[] cipherBytes)
        {
            using (HMACSHA256 signer = new HMACSHA256(signingKey))
                return signer.ComputeHash(Concatenate(initializationVector, cipherBytes));
        }

        private static bool AreEqualInConstantTime(byte[] first, byte[] second)
        {
            if (first.Length != second.Length) return false;

            int difference = 0;
            for (int index = 0; index < first.Length; index++)
                difference |= first[index] ^ second[index];

            return difference == 0;
        }

        private static byte[] Concatenate(byte[] first, byte[] second)
        {
            byte[] combined = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, combined, 0, first.Length);
            Buffer.BlockCopy(second, 0, combined, first.Length, second.Length);
            return combined;
        }
    }
}
