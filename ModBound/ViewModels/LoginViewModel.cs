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
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ModBound.Properties;
using ModBoundLib;
using ModBoundLib.Extensions;

namespace ModBound.ViewModels
{
    public class LoginViewModel : MBViewModel
    {
        public override string WindowTitle
        {
            get { return "Login"; }
        }

        public LoginViewModel()
        {
            Username = Settings.Default.Username;
        }

        public void CancelLogin()
        {
            TryClose(false);
        }

        public async Task<bool> Login(string username, string password, bool remember = false)
        {

            var api = ModBoundApi.Default;

            api.SetLoginDetails(username, password);

            if (remember)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[1024];

                rng.GetBytes(buffer);
                string salt = BitConverter.ToString(buffer);

                rng.Dispose();

                Settings.Default.Entropy = salt;
                Settings.Default.Username = username;
                Settings.Default.Password = password.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(salt));
                Settings.Default.Save();
            }

            return await api.SignInAsync();

        }

        private string _username;

        public string Username
        {
            get
            {
                return _username;
            }
            set
            {

                if (_username == value)
                    return;

                _username = value;

                NotifyOfPropertyChange(() => Username);

            }
        }

    }
}
