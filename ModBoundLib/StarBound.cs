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
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using ModBoundLib.Extensions;
using ModBoundLib.Extensions.Json;
using ModBoundLib.Helpers;
using ModBoundLib.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModBoundLib
{
    public static class StarBound
    {

        public const string BootstrapFile = "bootstrap.config";

        public const string WindowsFolder = "win32";

        private readonly static string[] ExtensionsToIgnore = {
            ".png", ".wav", ".ogg", ".lua", ".ttf", ".dat"
        };

        private readonly static string[] ExtensionsToNotCopy = {
            ".modinfo"
        };

        public const string ModBoundDir = "ModBound";

        public const string ModInfoFileExt = ".modinfo";

        public const string ModBoundInfoFile = "modbound" + ModInfoFileExt;

        public const string BackupFormat = "MM-dd-yy-hh-mm-ss";

        public static void InstallMod(string installDir, string modFile)
        {

            if (!File.Exists(modFile))
                throw new FileNotFoundException("File not found!", modFile);

            if (Path.GetExtension(modFile) != ".zip")
                throw new NotSupportedException("Only .zip files are currently supported!");

            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempDir);

            using (ZipArchive archive = ZipFile.Open(modFile, ZipArchiveMode.Read))
            {
                archive.ExtractToDirectory(tempDir);
            }

            string modInfoDir = SearchForModInfo(tempDir);

            if (string.IsNullOrEmpty(modInfoDir))
                throw new Exception("Could not find modinfo!");

            DirectoryInfo midInfo = new DirectoryInfo(modInfoDir);

            string path = Path.Combine(installDir, WindowsFolder, GetModSource(installDir), midInfo.Name);

            if (Directory.Exists(path))
                IOHelper.DeleteDirectory(path);
            
            IOHelper.CopyDirectory(modInfoDir, path);


            //IOHelper.CopyDirectory(modInfoDir, Path.Combine(installDir, ModBoundDir, midInfo.Name));

            //RebuildMods(installDir, modBuildOrder ?? new List<ModBuildOrder>());


            //if (modDir != null)
            //{
            //    IOHelper.CopyDirectory(modDir.FullName, installDir);
            //}

            //foreach (string assetSource in StarBound.GetAssetSources(installDir))
            //{

            //    string path = Path.Combine(installDir, StarBound.WindowsFolder, assetSource);

            //    DirectoryInfo dInfo = new DirectoryInfo(path);

            //    DirectoryInfo dInfo2 = dirs.SingleOrDefault(p => p.Name == dInfo.Name);

            //    if (dInfo2 == null)
            //        continue;

            //    MergeModFiles(dInfo2, dInfo);

            //}

            IOHelper.DeleteDirectory(tempDir);

        }

        public static void BackupMod(string installDir, string modDirName, string backupTo)
        {

            string dir = Path.Combine(installDir, WindowsFolder, GetModSource(installDir), modDirName);

            Console.WriteLine(modDirName + " " + dir + " " + Path.Combine(backupTo, String.Format("{0}-{1}.zip", modDirName, DateTime.Now.ToString(BackupFormat))));

            ZipFile.CreateFromDirectory(dir, Path.Combine(backupTo, String.Format("{0}-{1}.zip", modDirName, DateTime.Now.ToString(BackupFormat))));

        }

        public static IEnumerable<ModInfo> GetInstalledMods(string installDir)
        {

            List<ModInfo> modInfos = new List<ModInfo>();

            string path = Path.Combine(installDir, WindowsFolder, GetModSource(installDir));

            foreach (DirectoryInfo dInfo in new DirectoryInfo(path).GetDirectories())
            {

                FileInfo[] fileInfos = dInfo.GetFiles();

                FileInfo fInfo = fileInfos.FirstOrDefault(p => p.Extension == ModInfoFileExt);

                if (fInfo == null)
                    continue;

                ModInfo mInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(fInfo.FullName));

                if (mInfo != null)
                {

                    mInfo.ModPath = dInfo.FullName;

                    modInfos.Add(mInfo);

                }

            }

            return modInfos;

        }

        public static string SearchForModInfo(string baseDir)
        {

            if (!Directory.Exists(baseDir))
                throw new DirectoryNotFoundException();

            DirectoryInfo baseDirInfo = new DirectoryInfo(baseDir);

            FileInfo[] files = baseDirInfo.GetFiles();

            if (files.Any(p => p.Extension.Equals(ModInfoFileExt, StringComparison.OrdinalIgnoreCase)))
            {
                return baseDirInfo.FullName;
            }

            string modInfoDir = String.Empty;

            DirectoryInfo[] dInfos = baseDirInfo.GetDirectories();

            foreach (DirectoryInfo dInfo in dInfos)
            {

                string temp = SearchForModInfo(dInfo.FullName);

                if (!string.IsNullOrEmpty(temp))
                {

                    modInfoDir = temp;

                    break;

                }

            }

            return modInfoDir;

        }

        public static void RebuildMods(string installDir, List<ModBuildOrder> modBuildOrder)
        {

            string modSource = GetModSource(installDir);

            string path = Path.Combine(installDir, ModBoundDir);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);


            if (!Directory.Exists(path))
            {

                Directory.CreateDirectory(path);

                return;

            }

            string modModBoundDir = Path.Combine(installDir, WindowsFolder, modSource, ModBoundDir);

            if (!Directory.Exists(modModBoundDir))
                Directory.CreateDirectory(modModBoundDir);

            string mbInfo = JsonConvert.SerializeObject(new ModInfo
            {
                Name = "ModBound",
                Path = ".",
                Dependencies = new string[0],
                Version = Settings.Default.StarBoundVersion
            });

            File.WriteAllText(Path.Combine(modModBoundDir, ModBoundInfoFile), mbInfo);

            DirectoryInfo msInfo = new DirectoryInfo(modModBoundDir);
            DirectoryInfo pathInfo = new DirectoryInfo(path);


            DirectoryInfo[] inMbDir = pathInfo.GetDirectories();

            if (modBuildOrder.Count > 0)
            {
                var oRev = modBuildOrder.OrderBy(p => p.BuildOrder).Reverse();

                foreach (var order in oRev)
                {

                    DirectoryInfo dInfo = inMbDir.SingleOrDefault(p => p.Name.Equals(order.ModName, StringComparison.OrdinalIgnoreCase));

                    if (dInfo == null)
                        continue;

                    MergeModFiles(dInfo, msInfo);

                }
            }

            foreach (DirectoryInfo dInfo in inMbDir)
            {

                if (modBuildOrder.Any(p => p.ModName.Equals(dInfo.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                MergeModFiles(dInfo, msInfo);

            }


        }

        private static void MergeModFiles(DirectoryInfo sourceInfo, DirectoryInfo destInfo)
        {

            FileInfo[] dfInfo = destInfo.GetFiles();

            foreach (FileInfo sFileInfo in sourceInfo.GetFiles().Where(p => ExtensionsToNotCopy.Any(x => !x.Equals(p.Extension, StringComparison.OrdinalIgnoreCase))))
            {

                var f = dfInfo.SingleOrDefault(p => p.Name == sFileInfo.Name);

                if (f != null && ExtensionsToIgnore.All(p => p != Path.GetExtension(f.FullName)))
                {

                    try
                    {

                        var newJ = JToken.Parse(File.ReadAllText(sFileInfo.FullName).RemoveComments());
                        var orig = JToken.Parse(File.ReadAllText(f.FullName).RemoveComments());

                        var merged = orig.Merge(newJ);

                        File.WriteAllText(Path.Combine(destInfo.FullName, sFileInfo.Name), merged.ToString(Formatting.Indented));

                    }
                    catch (Exception)
                    {
                        sFileInfo.CopyTo(Path.Combine(destInfo.FullName, sFileInfo.Name), true);
                    }

                }
                else
                {
                    sFileInfo.CopyTo(Path.Combine(destInfo.FullName, sFileInfo.Name), true);
                }

            }

            DirectoryInfo[] ddInfo = destInfo.GetDirectories();

            foreach (DirectoryInfo dInfo in sourceInfo.GetDirectories())
            {

                var d = ddInfo.SingleOrDefault(p => p.Name == dInfo.Name);

                if (d != null)
                {
                    MergeModFiles(dInfo, d);
                }
                else
                {

                    Directory.CreateDirectory(Path.Combine(destInfo.FullName, dInfo.Name));

                    MergeModFiles(dInfo, new DirectoryInfo(Path.Combine(destInfo.FullName, dInfo.Name)));

                }

            }

        }

        public static IEnumerable<string> GetMods(string installDir)
        {

            if (!Directory.Exists(installDir))
                throw new DirectoryNotFoundException();

            List<string> mods = new List<string>();

            foreach (DirectoryInfo dInfo in new DirectoryInfo(Path.Combine(installDir, GetModSource(installDir))).GetDirectories())
            {
                mods.Add(dInfo.Name);
            }

            return mods;

        }

        public static string GetModSource(string installDir)
        {

            string path = Path.Combine(installDir, WindowsFolder, BootstrapFile);

            var j = JObject.Parse(File.ReadAllText(path).RemoveComments());

            var token = j["modSource"];

            return token.Value<string>();

        }

        public static IEnumerable<string> GetAssetSources(string installDir)
        {

            string path = Path.Combine(installDir, WindowsFolder, BootstrapFile);

            var j = JObject.Parse(File.ReadAllText(path).RemoveComments());

            var token = j["assetSources"];

            List<string> sources = new List<string>();

            foreach (var t in token)
            {
                sources.Add(t.Value<string>());
            }

            return sources;

        }

        public static string SearchForInstallDir()
        {

            RegistryKey starBound;

            if (Environment.Is64BitOperatingSystem)
                starBound = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 211820");
            else
                starBound = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 211820");

            if (starBound == null)
                return String.Empty;

            return (string)starBound.GetValue("InstallLocation");

        }

    }
}
