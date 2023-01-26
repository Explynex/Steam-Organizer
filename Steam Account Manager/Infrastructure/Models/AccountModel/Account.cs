﻿using Steam_Account_Manager.Infrastructure.Parsers;
using System;
using System.Web.UI.WebControls;

namespace Steam_Account_Manager.Infrastructure.Models.AccountModel
{
    [Serializable]
    internal sealed class Account
    {
        #region Properties

        #region Summaries properties
        public ulong? SteamId64 { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Nickname { get; set; }
        public string AvatarHash { get; set; }
        public string ProfileURL { get; set; }
        public bool IsProfilePublic { get; set; }
        public DateTime AccCreatedDate { get; set; }
        public string CreatedDateImageUrl { get; set; }
        public bool ContainParseInfo { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public UInt32? SteamId32   => Utils.Common.SteamId64ToSteamId32(SteamId64);
        public string AvatarFull   => ContainParseInfo ? $"https://avatars.akamai.steamstatic.com/{AvatarHash}_full.jpg" : "/Images/default_steam_profile.png";
        public string AvatarMedium => ContainParseInfo ? $"https://avatars.akamai.steamstatic.com/{AvatarHash}_medium.jpg" : "/Images/default_steam_profile.png";
        #endregion

        #region Bans properties
        public bool EconomyBanned { get; set; }
        public bool CommunityBanned { get; set; }
        public int VacBansCount { get; set; }
        public int GameBansCount { get; set; }
        public int DaysSinceLastBan { get; set; }
        #endregion

        #region Games properties
        public int? SteamLevel { get; set; }
        public int? TotalGamesCount { get; set; }
        public int? GamesPlayedCount { get; set; }
        public int? HoursOnPlayed { get; set; }
        public string CountGamesImageUrl { get; set; }
        public CSGO CsgoStats { get; set; }
        #endregion

        #region Other info properties
        public string Note { get; set; }
        public string EmailLogin { get; set; }
        public string EmailPass { get; set; }
        public string RockstarEmail { get; set; }
        public string RockstarPass { get; set; }
        public string UplayEmail { get; set; }
        public string UplayPass { get; set; }
        public string OriginEmail { get; set; }
        public string OriginPass { get; set; }
        public string AuthenticatorPath { get; set; }  
        #endregion

        #endregion

        //Default
        public Account(string login, string password, ulong steamId64)
        {
            this.Login     = login;
            this.Password  = password;
            this.SteamId64 = steamId64;

            SteamParser steamParser  = new SteamParser(steamId64);
            steamParser.AccountParse().GetAwaiter().GetResult();

            this.Nickname            = steamParser.Nickname;
            this.AvatarHash          = steamParser.AvatarHash;
            this.ProfileURL          = steamParser.ProfileURL;
            this.IsProfilePublic     = steamParser.IsProfilePublic;
            this.AccCreatedDate      = steamParser.CreatedDateTime;
            this.CreatedDateImageUrl = steamParser.CreatedDateImageUrl;
            this.LastUpdateTime      = DateTime.Now;

            this.EconomyBanned    = steamParser.EconomyBanned;
            this.CommunityBanned  = steamParser.CommunityBanned;
            this.VacBansCount     = steamParser.VacBansCount;
            this.GameBansCount    = steamParser.GameBansCount;
            this.DaysSinceLastBan = steamParser.DaysSinceLastBan;

            this.SteamLevel         = steamParser.SteamLevel;
            this.TotalGamesCount    = steamParser.TotalGamesCount;
            this.GamesPlayedCount   = steamParser.GamesPlayedCount;
            this.HoursOnPlayed      = steamParser.HoursOnPlayed;
            this.CountGamesImageUrl = steamParser.CountGamesImageUrl;
            this.ContainParseInfo   = true;

            this.CsgoStats = new CSGO();
        }

        //Update account counstructor
        public Account(string login, string password, ulong steamId64, string note, string emailLogin, string emailPass,
             string rockstarEmail, string rockstarPass, string uplayEmail, string uplayPass,
             string originEmail, string originPass, CSGO csgoStats, string authenticatorPath,string nick = null) : this(login,password,steamId64)
        {
            if (csgoStats != null)
                this.CsgoStats = csgoStats;

            this.AuthenticatorPath = authenticatorPath;

            this.Note = note;
            this.EmailLogin = emailLogin;
            this.EmailPass = emailPass;
            this.RockstarEmail = rockstarEmail;
            this.RockstarPass = rockstarPass;
            this.UplayEmail = uplayEmail;
            this.UplayPass = uplayPass;
            this.OriginPass = originPass;
            this.OriginEmail = originEmail;

#if !DEBUG
            if(nick.GetHashCode() != this.Nickname.GetHashCode())
            {
                foreach (var item in Config.Properties.RecentlyLoggedUsers) 
                {
                    if (item.SteamID64 == this.SteamId64 && item.Nickname != this.Nickname)
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Config.Properties.RecentlyLoggedUsers[Config.Properties.RecentlyLoggedUsers.IndexOf(item)] = new RecentlyLoggedUser()
                            {
                                SteamID64 = item.SteamID64,
                                IsRewritable = item.IsRewritable,
                                Nickname = this.Nickname
                            };
                            App.Tray.TrayListUpdate();
                            Config.SaveProperties();
                        });
                        break;
                    }
                }
            }
#endif


        }

        public Account(string login, string password, string nickname, bool anonymAccount)
        {
            this.ContainParseInfo = false;

            this.Login = login;
            this.Password = password;
            this.Nickname = nickname;
            this.LastUpdateTime = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Nickname} [{SteamId32}]";
        }
    }
}
