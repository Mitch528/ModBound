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
using System.Text;
using System.Threading.Tasks;

namespace ModBoundLib.Helpers
{
    public static class IOHelper
    {

        public static void DeleteDirectory(string dirToDelete)
        {
            string[] files = Directory.GetFiles(dirToDelete);
            string[] dirs = Directory.GetDirectories(dirToDelete);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(dirToDelete, true);
        }

        public static void CopyDirectory(string source, string dest)
        {

            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);

            DirectoryInfo sdInfo = new DirectoryInfo(source);

            FileInfo[] sfInfos = sdInfo.GetFiles();

            foreach (FileInfo fInfo in sfInfos)
            {
                fInfo.CopyTo(Path.Combine(dest, fInfo.Name), true);
            }

            foreach (DirectoryInfo dInfo in sdInfo.GetDirectories())
            {
                CopyDirectory(dInfo.FullName, Path.Combine(dest, dInfo.Name));
            }

        }

    }
}
