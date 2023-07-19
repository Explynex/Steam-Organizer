﻿using SteamOrganizer.Helpers;
using SteamOrganizer.Infrastructure;
using SteamOrganizer.MVVM.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace SteamOrganizer.Storages
{

    internal enum ESideBarState : byte
    {
        Hidden = 0,
        Open = 70,
        Expanded = 200
    }

    [Serializable]
    internal sealed class GlobalStorage
    {
        [field: NonSerialized]
        public ObservableCollection<Account> Database { get; set; }

        /// <summary>
        /// Called only upon successful loading from a file
        /// </summary>
        [field: NonSerialized]
        public event Action DatabaseLoaded;


        #region UI Meta information
        /// <summary>
        /// To indicate whether you need to update the config
        /// </summary>
        [field: NonSerialized]
        public bool IsPropertiesChanged { get; set; }

        public ESideBarState SideBarState { get; set; } = ESideBarState.Expanded;

        public double MainWindowCornerRadius { get; set; } = 9d;
        #endregion

        public bool MinimizeOnStart { get; set; }
        public bool MinimizeToTray { get; set; }
        public string SteamApiKey { get; set; }

        private byte[] _databaseKey;
        public byte[] DatabaseKey
        {
            get => _databaseKey;
            set
            {
                if (value.Length != 32)
                    throw new InvalidDataException(nameof(DatabaseKey));

                _databaseKey = value;
            }
        }

        #region Storing/restoring
        public bool Save()
            => FileCryptor.Serialize(this, App.ConfigPath, Utils.GetLocalMachineGUID());


        public static GlobalStorage Load()
        {
            if (File.Exists(App.ConfigPath) &&
                FileCryptor.Deserialize(App.ConfigPath, out GlobalStorage result, Utils.GetLocalMachineGUID()))
            {
                return result;
            }

            return new GlobalStorage();
        }


        public bool LoadDatabase()
        {
            if (!File.Exists(App.DatabasePath))
            {
                Database = new ObservableCollection<Account>();
                return true;
            }

            if (FileCryptor.Deserialize(App.DatabasePath, out ObservableCollection<Account> result, _databaseKey))
            {
                Database = result;
                DatabaseLoaded?.Invoke();
                return true;
            }

            Database = new ObservableCollection<Account>();
            return false;
        }

        private int _waitingCounter = 0;

        /// <param name="timeout">Useful for frequent save prompts like textboxes</param>
        public async void SaveDatabase(int timeout = 0)
        {

            // We need to check the counter to know about the calls that happen while waiting.
            if (_waitingCounter != 0)
            {
                // Maximum 2 wait cycles so it's not too long
                if (_waitingCounter < 2)
                {
                    _waitingCounter++;
                }

                return;
            }

            if (timeout != 0)
            {
                for (_waitingCounter = 1; _waitingCounter > 0; _waitingCounter--)
                {
                    await Task.Delay(timeout);
                }
            }

            _databaseKey.ThrowIfNull();

            FileCryptor.Serialize(Database, App.DatabasePath, _databaseKey);
        } 
        #endregion
    }
}
