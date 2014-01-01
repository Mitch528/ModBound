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
using MahApps.Metro.Controls.Dialogs;
using ModBound.ViewModels;
using ModBoundLib;
using ModBoundLib.Responses;

namespace ModBound.Views
{
    /// <summary>
    /// Interaction logic for RegisterView.xaml
    /// </summary>
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {

            var window = Window.GetWindow(this) as MetroWindow;

            if (window != null)
            {

                var prog = await window.ShowProgressAsync("Working....", "Registering!");
                prog.SetIndeterminate();

                Exception exception = null;

                try
                {

                    RegisterResponse resp = await ((RegisterViewModel)DataContext).Register(Username.Text, Password.Password, Email.Text);

                    await prog.CloseAsync();

                    if (resp.ResponseCode != ResponseCodes.Error)
                    {

                        ModBoundApi.Default.SetLoginDetails(Username.Text, Password.Password);
                        
                        ((RegisterViewModel)(DataContext)).TryClose(true);

                    }
                    else
                    {

                        if (resp.Errors == null || resp.Errors.Length == 0)
                            await window.ShowMessageAsync("Error", "The username/email has already been used!");
                        else
                            await window.ShowMessageAsync("Error", string.Join("\n", resp.Errors));
                    
                    }

                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception != null)
                {

                    await prog.CloseAsync();

                    await window.ShowMessageAsync("Error", exception.Message);
                
                }

            }

        }
    }
}
