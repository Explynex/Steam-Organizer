﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Steam_Account_Manager.Infrastructure.Base;
using FuzzySharp;

namespace Steam_Account_Manager.Infrastructure
{
    [Serializable]
    internal class Config
    {
        private static Config config;

        private Config()
        {
            this.accountsDB = new List<Account>();
            SupportedThemes = new List<Themes>();
            SupportedThemes.Add(Themes.Dark);
            SupportedThemes.Add(Themes.Light);
            NoConfirmMode = TakeAccountInfo = AutoClose = false;
            Theme = SupportedThemes[0];
            SteamDirection = "";
        }
        public enum Themes
        {
            Dark,
            Light
        }

        public List<Account> accountsDB;
        public string SteamDirection { get; set; }
        public List<Themes> SupportedThemes { get; set; }
        
        private Themes _theme;

        //сюда добавляем темы
        public Themes Theme
        {
            get { return _theme; }
            set 
            {
                _theme = value;
                switch (value)
                {
                    case Themes.Dark: break;
                    case Themes.Light: break;
                    default:  break;
                }
            }
        }

        public bool NoConfirmMode;
        public bool TakeAccountInfo;
        public bool AutoClose;

        public void SaveChanges()
        {
            serialize(config);
        }

        public void Clear()
        {
            config = new Config();
            SaveChanges();
        }

        public List<int> searchByNickname(string nickname = "")
        {
            var foundAccountsIndexes = new List<int>();
            if (nickname != "")
            {
                for (int i = 0; i < accountsDB.Count; i++)
                {
                    if (accountsDB[i].nickname.ToLower().StartsWith(nickname) ||
                        Fuzz.Ratio(nickname.ToLower(), accountsDB[i].nickname.ToLower()) > 40)
                    {
                        foundAccountsIndexes.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < accountsDB.Count; i++)
                    foundAccountsIndexes.Add(i);
            }
            return foundAccountsIndexes;
        }

        public static Config GetInstance()
        {
            if(config == null)
            {
                if (File.Exists("config.dat"))
                {
                    config = deserialize();
                    config.Theme = config.Theme;
                }
                else config = new Config();
            }
            return config;
        }

        //Encrypting
        private const string CryptoKey = "Q3JpcHRvZ3JhZmlhcyBjb20gUmluamRhZWwgLyBBRVM=";
        private const int keySize = 256;
        private const int ivSize = 16; // block size is 128-bit

        private static void WriteObjectToStream(Stream outputStream, Object obj)
        {
            if (object.ReferenceEquals(obj, null)) return;
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(outputStream, obj);
        }
        private static object ReadObjectFromStream(Stream inputStream)
        {
            BinaryFormatter binForm = new BinaryFormatter();
            object obj = binForm.Deserialize(inputStream);
            return obj;
        }
        private static CryptoStream createEncryptionStream(byte[] key,Stream outputStream)
        {
            byte[] iv = new byte[ivSize];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetNonZeroBytes(iv);

            outputStream.Write(iv, 0, iv.Length);
            Rijndael rijndael = new RijndaelManaged();
            rijndael.KeySize = keySize;
            CryptoStream encryptor = new CryptoStream(outputStream,rijndael.CreateEncryptor(key, iv),CryptoStreamMode.Write);
            return encryptor;
        }
        public static CryptoStream CreateDecryptionStream(byte[] key, Stream inputStream)
        {
            byte[] iv = new byte[ivSize];

            if (inputStream.Read(iv, 0, iv.Length) != iv.Length)
            {
                throw new ApplicationException("Failed to read IV from stream.");
            }

            Rijndael rijndael = new RijndaelManaged();
            rijndael.KeySize = keySize;

            CryptoStream decryptor = new CryptoStream(
                inputStream,
                rijndael.CreateDecryptor(key, iv),
                CryptoStreamMode.Read);
            return decryptor;
        }
        private static void serialize(Config config)
        {
            byte[] key = Convert.FromBase64String(CryptoKey);

            using (FileStream file = new FileStream(Environment.CurrentDirectory + @"\config.dat", FileMode.Create))
            {
                using (CryptoStream cryptoStream = createEncryptionStream(key, file))
                {
                    WriteObjectToStream(cryptoStream, config);
                }
            }
        }
        private static Config deserialize()
        {
            byte[] key = Convert.FromBase64String(CryptoKey);

            using (FileStream file = new FileStream(Environment.CurrentDirectory + @"\config.dat", FileMode.Open))
            using (CryptoStream cryptoStream = CreateDecryptionStream(key, file))
            {
                return (Config)ReadObjectFromStream(cryptoStream);
            }
        }

    }
}
