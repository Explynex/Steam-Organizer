﻿using SteamOrganizer.Helpers;
using SteamOrganizer.Infrastructure;
using SteamOrganizer.MVVM.Core;
using SteamOrganizer.MVVM.Models;
using SteamOrganizer.MVVM.View.Controls;
using SteamOrganizer.MVVM.View.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace SteamOrganizer.MVVM.ViewModels
{
    internal sealed class AccountsViewModel : ObservableObject
    {
        public RelayCommand OpenProfileCommand { get; private set; }
        public RelayCommand ClearSearchBar { get; private set; }
        public RelayCommand RemoveAccountCommand { get; private set; }
        public RelayCommand PinAccountCommand { get; private set; }
        public RelayCommand AddAccountCommand { get; private set; }
        public RelayCommand SortCommand { get; private set; }
        public ObservableCollection<Account> Accounts => App.Config.Database;
        private ICollectionView AccountsCollectionView;

        private readonly string[] SortTypes = new string[]
        {
            nameof(Account.SteamID64), nameof(Account.AddedDate), nameof(Account.LastUpdateDate), nameof(Account.SteamLevel)
        };

        private readonly AccountsView View;

        private string _searchBarText;
        public string SearchBarText
        {
            get => _searchBarText;
            set => SetProperty(ref _searchBarText, value);
        }

        #region Sorting

        private bool _sortDirection;
        public bool SortDirection
        {
            get => _sortDirection;
            set
            {
                _sortDirection = value;
                Sort();
            }

        }
        private int _sortByIndex = -1;
        public int SortByIndex
        {
            set
            {
                if (value.Equals(_sortByIndex))
                {
                    return;
                }

                if (value == 0)
                {
                    View.SortComboBox.SelectedIndex = -1;
                    return;
                }

                _sortByIndex = value;
                Sort();
            }
        }

        private void Sort()
        {
            if (AccountsCollectionView.SortDescriptions.Count != 0)
            {
                AccountsCollectionView.SortDescriptions.Clear();
            }

            if (_sortByIndex == -1)
            {
                return;
            }

            AccountsCollectionView.SortDescriptions.Add(new SortDescription(SortTypes[_sortByIndex - 1], _sortDirection ? ListSortDirection.Descending : ListSortDirection.Ascending));
        } 

        #endregion

        #region Global DB Actions
        private void OnFailedDatabaseLoading(object sender, System.EventArgs e)
        {
            // request password for exists db
            if (System.IO.File.Exists(App.DatabasePath))
            {
                App.MainWindowVM.OpenPopupWindow(new AuthenticationView(App.DatabasePath, OnSuccessDecrypt, true), App.FindString("av_title"), OnInstallationCanceled);
            }
#if !DEBUG
            // request password for new db
            else if (Config.DatabaseKey == null)
            {
                MainWindow.OpenPopupWindow(new MVVM.View.Controls.AuthenticationView(), FindString("word_registration"), OnInstallationCanceled);
            }
#endif


            void OnSuccessDecrypt(object content, byte[] key)
            {
                if (content is ObservableCollection<Account> db)
                {
                    App.Config.Database = db;
                    App.Config.DatabaseKey = key;
                    App.Config.Save();
                    App.Config.SaveDatabase();
                }
            }

            void OnInstallationCanceled()
            {
                if (App.Config.DatabaseKey == null)
                    App.Shutdown();
            }
        }
        private void OnDatabaseLoaded()
        {
            //We load images in another thread so as not to block the UI thread
            Utils.InBackground(() =>
            {
                for (int i = 0; i < Accounts.Count; i++)
                {
                    Accounts[i].LoadImage();
                }

                App.STAInvoke(() =>
                {
                    AccountsCollectionView = CollectionViewSource.GetDefaultView(Accounts);
                   // AccountsCollectionView.Refresh();
                });
            });
        } 
        #endregion

        private void OnRemovingAccount(object param)
        {
            var acc = (param as FrameworkElement).DataContext as Account;
            QueryPopup.GetPopup($"{App.FindString("acv_confirmDel")} {acc.Nickname}",Removed)
                .OpenPopup(param as FrameworkElement,System.Windows.Controls.Primitives.PlacementMode.Bottom);

            void Removed()
            {
                Accounts.Remove(acc);
                App.Config.SaveDatabase();
            }
        }

        private void OnPinningAccount(object param)
        {
            var acc = param as Account;

            var index = Accounts.IndexOf(acc);

            #region Pinning

            if (!acc.Pinned)
            {
                //Since there is a fixed element in front of it - the current one is already in its place and does not require moving
                if (index != 0 && !Accounts[index - 1].Pinned)
                {
                    for (int i = 0, pinnedCount = 0; i < index; i++)
                    {
                        if (!Accounts[i].Pinned)
                        {
                            acc.UnpinIndex = index - pinnedCount;
                            Accounts.Move(index, i);
                            break;
                        }
                        pinnedCount++;
                    }
                }

                acc.Pinned = true;
                return;
            }

            #endregion

            #region Unpin

            int? minAllowedIndex = null;

            for (int i = index + 1; i < Accounts.Count; i++)
            {
                if (!Accounts[i].Pinned)
                {
                    minAllowedIndex = i;
                    break;
                }
            }

            try
            {
                // If there is only 1 element in the collection or only 1 is pinned and it was the first item before pinning
                if (Accounts.Count == 1 || (minAllowedIndex.HasValue && minAllowedIndex.Value - 1 == 0 && acc.UnpinIndex == 0))
                {
                    return;
                }

                //No places/all items are pinned or unpinning index greater than items count
                if (!minAllowedIndex.HasValue || acc.UnpinIndex >= Accounts.Count)
                {
                    Accounts.Move(index, Accounts.Count - 1);
                    return;
                }

                Accounts.Move(index, acc.UnpinIndex < minAllowedIndex.Value - 1 ? minAllowedIndex.Value - 1 : acc.UnpinIndex);
            }
            finally
            {
                acc.Pinned = false;
                acc.UnpinIndex = 0;
            } 

            #endregion
        }

        private void OnAddingAccount(object param)
        {
            App.MainWindowVM.OpenPopupWindow(new AccountAddingView(),"New account");
        }

        public AccountsViewModel(AccountsView owner)
        {
            View = owner;
            OpenProfileCommand = new RelayCommand((o) 
                => Process.Start($"{WebBrowser.SteamProfilesHost}{SteamIdConverter.SteamID32ToID64((uint)o)}").Dispose());

            ClearSearchBar       = new RelayCommand((o) => SearchBarText = null);
            RemoveAccountCommand = new RelayCommand(OnRemovingAccount);
            PinAccountCommand    = new RelayCommand(OnPinningAccount);
            AddAccountCommand    = new RelayCommand(OnAddingAccount);

            SortCommand = new RelayCommand((o) =>
            {
                SortByIndex = -1;
            });

            App.Config.DatabaseLoaded += OnDatabaseLoaded;
            if (!App.Config.LoadDatabase())
            {
                App.Current.MainWindow.SourceInitialized += OnFailedDatabaseLoading;
                return;
            }
        }

    }
}


      