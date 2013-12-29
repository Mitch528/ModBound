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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ModBound.Misc
{
    public static class BrowserProtocol
    {


        //Credit: http://www.codingvision.net/miscellaneous/c-register-a-url-protocol
        public static void RegisterModBound()
        {

            try
            {

                RegistryKey key = Registry.ClassesRoot.OpenSubKey("ModBound");

                if (key == null)
                {

                    key = Registry.ClassesRoot.CreateSubKey("ModBound");

                    string loc = AssemblyHelper.GetCurrentExecutingFile();


                    key.SetValue(string.Empty, "URL: ModBound Protocol");
                    key.SetValue("URL Protocol", string.Empty);

                    key = key.CreateSubKey(@"shell\open\command");
                    key.SetValue(string.Empty, loc + " " + "%1");

                }

                key.Close();
                key.Dispose();

            }
            catch (Exception ex)
            {
            }

        }

    }
}
