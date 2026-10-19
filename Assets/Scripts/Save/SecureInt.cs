using System;

namespace bison.core.Save
{
    internal struct SecureInt
    {
        private const int FirstChecksumPrime = 73856093;
        private const int SecondChecksumPrime = 19349663;

        private static readonly Random randomGenerator = new Random();

        private int obfuscatedValue;
        private int key;
        private int checksum;

        public static event Action Tampered;

        public SecureInt(int initialValue)
        {
            obfuscatedValue = 0;
            key = 0;
            checksum = 0;
            Value = initialValue;
        }

        public int Value
        {
            get
            {
                if (checksum != ComputeChecksum(obfuscatedValue, key)) Tampered?.Invoke();
                return obfuscatedValue ^ key;
            }
            set
            {
                key = randomGenerator.Next(int.MinValue, int.MaxValue);
                obfuscatedValue = value ^ key;
                checksum = ComputeChecksum(obfuscatedValue, key);
            }
        }

        private static int ComputeChecksum(int obfuscated, int keyValue)
        {
            return unchecked((obfuscated * FirstChecksumPrime) ^ (keyValue * SecondChecksumPrime));
        }
    }
}
