﻿// This file is part of ModBound.
// 
// ModBound is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ModBound is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ModBound.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ModBound.ViewModels;
using wyDay.Controls;

namespace ModBound.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {

        private readonly AutomaticUpdaterBackend _au;

        public MainView()
        {

            InitializeComponent();

            _au = new AutomaticUpdaterBackend
            {
                GUID = "ModBound",
                UpdateType = UpdateType.Automatic
            };

            _au.UpdateSuccessful += _au_UpdateSuccessful;
            _au.ReadyToBeInstalled += _au_ReadyToBeInstalled;
            _au.ProgressChanged += _au_ProgressChanged;
            _au.CloseAppNow += (sender, e) => Dispatcher.Invoke(() => Application.Current.Shutdown());

            _au.Initialize();
            _au.AppLoaded();

            _au.ForceCheckForUpdate(true);

        }

        private void _au_ProgressChanged(object sender, int progress)
        {
        }

        private void _au_ReadyToBeInstalled(object sender, EventArgs e)
        {

            Dispatcher.InvokeAsync(async () =>
            {

                var metroWindow = (MetroWindow)Application.Current.MainWindow;

                var result = await metroWindow.ShowMessageAsync("Install?", "An update is ready to be installed. Install it now?", MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Affirmative)
                {
                    _au.InstallNow();
                }

            }).Wait();

        }

        private async void _au_UpdateSuccessful(object sender, SuccessArgs e)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            await metroWindow.ShowMessageAsync("Success", "ModBound has successfully updated!");

        }

        private void InstalledModsListView_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {

                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                ((MainViewModel)DataContext).InstallMod(files[0]);

            }

        }

        private void InstalledModsListView_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void InstalledModsListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }
    }
}