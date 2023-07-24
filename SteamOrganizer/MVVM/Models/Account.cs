﻿using SteamOrganizer.Helpers;
using SteamOrganizer.Infrastructure;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using static SteamOrganizer.Helpers.SteamParser;

namespace SteamOrganizer.MVVM.Models
{
    [Serializable]
    internal sealed class Account : INotifyPropertyChanged
    {
        [JsonProperty(Required = Required.Always)]
        public string Nickname { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Login { get; set; }

        public string Password { get; set; }


        #region Summaries
        public uint? AccountID => SteamID64.HasValue ? (uint?)(SteamID64 - SteamIdConverter.SteamID64Indent) : null;
        public ulong? SteamID64 { get; set; }
        public bool IsFullyParsed => SteamID64 != null;
        public string AvatarHash { get; set; }
        public byte VisibilityState { get; set; }
        public string VanityURL { get; set; }
        public int? SteamLevel { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime AddedDate { get; }
        public float? YearsOfService => CreatedDate == null ? null : (float?)((DateTime.Now - CreatedDate.Value).TotalDays / 365.25);
        #endregion

        #region Bans
        public bool HaveCommunityBan { get; set; }
        public int VacBansCount { get; set; }
        public int GameBansCount { get; set; }
        public int DaysSinceLastBan { get; set; }
        public int EconomyBan { get; set; }
        #endregion

        #region Games
        public int GamesCount { get; set; }
        public int PlayedGamesCount { get; set; }
        public ushort GamesBadgeBoundary { get; set; }
        public float HoursOnPlayed { get; set; }
        #endregion

        public string Note { get; set; }


        private SteamAuth _authenticator;
        public SteamAuth Authenticator 
        {
            get => _authenticator;
            set
            {
                _authenticator = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Authenticator)));
            }
        }

        public int UnpinIndex;
        private bool _pinned;
        public bool Pinned 
        {
            get => _pinned;
            set
            {
                _pinned = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Pinned)));
            }
        }

        [field: NonSerialized]
        public BitmapImage AvatarBitmap { get; set; }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized]
        public bool IsCurrentlyUpdating { get; set; }


        public void LoadImage(string hash = null)
            => AvatarBitmap = CachingManager.GetCachedAvatar(hash ?? AvatarHash, 0, 0,size : EAvatarSize.medium);

        public string GetProfileUrl() 
            => SteamID64 == null ? WebBrowser.SteamHost : WebBrowser.SteamProfilesHost + SteamID64.ToString();

        public void OpenInBrowser(string hostPath = null)
            => Process.Start(GetProfileUrl() + hostPath).Dispose();

        public void InvokePropertyChanged(string property)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

        public void InvokeBannerPropertiesChanged()
        {
            InvokePropertyChanged(nameof(Nickname));
            InvokePropertyChanged(nameof(AccountID));
            InvokePropertyChanged(nameof(CreatedDate));
            InvokePropertyChanged(nameof(SteamLevel));
            InvokePropertyChanged(nameof(YearsOfService));
            InvokePropertyChanged(nameof(HaveCommunityBan));
            InvokePropertyChanged(nameof(VacBansCount));
            InvokePropertyChanged(nameof(GameBansCount));
            InvokePropertyChanged(nameof(EconomyBan));
            InvokePropertyChanged(nameof(AvatarBitmap));
        }

        public async Task<bool> RetrieveInfo(bool markUpdate = false)
        {
            try
            {
                IsCurrentlyUpdating = true;

                var prevHash = this.AvatarHash;

                if (await ParseInfo(this) != EParseResult.OK)
                    return false;

                if (prevHash != this.AvatarHash)
                    LoadImage();

                if (markUpdate)
                    LastUpdateDate = DateTime.Now;

                return true;
            }
            finally
            {
                IsCurrentlyUpdating = false;
            }
        }
        #region Constructors

        [JsonConstructor]
        private Account() { }

        public Account(string login, string password)
        {
            LoadImage();
            this.AddedDate = DateTime.Now;
            this.Nickname  = this.Login = login;
            this.Password  = password;
        }

        public Account(string login, string password, ulong steamID64)
        {
            this.AddedDate = DateTime.Now;
            this.Nickname  = this.Login = login;
            this.Password  = password;
            this.SteamID64 = steamID64;
        }

        public Account(string login, string password, uint accountId) :
            this(login, password, accountId + SteamIdConverter.SteamID64Indent)
        { }

        #endregion
    }
}
