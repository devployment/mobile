﻿using System;
using Bit.App.Abstractions;
using XLabs.Ioc;
using Bit.App.Enums;

namespace Bit.App.Models
{
    public class CipherString
    {
        private string _decryptedValue;

        public CipherString(string encryptedString)
        {
            if(string.IsNullOrWhiteSpace(encryptedString) || !encryptedString.Contains("|"))
            {
                throw new ArgumentException(nameof(encryptedString));
            }

            var headerPieces = encryptedString.Split('.');
            string[] encPieces;
            if(headerPieces.Length == 2 && Enum.TryParse(headerPieces[0], out EncryptionType encType))
            {
                EncryptionType = encType;
                encPieces = headerPieces[1].Split('|');
            }
            else if(headerPieces.Length == 1)
            {
                encPieces = headerPieces[0].Split('|');
                EncryptionType = encPieces.Length == 3 ? EncryptionType.AesCbc128_HmacSha256_B64 : EncryptionType.AesCbc256_B64;
            }
            else
            {
                throw new ArgumentException("Malformed header.");
            }

            switch(EncryptionType)
            {
                case EncryptionType.AesCbc256_B64:
                    if(encPieces.Length != 2)
                    {
                        throw new ArgumentException("Malformed encPieces.");
                    }
                    InitializationVector = encPieces[0];
                    CipherText = encPieces[1];
                    break;
                case EncryptionType.AesCbc128_HmacSha256_B64:
                case EncryptionType.AesCbc256_HmacSha256_B64:
                    if(encPieces.Length != 3)
                    {
                        throw new ArgumentException("Malformed encPieces.");
                    }
                    InitializationVector = encPieces[0];
                    CipherText = encPieces[1];
                    Mac = encPieces[2];
                    break;
                case EncryptionType.RsaOaep_Sha256_B64:
                    if(encPieces.Length != 1)
                    {
                        throw new ArgumentException("Malformed encPieces.");
                    }
                    CipherText = encPieces[0];
                    break;
                default:
                    throw new ArgumentException("Unknown encType.");
            }

            EncryptedString = encryptedString;
        }

        public CipherString(EncryptionType encryptionType, string initializationVector, string cipherText, string mac = null)
        {
            if(string.IsNullOrWhiteSpace(initializationVector))
            {
                throw new ArgumentNullException(nameof(initializationVector));
            }

            if(string.IsNullOrWhiteSpace(cipherText))
            {
                throw new ArgumentNullException(nameof(cipherText));
            }

            EncryptionType = encryptionType;
            EncryptedString = string.Format("{0}.{1}|{2}", (byte)encryptionType, initializationVector, cipherText);

            if(!string.IsNullOrWhiteSpace(mac))
            {
                EncryptedString = string.Format("{0}|{1}", EncryptedString, mac);
            }

            CipherText = cipherText;
            InitializationVector = initializationVector;
            Mac = mac;
        }

        public EncryptionType EncryptionType { get; private set; }
        public string EncryptedString { get; private set; }
        public string InitializationVector { get; private set; }
        public string CipherText { get; private set; }
        public string Mac { get; private set; }
        public byte[] InitializationVectorBytes => InitializationVector == null ?
            null : Convert.FromBase64String(InitializationVector);
        public byte[] CipherTextBytes => Convert.FromBase64String(CipherText);
        public byte[] MacBytes => Mac == null ? null : Convert.FromBase64String(Mac);

        public string Decrypt()
        {
            if(_decryptedValue == null)
            {
                var cryptoService = Resolver.Resolve<ICryptoService>();
                _decryptedValue = cryptoService.Decrypt(this);
            }

            return _decryptedValue;
        }
    }
}
