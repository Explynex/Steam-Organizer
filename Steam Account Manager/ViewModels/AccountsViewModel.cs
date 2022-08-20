﻿using System;
using Steam_Account_Manager.ViewModels.View;
using Steam_Account_Manager.Infrastructure;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Steam_Account_Manager.ViewModels
{

    internal class AccountsViewModel : ObservableObject
    {
        public AsyncRelayCommand UpdateDatabaseCommand { get; set; }
        private RelayCommand _addAccountWindowCommand;
        public RelayCommand NoButtonCommand { get; set; }
        public RelayCommand YesButtonCommand { get; set; }
        public RelayCommand RestoreAccountCommand { get; set; }
        public RelayCommand RestoreAccountDatabaseCommand { get; set; }
        public RelayCommand StoreAccountDatabaseCommand { get; set; }

        private static CryptoBase database;
        private string _accountName;
        private string _searchBoxText;
        private int _accountId;
        public static int TempId;

        public static event EventHandler ConfirmBannerChanged;
        private static bool _confirmBanner;

        public static bool ConfirmBanner
        {
            get => _confirmBanner;
            set
            {
                _confirmBanner = value;
                ConfirmBannerChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public string AccountName
        {
            get => _accountName;
            set
            {
                _accountName = value;
                OnPropertyChanged();
            }
        }

        public int AccountId
        {
            get => _accountId;
            set
            {
                _accountId = value;
                OnPropertyChanged();
            }
        }

        public static event EventHandler AccountTabViewsChanged;
        private static ObservableCollection<AccountTabView> _accountTabViews;

        public static ObservableCollection<AccountTabView> AccountTabViews
        {
            get => _accountTabViews;
            set
            {
                _accountTabViews = value;
                AccountTabViewsChanged?.Invoke(null, EventArgs.Empty);
            }
        }


        public string SearchBoxText
        {
            get => _searchBoxText;
            set
            {
                _searchBoxText = value;
                FillAccountTabViews(database.SearchByNickname(value), _searchBoxText);
                OnPropertyChanged();
            }
        }

        public static void FillAccountTabViews(List<int> accountIndexes = null, string searchBoxText = null)
        {
            AccountTabViews.Clear();
            ConfirmBanner = false;
            if (accountIndexes == null) accountIndexes = database.SearchByNickname();
            foreach (var index in accountIndexes)
            {
                AccountTabViews.Add(new AccountTabView(index));
            }
            MainWindowViewModel.TotalAccounts = accountIndexes.Count;

        }

        public static void RemoveAccount(ref int id)
        {
            ConfirmBanner = true;
            TempId = id;
        }


        private async Task UpdateDatabase()
        {
            try 
            {
                MainWindowViewModel.IsEnabledForUser = false;
                await Task.Factory.StartNew(() =>
                {
                    for (int i = 0; i < database.Accounts.Count; i++)
                    {
                        if (!database.Accounts[i].ContainParseInfo) continue;
                        MainWindowViewModel.UpdatedAccountIndex = i + 1;
                        database.Accounts[i] = new Infrastructure.Base.Account(
                              database.Accounts[i].Login,
                             database.Accounts[i].Password,
                             database.Accounts[i].SteamId64,
                             database.Accounts[i].Note,
                             database.Accounts[i].EmailLogin,
                             database.Accounts[i].EmailPass,
                             database.Accounts[i].RockstarEmail,
                             database.Accounts[i].RockstarPass,
                             database.Accounts[i].UplayEmail,
                             database.Accounts[i].UplayPass,
                             database.Accounts[i].CsgoStats,
                             database.Accounts[i].AuthenticatorPath);
                    }
                    MainWindowViewModel.UpdatedAccountIndex = 0;
                    MainWindowViewModel.IsEnabledForUser = true;
                    Task.Run(() => MainWindowViewModel.NotificationView("Database has been updated"));
                    database.SaveDatabase();
                });
                if (SearchBoxText != null) FillAccountTabViews(database.SearchByNickname(SearchBoxText), SearchBoxText);
                else FillAccountTabViews();
            }
            catch
            {
                MainWindowViewModel.UpdatedAccountIndex = 0;
                MainWindowViewModel.IsEnabledForUser = true;
                await Task.Run(() => MainWindowViewModel.NotificationView("Error, no internet connection..."));
            }

        }

        public RelayCommand AddAccountWindowCommand
        {
            get { return _addAccountWindowCommand ?? new RelayCommand(o => { OpenAddAccountWindow(); }); }
        }

        private static void OpenAddAccountWindow()
        {
            AddAccountView addAccountWindow = new AddAccountView();
            ConfirmBanner = false;
            ShowDialogWindow(addAccountWindow);
        }

        private static bool? OpenCryptoKeyWindow(string path)
        {
            CryptoKeyWindow cryptoKeyWindow = new CryptoKeyWindow(false,path);
            cryptoKeyWindow.Owner = App.Current.MainWindow;
            cryptoKeyWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            return cryptoKeyWindow.ShowDialog();
        }

        public AccountsViewModel()
        {
            database = CryptoBase._database;

            AccountTabViews = new ObservableCollection<AccountTabView>();
            AccountName = "";
            AccountId = -1;
            UpdateDatabaseCommand = new AsyncRelayCommand(async (o) => await UpdateDatabase());
            NoButtonCommand = new RelayCommand(o =>
            {
                ConfirmBanner = false;
            });

            YesButtonCommand = new RelayCommand(o =>
            {
                database = CryptoBase.GetInstance();
                if (database.Accounts[TempId].AuthenticatorPath == null)
                {
                    database.Accounts.RemoveAt(TempId);
                    database.SaveDatabase();
                    FillAccountTabViews();
                }
                else
                {
                    Task.Run(() => MainWindowViewModel.NotificationView("You cannot delete an account with 2FA"));
                }
                ConfirmBanner = false;

            });

            RestoreAccountCommand = new RelayCommand(o =>
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "Steam Account (.sa)|*.sa"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    try
                    {
                        database.Accounts.Add((Infrastructure.Base.Account)Config.Deserialize(fileDialog.FileName,Config._config.UserCryptoKey));
                        FillAccountTabViews();
                        Task.Run(() => MainWindowViewModel.NotificationView("Account restored from file"));
                        database.SaveDatabase();
                    }
                    catch
                    {
                        if (OpenCryptoKeyWindow(fileDialog.FileName) == true)
                        {
                            database.Accounts.Add((Infrastructure.Base.Account)Config.Deserialize(fileDialog.FileName, Config.TempUserKey));
                            FillAccountTabViews();
                            Task.Run(() => MainWindowViewModel.NotificationView("Account restored from file"));
                            database.SaveDatabase();
                        }
                    }
                }
            });

            StoreAccountDatabaseCommand = new RelayCommand(o =>
            {
                var fileDialog = new SaveFileDialog
                {
                    Filter = "Steam Account Database (.sadb)|*.sadb"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    Config.Serialize(database.Accounts, fileDialog.FileName,Config._config.UserCryptoKey);
                    Task.Run(() => MainWindowViewModel.NotificationView("Database saved to file"));
                }
            });

            RestoreAccountDatabaseCommand = new RelayCommand(o =>
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "Steam Account (.sadb)|*.sadb"
                };
                if (fileDialog.ShowDialog() == true)
                {
                    try
                    {
                        database.Accounts = (List<Infrastructure.Base.Account>)Config.Deserialize(fileDialog.FileName,Config._config.UserCryptoKey);
                        FillAccountTabViews();
                        Task.Run(() => MainWindowViewModel.NotificationView("Database restored from file"));
                        database.SaveDatabase();
                    }
                    catch
                    {
                        if(OpenCryptoKeyWindow(fileDialog.FileName) == true)
                        {
                            database.Accounts = (List<Infrastructure.Base.Account>)Config.Deserialize(fileDialog.FileName, Config.TempUserKey);
                            FillAccountTabViews();
                            Task.Run(() => MainWindowViewModel.NotificationView("Database restored from file"));
                            database.SaveDatabase();
                        }
                    }
                }

            });


            FillAccountTabViews();
        }

    }
}
