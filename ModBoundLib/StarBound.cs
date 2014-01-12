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
using MoreLinq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Archive;
using SharpCompress.Common;

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

        private readonly static string[] ArchiveTypesSupported = 
        {
            ".zip", ".rar", ".7z"
        };

        public const string ModBoundDir = "ModBound";

        public const string ModBoundModDir = "ModBoundMods";

        public const string ModInfoFileExt = ".modinfo";

        public const string ModBoundInfoFile = "modbound" + ModInfoFileExt;

        public const string BackupFormat = "MM-dd-yy-hh-mm-ss";

        public static ModInstallResult InstallMod(string installDir, string modFile, List<ModBuildOrder> modOrder, bool merge = true)
        {

            if (!File.Exists(modFile))
                throw new FileNotFoundException("File not found!", modFile);

            if (!ArchiveTypesSupported.Any(p => p.Equals(Path.GetExtension(modFile))))
                throw new NotSupportedException("File type not supported! (Supported: " + string.Join(", ", ArchiveTypesSupported) + ")");

            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            Directory.CreateDirectory(tempDir);

            using (IArchive archive = ArchiveFactory.Open(modFile))
            {
                archive.WriteToDirectory(tempDir, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
            }

            List<string> modInfoFiles = SearchForModInfos(tempDir).ToList();

            if (!modInfoFiles.Any())
                throw new Exception("Could not find modinfo!");


            string modSource = GetModSource(installDir);

            var requiredDeps = new List<string>();

            foreach (string modInfoFile in modInfoFiles)
            {

                string modInfoDir = Path.GetDirectoryName(modInfoFile);


                if (string.IsNullOrEmpty(modInfoDir))
                    throw new Exception("An unexpected error has occurred!");

                DirectoryInfo midInfo = new DirectoryInfo(modInfoDir);


                var cInstMods = GetInstalledMods(installDir).ToList();

                ModInfo mInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(modInfoFile));

                if (mInfo.Dependencies != null && mInfo.Dependencies.Length > 0)
                {

                    foreach (string dep in mInfo.Dependencies)
                    {

                        if (!cInstMods.Any(p => p.Name.Equals(dep, StringComparison.OrdinalIgnoreCase)))
                        {
                            requiredDeps.Add(dep);
                        }

                    }

                    if (requiredDeps.Any())
                        break;

                }

                bool any = false;

                if (merge)
                {
                    foreach (ModInfo instMod in cInstMods.Where(p => !p.Name.Equals(midInfo.Name)))
                    {
                        if (IOHelper.AnyFilesOverlapInDirectories(modInfoDir, instMod.ModPath))
                        {

                            any = true;

                            DirectoryInfo dInfo0 = new DirectoryInfo(instMod.ModPath);

                            DirectoryInfo dInfo = new DirectoryInfo(Path.Combine(installDir, ModBoundDir, dInfo0.Name));

                            if (dInfo.Exists && dInfo.FullName == dInfo0.FullName)
                                continue;

                            Directory.Move(instMod.ModPath, dInfo.FullName);

                        }
                    }
                }

                if (any)
                {

                    string path = Path.Combine(installDir, ModBoundDir, midInfo.Name);

                    if (Directory.Exists(path))
                        IOHelper.DeleteDirectory(path, false);

                    IOHelper.CopyDirectory(modInfoDir, path);

                    if (modOrder != null)
                        RebuildMods(installDir, modOrder);

                }
                else
                {

                    string path = Path.Combine(installDir, WindowsFolder, modSource, midInfo.Name);

                    if (Directory.Exists(path))
                        IOHelper.DeleteDirectory(path, false);


                    IOHelper.CopyDirectory(modInfoDir, path);

                }

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

            }

            IOHelper.DeleteDirectory(tempDir);

            return new ModInstallResult { Result = requiredDeps.Count == 0, DependenciesNeeded = requiredDeps };

        }

        public static void BackupMod(string installDir, string modDirName, string backupTo)
        {

            string dir = Path.Combine(installDir, WindowsFolder, GetModSource(installDir), modDirName);

            ZipFile.CreateFromDirectory(dir, Path.Combine(backupTo, String.Format("{0}-{1}.zip", modDirName, DateTime.Now.ToString(BackupFormat))));

        }

        public static IEnumerable<ModInfo> GetInstalledMods(string installDir)
        {

            List<ModInfo> modInfos = new List<ModInfo>();

            string mbPath = Path.Combine(installDir, ModBoundDir);
            string path = Path.Combine(installDir, WindowsFolder, GetModSource(installDir));

            if (!Directory.Exists(path))
                return modInfos;

            foreach (DirectoryInfo dInfo in new DirectoryInfo(path).GetDirectories().Where(p => !p.Name.Equals(ModBoundModDir)))
            {

                FileInfo[] fileInfos = dInfo.GetFiles();

                FileInfo fInfo = fileInfos.FirstOrDefault(p => p.Extension == ModInfoFileExt);

                if (fInfo == null)
                    continue;

                ModInfo mInfo;

                try
                {
                    mInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(fInfo.FullName));
                }
                catch (Exception)
                {
                    continue;
                }

                if (mInfo != null)
                {

                    mInfo.ModPath = dInfo.FullName;

                    modInfos.Add(mInfo);

                }

            }

            if (Directory.Exists(mbPath))
            {
                foreach (DirectoryInfo dInfo in new DirectoryInfo(mbPath).GetDirectories())
                {

                    FileInfo[] fileInfos = dInfo.GetFiles();

                    FileInfo fInfo = fileInfos.FirstOrDefault(p => p.Extension == ModInfoFileExt);

                    if (fInfo == null)
                        continue;

                    ModInfo mInfo;

                    try
                    {
                        mInfo = JsonConvert.DeserializeObject<ModInfo>(File.ReadAllText(fInfo.FullName));
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (mInfo != null)
                    {

                        mInfo.ModPath = dInfo.FullName;

                        modInfos.Add(mInfo);

                    }

                }
            }

            return modInfos.DistinctBy(p => p.Name.ToLower());

        }

        public static IEnumerable<string> SearchForModInfos(string baseDir)
        {

            if (!Directory.Exists(baseDir))
                throw new DirectoryNotFoundException();

            List<string> modInfos = new List<string>();

            DirectoryInfo baseDirInfo = new DirectoryInfo(baseDir);

            FileInfo[] files = baseDirInfo.GetFiles();

            FileInfo modInfoFileInfo = files.FirstOrDefault(p => p.Extension.Equals(ModInfoFileExt, StringComparison.OrdinalIgnoreCase));

            if (modInfoFileInfo != null)
            {
                modInfos.Add(modInfoFileInfo.FullName);
            }

            DirectoryInfo[] dInfos = baseDirInfo.GetDirectories();

            foreach (DirectoryInfo dInfo in dInfos)
            {
                modInfos.AddRange(SearchForModInfos(dInfo.FullName));
            }

            return modInfos;

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

            string modModBoundDir = Path.Combine(installDir, WindowsFolder, modSource, ModBoundModDir);

            if (Directory.Exists(modModBoundDir))
                IOHelper.DeleteDirectory(modModBoundDir, false);

            Directory.CreateDirectory(modModBoundDir);

            string mbInfo = JsonConvert.SerializeObject(new ModInfo
            {
                Name = "ModBoundMods",
                Path = ".",
                Dependencies = new string[0],
                Version = Settings.Default.StarBoundVersion
            });

            File.WriteAllText(Path.Combine(modModBoundDir, ModBoundInfoFile), mbInfo);

            DirectoryInfo msInfo = new DirectoryInfo(modModBoundDir);
            DirectoryInfo pathInfo = new DirectoryInfo(path);


            DirectoryInfo[] inMbDir = pathInfo.GetDirectories();

            List<ModInfo> instMods = GetInstalledMods(installDir).ToList();

            if (modBuildOrder.Count > 0)
            {

                var oRev = modBuildOrder.OrderBy(p => p.BuildOrder);

                foreach (var order in oRev)
                {

                    ModInfo installedMod = instMods.SingleOrDefault(p => p.Name.Equals(order.ModName, StringComparison.OrdinalIgnoreCase));

                    if (installedMod == null)
                        continue;

                    DirectoryInfo mDirInfo = new DirectoryInfo(installedMod.ModPath);

                    if (!mDirInfo.Exists)
                        continue;

                    DirectoryInfo dInfo = inMbDir.SingleOrDefault(p => p.Name.Equals(mDirInfo.Name, StringComparison.OrdinalIgnoreCase));

                    if (dInfo == null)
                        continue;

                    MergeModFiles(dInfo, msInfo);

                }
            }

            foreach (DirectoryInfo dInfo in inMbDir)
            {

                ModInfo installedMod = instMods.SingleOrDefault(p => p.Name.Equals(dInfo.Name, StringComparison.OrdinalIgnoreCase));

                if (installedMod == null)
                    continue;

                DirectoryInfo mDirInfo = new DirectoryInfo(installedMod.ModPath);

                if (!mDirInfo.Exists)
                    continue;

                if (modBuildOrder.Any(p => p.ModName.Equals(installedMod.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                MergeModFiles(dInfo, msInfo);

            }


        }

        public static bool HasConflictingMods(string installDir)
        {

            string modBoundDir = Path.Combine(installDir, ModBoundDir);
            string modBoundModDir = Path.Combine(installDir, WindowsFolder, GetModSource(installDir), ModBoundModDir);

            DirectoryInfo dInfo = new DirectoryInfo(modBoundDir);
            DirectoryInfo dinfo2 = new DirectoryInfo(modBoundModDir);

            return (dInfo.Exists && dInfo.GetDirectories().Any()) || (dinfo2.Exists && (dinfo2.GetDirectories().Any() || dinfo2.GetFiles().Any()));

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

                        var merged = newJ.Merge(orig, new MergeOptions { ArrayHandling = MergeOptionArrayHandling.Concat });

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
