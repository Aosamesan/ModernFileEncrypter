﻿using System;
using System.Numerics;
using System.Threading.Tasks;

namespace ModernFileEncrypter.RSA
{
    internal class RSAEncrypter
    {
        private static RSAEncrypter instance;
        public static RSAEncrypter Instance
        {
            get
            {
                if (instance == null)
                    instance = new RSAEncrypter();
                return instance;
            }
        }

        private RSAEncrypter()
        {

        }

        public async Task<RSAKeyPair[]> GenerateKeyPairs(BigInteger p, BigInteger q)
        {
            BigInteger n = p * q;
            BigInteger pin = (p - 1) * (q - 1);
            var encryptKey = new RSAKeyPair();
            var decryptKey = new RSAKeyPair();
            encryptKey.number = decryptKey.number = n;
            encryptKey.value = await CalculateEncryptionKey(pin);
            decryptKey.value = await CalculateDecryptionKey(pin, encryptKey.value);

            return new RSAKeyPair[] { encryptKey, decryptKey };
        }

        private async Task<BigInteger> CalculateEncryptionKey(BigInteger pin)
        {
            BigInteger cnt = 0;
            BigInteger start = (BigInteger)(Math.Sqrt((int)BigInteger.ModPow(pin, 1, int.MaxValue)));
            for (BigInteger i = start; i < pin; i++)
            {
                if (BigInteger.GreatestCommonDivisor(i, pin) == 1)
                    return i;
                if ((cnt++) % 35000 == 0)
                    await Task.Delay(1);
            }
            return pin - 1;
        }

        private async Task<BigInteger> CalculateDecryptionKey(BigInteger pin, BigInteger encryptKeyValue)
        {
            BigInteger cnt = 0;
            for (BigInteger i = pin >> 5; i < pin; i++)
            {
                if ((i * encryptKeyValue) % pin == 1)
                    return i;
                if ((cnt++) % 35000 == 0)
                    await Task.Delay(1);
            }
            return pin - 1;
        }

        public BigInteger Encrypt(RSAKeyPair keyPair, BigInteger b)
        {
            return BigInteger.ModPow(b, keyPair.value, keyPair.number);
        }
    }

    [Serializable]
    public struct RSAKeyPair
    {
        public BigInteger number;
        public BigInteger value;

        public RSAKeyPair(BigInteger number, BigInteger value)
        {
            this.number = number;
            this.value = value;
        }
    }
}
