﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using SteamOrganizer.Infrastructure.Models;
using System.Threading.Tasks;
using System.IO;
using SteamOrganizer.Helpers;
using SteamOrganizer.Infrastructure;

namespace SteamOrganizer.Storages
{

    internal enum ESideBarState : byte
    {
        Hidden = 0,
        Open = 70,
        Expanded = 200
    }

    [Serializable]
    internal class GlobalStorage
    {
        [field: NonSerialized]
        public ObservableCollection<Account> Accounts { get; set; }

        #region UI Meta information
        /// <summary>
        /// To indicate whether you need to update the config
        /// </summary>
        [field: NonSerialized]
        public bool IsPropertiesChanged { get; set; }

        public ESideBarState SideBarState { get; set; } = ESideBarState.Expanded;
        #endregion

        public bool MinimizeOnStart { get; set; }
        public bool MinimizeToTray { get; set; }

        private byte[] _databaseKey;
        public byte[] DatabaseKey
        {
            set
            {
                if (value.Length != 32)
                    throw new InvalidDataException(nameof(DatabaseKey));

                _databaseKey = value;
            }
        }

        public bool Save()
            =>  SerializationManager.Serialize(this, App.ConfigPath, Utils.GetLocalMachineGUID());
        

        public static GlobalStorage Load()
        {
            if (File.Exists(App.ConfigPath) &&
                SerializationManager.Deserialize(App.ConfigPath,out GlobalStorage result,Utils.GetLocalMachineGUID()))
            {
                return result;
            }

            return new GlobalStorage();
        }
    }
}
