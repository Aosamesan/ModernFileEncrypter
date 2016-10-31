using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernFileEncrypter.RSA
{
    public class RSAByteEncrypter
    {
        #region Constant
        private static readonly int Prime;
        #endregion

        #region Base Encrypter
        private static RSAEncrypter BaseEncrypter { get; }
        #endregion

        #region For Singleton
        private static RSAByteEncrypter encrypter;
        public static RSAByteEncrypter Encrypter
        {
            get
            {
                if (encrypter == null)
                    encrypter = new RSAByteEncrypter();
                return encrypter;
            }
        }
        #endregion

        #region Constructor
        private RSAByteEncrypter() { }
        static RSAByteEncrypter()
        {
            Prime = 257;
            BaseEncrypter = RSAEncrypter.Instance;
        }
        #endregion

        #region Generate Keys
        public async Task<RSAKeyPair[]> GenerateKeyPairs(int n)
            => await BaseEncrypter.GenerateKeyPairs(n, Prime);
        #endregion

        #region Encrypt / Decrypt
        private byte ConvertByte(ref RSAKeyPair keyPair, byte b)
            => Convert.ToByte((int)(BaseEncrypter.Encrypt(keyPair, b) % Prime));
        #endregion

        #region CreateMap
        public async Task<IDictionary<byte, byte>> CreateByteMap(RSAKeyPair keyPair, IProgress<long> progress = null)
        {
            Dictionary<byte, byte> map = new Dictionary<byte, byte>();
            for(int i = 0; i < 256; i++)
            {
                byte idx = Convert.ToByte(i);
                byte b = ConvertByte(ref keyPair, idx);
                map.Add(idx, b);
                if (i % 16 == 0)
                {
                    progress?.Report(i);
                    await Task.Delay(1);
                }
            }
            return map;
        }
        #endregion
    }
}
