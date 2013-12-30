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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModBound.Misc
{
    public static class AssemblyHelper
    {

        //Credit: http://stackoverflow.com/a/2567233
        public static string GetCurrentExecutingFile()
        {
            return new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
        }

        public static string GetCurrentExecutingDirectory()
        {
            return Path.GetDirectoryName(GetCurrentExecutingFile());
        }

    }
}
