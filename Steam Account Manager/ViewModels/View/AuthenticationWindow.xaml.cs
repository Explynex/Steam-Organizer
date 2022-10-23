﻿using Steam_Account_Manager.Infrastructure;
using System.Windows;

namespace Steam_Account_Manager.ViewModels.View
{
    public partial class AuthenticationWindow : Window
    {
        private int errorCounter = 3;
        private bool mainWindow;
        public AuthenticationWindow(bool mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (Password.Password.Length > 30)
            {
                ErrorBlock.Text = (string)FindResource("aw_longPass");
                return;
            }

            if (Utilities.Sha256(Password.Password + Config.GetDefaultCryptoKey) == Config.Properties.Password)
            {
                if (!mainWindow) DialogResult = true;
                else
                {
                    try
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                        this.Hide();

                    }
                    catch
                    {
                        Application.Current.MainWindow = new CryptoKeyWindow(true)
                        {
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };
                        this.Close();
                        Application.Current.MainWindow.Show();
                    }
                }
            }

            else
            {
                if (errorCounter > 1)
                {
                    ErrorBlock.Text = (string)FindResource("aw_invalidPass");
                    errorCounter--;
                }
                else if (errorCounter == 1)
                {
                    ErrorBlock.Text = (string)FindResource("aw_manyAttempts");
                    errorCounter--;
                }
                else
                {
                    if (mainWindow) App.Shutdown();
                    else DialogResult = false;
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow) App.Shutdown();
            else DialogResult = false;
        }

        private void noConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ResetBorder.Visibility = Visibility.Hidden;
        }

        private void yesConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            ResetBorder.Visibility = Visibility.Hidden;
            System.IO.File.Delete(App.WorkingDirectory + "\\config.dat");
            Config.ClearProperties();
            try
            {
                Config.GetAccountsInstance();
                Application.Current.MainWindow = new MainWindow();
                Application.Current.MainWindow.Show();
                this.Hide();

            }
            catch
            {
                var cryptoKeyWindow = new CryptoKeyWindow(true)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                this.Close();
                cryptoKeyWindow.Show();

            }
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            ResetBorder.Visibility = Visibility.Visible;
        }
    }
}
