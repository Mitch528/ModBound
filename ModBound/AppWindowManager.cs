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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using MahApps.Metro.Controls;
using MahApps.Metro.Native;
using ModBound.ViewModels;

namespace ModBound
{
    public class AppWindowManager : WindowManager
    {

        static readonly ResourceDictionary[] resources;

        static AppWindowManager()
        {
            resources = new[] 
            {
                new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml", UriKind.RelativeOrAbsolute) },
                new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml", UriKind.RelativeOrAbsolute) },
                new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml", UriKind.RelativeOrAbsolute) },
                new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml", UriKind.RelativeOrAbsolute) },
                new ResourceDictionary { Source = new Uri("pack://application:,,,/MahApps.Metro;component/Styles/Controls.AnimatedTabControl.xaml", UriKind.RelativeOrAbsolute) },
                new ResourceDictionary { Source = new Uri("pack://application:,,,/ModBound;component/Resources/Icons.xaml", UriKind.RelativeOrAbsolute)}
            };
        }


        protected override Window EnsureWindow(object model, object view, bool isDialog)
        {

            MetroWindow window = view as MetroWindow;

            if (window == null)
            {

                window = new MetroWindow
                {
                    Content = view,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.CanMinimize,
                    Foreground = Brushes.White,
                    ShowMinButton = true,
                    ShowMaxRestoreButton = false,
                    BorderThickness = new Thickness(0)
                };

                window.Background = new SolidColorBrush(Color.FromArgb(255, 21, 21, 21));

                foreach (ResourceDictionary resourceDictionary in resources)
                {
                    window.Resources.MergedDictionaries.Add(resourceDictionary);
                }

                window.SetValue(View.IsGeneratedProperty, true);

                Window owner = InferOwnerOf(window);

                if (owner != null)
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.Owner = owner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

            }
            else
            {
                Window owner2 = InferOwnerOf(window);
                if (owner2 != null && isDialog)
                {
                    window.Owner = owner2;
                }

            }

            MBViewModel vm = model as MBViewModel;

            if (vm != null)
            {

                Binding bind = new Binding();
                bind.Source = vm.WindowTitle;

                window.SetBinding(Window.TitleProperty, bind);

            }

            if (model is MainViewModel)
            {

                MainViewModel mvm = (MainViewModel)model;

                //window.Icon = new BitmapImage(new Uri("pack://application:,,,/ModBound;component/Resources/ModBoundIcon.ico"));

                Button settingsButton = new Button();
                settingsButton.Content = "Settings";

                Button accountButton = new Button();
                accountButton.Content = "Account";

                Binding loginLogoutBinding = new Binding("LoginButtonText")
                {
                    Source = model
                };

                Button loginLogoutButton = new Button();
                loginLogoutButton.SetBinding(ContentControl.ContentProperty, loginLogoutBinding);
                loginLogoutButton.Click += (sender, e) => mvm.OpenLoginDialog();

                Binding registerButtonBinding = new Binding("RegisterButtonVisible")
                {
                    Source = mvm
                };

                Button registerButton = new Button();
                registerButton.Content = "Register";
                registerButton.SetBinding(UIElement.VisibilityProperty, registerButtonBinding);
                registerButton.Click += (sender, e) => mvm.OpenRegisterDialog();

                WindowCommands wc = new WindowCommands();
                //wc.Items.Add(settingsButton);
                //wc.Items.Add(accountButton);
                wc.Items.Add(loginLogoutButton);
                wc.Items.Add(registerButton);

                window.WindowCommands = wc;

            }

            return window;
        }

    }
}
