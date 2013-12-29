// This file is part of ModBound.
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
using ModBound.ViewModels;
using MahApps.Metro.Controls.Dialogs;

namespace ModBound.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {

            var window = Window.GetWindow(this) as MetroWindow;

            if (window != null)
            {

                var prog = await window.ShowProgressAsync("Working....", "Authenticating!");
                prog.SetIndeterminate();

                bool result = await ((LoginViewModel)DataContext).Login(Username.Text, Password.Password, RememberMe.IsChecked.HasValue && RememberMe.IsChecked.Value);


                await prog.CloseAsync();


                if (result)
                {
                    ((LoginViewModel)(DataContext)).TryClose(true);
                }
                else
                {
                    await window.ShowMessageAsync("Error", "The username or password entered was incorrect!");
                }

            }

        }
    }
}
