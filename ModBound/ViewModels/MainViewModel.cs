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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using Caliburn.Micro.Contrib.Results;
using GongSolutions.Wpf.DragDrop;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ModBound.Misc;
using ModBound.Models;
using ModBound.Properties;
using ModBoundLib;
using ModBoundLib.Extensions;
using ModBoundLib.Helpers;
using Newtonsoft.Json;

namespace ModBound.ViewModels
{
    [Export]
    public class MainViewModel : MBViewModel, IDropTarget
    {

        private string _status;
        private string _sbInstallFolder;
        private string _searchModText;

        private bool _isModInfoOpen;

        private bool _isRefreshingMyMods;
        private bool _isRefreshingMods;
        private bool _isRefreshingSelectedMod;
        private bool _isRefreshingInstalledMods;

        private bool _modSortDesc;

        private bool _isSignedIn;

        private Visibility _registerButtonVisible;

        private string _loginButtonText;

        private UserMod _mySelectedMod;

        private UserMod _selectedMod;

        private readonly ModBoundApi _api;

        private int _pageCtr;

        private readonly static string TempDir = Path.Combine(Path.GetTempPath(), "ModBound");
        private readonly static string TempFileDir = Path.Combine(TempDir, "TempFiles");

        private readonly IWindowManager _windowManager;

        public MainViewModel()
            : this(null)
        {
        }

        [ImportingConstructor]
        public MainViewModel(IWindowManager windowManager)
        {

            string updater = Path.Combine(AssemblyHelper.GetCurrentExecutingDirectory(), "updater.exe");

            if (File.Exists(updater))
            {

                ProcessStartInfo psi = new ProcessStartInfo(updater, "/justcheck");

                Process updaterProc = new Process();
                updaterProc.StartInfo = psi;
                updaterProc.EnableRaisingEvents = true;

                updaterProc.Exited += (sender, e) =>
                {

                    if (updaterProc.ExitCode == 0)
                    {
                        Process.Start(updater, "/checknow");
                    }

                };

                updaterProc.Start();

            }

            BrowserProtocol.RegisterModBound();

            _windowManager = windowManager;

            if (Settings.Default.UpdateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                Settings.Default.Save();
            }

            if (string.IsNullOrEmpty(Settings.Default.SBInstallFolder) || !Directory.Exists(Settings.Default.SBInstallFolder))
            {

                Settings.Default.SBInstallFolder = StarBound.SearchForInstallDir();
                Settings.Default.Save();

                SbInstallFolder = Settings.Default.SBInstallFolder;

            }
            else
            {
                SbInstallFolder = Settings.Default.SBInstallFolder;
            }

            _loginButtonText = "Login";

            _api = ModBoundApi.Default;
            _api.SetLoginDetails(Settings.Default.Username, Settings.Default.Password.DecryptString(Encoding.Unicode.GetBytes(Settings.Default.Entropy)).ToInsecureString());

            _modSortDesc = false;

            _isSignedIn = false;
            _isRefreshingMyMods = false;
            _isRefreshingSelectedMod = false;
            _isModInfoOpen = false;
            _isRefreshingInstalledMods = false;

            RegisterButtonVisible = Visibility.Visible;

            MyAccountInfo = new AccountInfo
            {
                Username = "Anonymous",
                Email = "unknown@modbound.com"
            };

            InstalledMods = new ObservableCollection<InstalledMod>();
            AvailableMods = new ObservableCollection<UserMod>();
            MyMods = new ObservableCollection<UserMod>();
            MyModVersions = new ObservableCollection<ModVersion>();

        }

        protected override void OnDeactivate(bool close)
        {

            base.OnDeactivate(close);

            _api.Dispose();

        }

        protected override async void OnViewLoaded(object view)
        {

            base.OnViewLoaded(view);

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length == 2)
            {

                int modId = int.Parse(args[1].Replace("modbound://", String.Empty).Replace("modbound:", String.Empty).Replace("/", String.Empty));

                await DownloadInstallModById(modId);

            }

            RefreshMods();

            try
            {

                bool authResult = await _api.SignInAsync();

                if (authResult)
                {

                    IsSignedIn = true;

                    RegisterButtonVisible = Visibility.Collapsed;

                    LoginButtonText = "Logout";

                    await RefreshInstalledMods();

                    await DownloadUnsyncedMods();

                    RefreshMyMods();

                    MyAccountInfo = await _api.GetAccountInfo();

                }
                else
                {
                    await RefreshInstalledMods();
                }

            }
            catch (Exception)
            {
            }

            await RefreshModOrder();

        }

        private async Task DownloadUnsyncedMods()
        {

            if (string.IsNullOrEmpty(Settings.Default.SBInstallFolder) || !Directory.Exists(Settings.Default.SBInstallFolder))
            {
                return;
            }

            var resp = await _api.GetSyncedMods();

            foreach (SyncedMod syncedMod in resp.SyncedMods)
            {

                InstalledMod iMod = InstalledMods.SingleOrDefault(p => p.ID == syncedMod.ModId);

                if (iMod == null)
                {
                    await DownloadInstallModById(syncedMod.ModId);
                }

            }

        }

        private async Task RefreshMods()
        {

            IsRefreshingMods = true;

            AvailableMods.Clear();

            List<SBMod> mods = await _api.GetMods(_pageCtr, SearchModText, _modSortDesc);

            foreach (SBMod mod in mods)
            {
                if (AvailableMods.All(p => p.ID != mod.ID))
                    AvailableMods.Add(SBModToUserMod(mod));
            }

            IsRefreshingMods = false;

        }

        private async Task RefreshMyMods()
        {

            if (!IsSignedIn)
                return;

            IsRefreshingMyMods = true;

            List<SBMod> myMods = await _api.GetMyMods();

            MyModVersions.Clear();
            MyMods.Clear();

            if (myMods != null)
            {
                foreach (SBMod mod in myMods)
                {
                    MyMods.Add(SBModToUserMod(mod));
                }
            }

            IsRefreshingMyMods = false;

        }

        private UserMod SBModToUserMod(SBMod mod)
        {

            SBModVersion firstModVersion = mod.Versions.OrderByDescending(p => p.DateAdded).FirstOrDefault();

            UserMod uMod = new UserMod();
            uMod.ID = mod.ID;
            uMod.Name = mod.Name;
            uMod.Author = mod.Author;
            uMod.Description = mod.Description;
            uMod.Downloads = mod.Downloads;

            if (firstModVersion != null)
            {
                uMod.LastUpdated = firstModVersion.DateAdded.ToLocalTime().ToString("g");
                uMod.Version = firstModVersion.Version;
            }

            uMod.Versions = new ObservableCollection<ModVersion>();

            foreach (var ver in mod.Versions)
            {

                ModVersion mv = new ModVersion();
                mv.Changes = ver.Changes;
                mv.Version = ver.Version;
                mv.DateAdded = ver.DateAdded;
                mv.File = new ModVersionFile();
                mv.ScreenShots = new ObservableCollection<ModVersionFile>();

                SBModFile file = ver.Files.FirstOrDefault(p => !p.IsScreenShot);

                if (file != null)
                {
                    mv.File.IsLocked = true;
                    mv.File.FileName = file.FileName;
                }

                foreach (var screenshot in ver.Files.Where(p => p.IsScreenShot))
                {
                    mv.ScreenShots.Add(new ModVersionFile { FileName = screenshot.FileName, IsLocked = true });
                }

                if (mv.ScreenShots.Count < 2)
                    for (int i = 0; i <= 2 - mv.ScreenShots.Count; i++)
                        mv.ScreenShots.Add(new ModVersionFile());

                uMod.Versions.Add(mv);

            }

            return uMod;

        }

        public void PlayStarbound()
        {

            string dir = Settings.Default.SBInstallFolder;

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {

                var metroWindow = (MetroWindow)Application.Current.MainWindow;

                metroWindow.ShowMessageAsync("Error", "Could not find StarBound!");

                return;

            }

            ProcessStartInfo psi = new ProcessStartInfo(Path.Combine(dir, StarBound.WindowsFolder, "starbound.exe"));

            Process proc = new Process();
            proc.StartInfo = psi;

            proc.Start();

            Application.Current.Shutdown();

        }

        public void OpenSettingsDialog()
        {

            bool? result = _windowManager.ShowDialog(new SettingsViewModel());

            if (result.HasValue && result.Value)
            {
                SbInstallFolder = Settings.Default.SBInstallFolder;
            }

        }

        public async Task<bool> OpenRegisterDialog()
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            bool? result = _windowManager.ShowDialog(new RegisterViewModel());

            if (result.HasValue && result.Value)
            {

                IsSignedIn = true;

                RegisterButtonVisible = Visibility.Collapsed;

                LoginButtonText = "Logout";

                await metroWindow.ShowMessageAsync("Success!", "Your account has been created. You have been sent a confirmation email!");

                MyAccountInfo = await _api.GetAccountInfo();

                await RefreshInstalledMods();
                await RefreshMyMods();

                return true;

            }

            MyAccountInfo = new AccountInfo
            {
                Username = "Anonymous",
                Email = "unknown@modbound.com"
            };

            RegisterButtonVisible = Visibility.Visible;

            MyModVersions.Clear();
            MyMods.Clear();

            LoginButtonText = "Login";

            await RefreshInstalledMods();

            return false;

        }

        public async Task<bool> OpenLoginDialog()
        {

            LoginButtonText = "Login";

            _api.SetLoginDetails(String.Empty, String.Empty);

            Settings.Default.Password = String.Empty;
            Settings.Default.Save();

            bool? result = _windowManager.ShowDialog(new LoginViewModel());

            if (result.HasValue && result.Value)
            {

                IsSignedIn = true;

                RegisterButtonVisible = Visibility.Collapsed;

                LoginButtonText = "Logout";

                MyAccountInfo = await _api.GetAccountInfo();

                await RefreshInstalledMods();
                await RefreshMyMods();

                return true;

            }

            MyAccountInfo = new AccountInfo
            {
                Username = "Anonymous",
                Email = "unknown@modbound.com"
            };

            IsSignedIn = false;

            RegisterButtonVisible = Visibility.Visible;

            MyModVersions.Clear();
            MyMods.Clear();

            LoginButtonText = "Login";

            await RefreshInstalledMods();

            return false;

        }

        public async void ImportMod()
        {

            var openResult = new OpenFileResult("Select a file containing the mod to import")
                .In(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                .FilterFiles(filter => filter.AddFilter("zip").WithDefaultDescription().AddFilter("rar").WithDefaultDescription())
                .WithFileDo(InstallMod);

            try
            {
                await openResult.ExecuteAsync();
            }
            catch (Exception)
            {
            }

        }

        public void AddMod()
        {

            UserMod uMod = new UserMod { Name = "My mod" };

            MyMods.Add(uMod);

            MySelectedMod = uMod;

        }

        public void AddModVersion()
        {

            if (MySelectedMod == null)
                return;

            if (MyModVersions == null)
                MyModVersions = new ObservableCollection<ModVersion>();

            int ctr = MyModVersions.Count(p => p.Version.Contains("New Version"));

            ModVersion newVersion = new ModVersion { Version = "New Version " + (++ctr), ScreenShots = new ObservableCollection<ModVersionFile>(), DateAdded = DateTime.Now };

            for (int i = 0; i < 2; i++)
                newVersion.ScreenShots.Add(new ModVersionFile());

            MyModVersions.Add(newVersion);

            MySelectedModVersion = newVersion;

        }

        public async void DeleteMod()
        {

            if (MySelectedMod == null)
                return;

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            int id = MySelectedMod.ID;

            if (id != 0)
            {

                var prog = await metroWindow.ShowProgressAsync("Working....", "Deleting Mod");
                prog.SetIndeterminate();

                await _api.DeleteMod(MySelectedMod.ID);

                await prog.CloseAsync();

                await RefreshMods();

            }

            MyMods.Remove(MySelectedMod);

            if (MyMods.Count > 0)
                MySelectedMod = MyMods.Last();

        }

        public async void DeleteModVersion()
        {

            if (MySelectedMod == null || MySelectedModVersion == null)
                return;

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            var prog = await metroWindow.ShowProgressAsync("Working....", "Deleting Version");
            prog.SetIndeterminate();


            await _api.DeleteModVersion(MySelectedMod.ID, MySelectedModVersion.Version);

            MyModVersions.Remove(MySelectedModVersion);

            if (MyModVersions.Count > 0)
                MySelectedModVersion = MyModVersions.Last();

            await prog.CloseAsync();

        }

        public async void DeleteScreenshot(ModVersionFile file)
        {

            if (MySelectedMod == null)
                return;

            if (MySelectedModVersion == null)
                return;

            string temp = file.FileName;

            file.FileName = String.Empty;
            file.IsLocked = false;

            if (MySelectedMod.ID == 0)
                return;

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            var prog = await metroWindow.ShowProgressAsync("Working....", "Deleting File");
            prog.SetIndeterminate();

            await _api.DeleteModFile(MySelectedMod.ID, MySelectedModVersion.Version, temp);

            await prog.CloseAsync();

        }

        public async void DeleteModFile()
        {

            if (MySelectedMod == null)
                return;

            if (MySelectedModVersion == null)
                return;

            if (MySelectedModVersion.File == null)
                return;

            string temp = MySelectedModVersion.File.FileName;

            MySelectedModVersion.File.FileName = String.Empty;
            MySelectedModVersion.File.IsLocked = false;

            if (MySelectedMod.ID == 0)
                return;

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            var prog = await metroWindow.ShowProgressAsync("Working....", "Deleting File");
            prog.SetIndeterminate();

            await _api.DeleteModFile(MySelectedMod.ID, MySelectedModVersion.Version, temp);

            await prog.CloseAsync();

        }

        public async void BrowseForMod(ModVersionFile file)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            if (file == null)
            {

                await metroWindow.ShowMessageAsync("Error", "You must first add a version to the mod!");

                return;

            }

            var openResult = new OpenFileResult("Select a file to add to this mod version")
                .In(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                .WithFileDo(x =>
                {
                    if (File.Exists(x))
                        file.FileName = x;
                });

            try
            {
                await openResult.ExecuteAsync();
            }
            catch (Exception)
            {
            }

        }

        public async void BackupMod(InstalledMod mod)
        {

            var browseResult = new BrowseFolderResult("Select a folder to backup to")
                .AllowNewFolder()
                .WithSelectedPathDo(x => StarBound.BackupMod(Settings.Default.SBInstallFolder, new DirectoryInfo(mod.Path).Name, x));

            try
            {
                await browseResult.ExecuteAsync();
            }
            catch (Exception)
            {
            }

        }

        public async void InstallMod(string zipFile)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            if (string.IsNullOrEmpty(Settings.Default.SBInstallFolder))
            {

                await metroWindow.ShowMessageAsync("Error", "Could not automatically find Starbound's installation folder! Please add the location in the settings window.");

                return;

            }

            var prog = await metroWindow.ShowProgressAsync("Working....", "Installing Mod");
            prog.SetIndeterminate();

            ModInstallResult result;

            Dispatcher dispatch = Dispatcher.CurrentDispatcher;

            string modOrderJson = Settings.Default.ModOrder;

            Exception exception = await Task.Run(async () =>
            {

                try
                {

                    List<ModBuildOrder> modOrder = new List<ModBuildOrder>();

                    if (!string.IsNullOrEmpty(modOrderJson))
                        modOrder = await JsonConvert.DeserializeObjectAsync<List<ModBuildOrder>>(modOrderJson);

                    result = StarBound.InstallMod(Settings.Default.SBInstallFolder, zipFile, modOrder, Settings.Default.MergeMods);

                    var depsNeeded = result.DependenciesNeeded.ToList();

                    dispatch.Invoke(async () => await prog.CloseAsync());

                    if (!result.Result && depsNeeded.Count > 0)
                    {

                        if (await dispatch.Invoke(async () => await InstallRequiredDependencies(depsNeeded)))
                        {
                            result = StarBound.InstallMod(Settings.Default.SBInstallFolder, zipFile, modOrder, Settings.Default.MergeMods);
                        }

                    }

                }
                catch (Exception ex)
                {
                    return ex;
                }

                return null;

            });

            await RefreshInstalledMods();
            await RefreshModOrder(Settings.Default.MergeMods);

            if (exception != null)
            {
                await metroWindow.ShowMessageAsync("Error", exception.Message);
            }

        }

        public async Task RefreshModOrder(bool forceRebuild = false)
        {


            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            Exception exception = null;

            try
            {

                string modOrderJson = Settings.Default.ModOrder;

                List<ModBuildOrder> modOrder = new List<ModBuildOrder>();

                if (!string.IsNullOrEmpty(modOrderJson))
                    modOrder = await JsonConvert.DeserializeObjectAsync<List<ModBuildOrder>>(modOrderJson);

                modOrder = modOrder.OrderBy(p => p.BuildOrder).ToList();

                foreach (var mod in InstalledMods)
                {

                    ModBuildOrder mbo = modOrder.FirstOrDefault(p => p.ModName.Equals(mod.Name, StringComparison.OrdinalIgnoreCase));

                    if (mbo != null)
                    {
                        mod.ModOrder = mbo.BuildOrder;
                    }
                    else
                    {

                        int x = 0;

                        if (modOrder.Any())
                            x = modOrder.Max(p => p.BuildOrder) + 1;

                        mod.ModOrder = x;

                        modOrder.Add(new ModBuildOrder { ModName = mod.Name, BuildOrder = x });

                    }

                }

                InstalledMods = new ObservableCollection<InstalledMod>(InstalledMods.OrderBy(p => p.ModOrder));

                Settings.Default.ModOrder = await JsonConvert.SerializeObjectAsync(modOrder);
                Settings.Default.Save();

                await Task.Run(() =>
                {
                    if (forceRebuild)
                    {
                        StarBound.RebuildMods(Settings.Default.SBInstallFolder, modOrder);
                    }
                });


            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
                await metroWindow.ShowMessageAsync("Error", exception.Message);

        }

        public async Task RefreshInstalledMods()
        {

            if (string.IsNullOrEmpty(Settings.Default.SBInstallFolder) || !Directory.Exists(Settings.Default.SBInstallFolder))
                return;

            List<SyncedMod> syncedMods = new List<SyncedMod>();

            if (IsSignedIn)
            {
                syncedMods = (await _api.GetSyncedMods()).SyncedMods;
            }

            IsRefreshingInstalledMods = true;

            InstalledMods.Clear();

            foreach (var modInfo in StarBound.GetInstalledMods(Settings.Default.SBInstallFolder))
            {

                List<SBMod> mods = await _api.GetMods(0, modInfo.Name, false);
                mods = mods.Where(p => p.Name.Equals(modInfo.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                InstalledMod mod = new InstalledMod();
                mod.Path = modInfo.ModPath;

                if (modInfo.Dependencies != null)
                {
                    mod.Dependencies = new ObservableCollection<string>(modInfo.Dependencies);
                }

                if (modInfo.ExtraData != null)
                {
                    mod.Version = modInfo.ExtraData.ModVersion;
                    mod.Description = modInfo.ExtraData.ModDescription;
                }
                else
                {
                    mod.Version = "Unknown";
                }

                if (mods.Count > 0)
                {

                    SBMod firstMod = mods.First();

                    mod.ID = firstMod.ID;
                    mod.Name = firstMod.Name;
                    mod.Description = firstMod.Description;
                    mod.Author = firstMod.Author;
                    mod.IsSynced = syncedMods.Any(p => p.ModId == mod.ID);

                    SBModVersion latest = firstMod.Versions.OrderByDescending(p => p.DateAdded).FirstOrDefault();

                    if (latest != null && mod.Version != null)
                    {
                        mod.Outdated = !(mod.Version.Equals(latest.Version, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (latest != null)
                    {
                        mod.Outdated = true;
                    }

                }
                else
                {
                    mod.Name = modInfo.Name;
                    mod.Author = "Unknown";
                }

                if (!InstalledMods.Any(p => p.Name.Equals(mod.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    InstalledMods.Add(mod);
                }

            }

            foreach (var syncedMod in syncedMods)
            {

                if (InstalledMods.Any(p => p.ID == syncedMod.ModId && !p.IsSynced))
                {
                    await DownloadInstallModById(syncedMod.ModId);
                }

            }

            AmountOfModsInstalled = InstalledMods.Count;

            IsRefreshingInstalledMods = false;

        }

        public async void UpdateMod(InstalledMod mod)
        {

            if (mod.ID == 0)
            {

                var metroWindow = (MetroWindow)Application.Current.MainWindow;

                await metroWindow.ShowMessageAsync("Error", "An error has occurred!");

                return;

            }

            SBMod sbMod = await _api.GetMod(mod.ID);

            await DownloadInstallMod(SBModToUserMod(sbMod));


        }

        public async void RemoveMod(InstalledMod mod)
        {

            bool isDep = false;

            foreach (InstalledMod iMod in InstalledMods)
            {

                if (iMod.Dependencies != null && iMod.Dependencies.Any(p => p.Equals(mod.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    isDep = true;
                }

            }

            if (isDep)
            {

                var metroWindow = (MetroWindow)Application.Current.MainWindow;

                var result = await metroWindow.ShowMessageAsync("Confirm", "This mod is a dependency for other mods! Removing it might break functionality. Are you sure you want to remove it?",
                    MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Negative)
                    return;

            }

            string modOrderJson = Settings.Default.ModOrder;

            if (!string.IsNullOrEmpty(modOrderJson))
            {

                List<ModBuildOrder> modOrder = await JsonConvert.DeserializeObjectAsync<List<ModBuildOrder>>(modOrderJson);

                ModBuildOrder mbo = modOrder.SingleOrDefault(p => p.ModName.Equals(mod.Name, StringComparison.OrdinalIgnoreCase));

                if (mbo != null)
                {

                    modOrder.Remove(mbo);

                    Settings.Default.ModOrder = await JsonConvert.SerializeObjectAsync(modOrder);
                    Settings.Default.Save();

                }

            }

            if (Directory.Exists(mod.Path))
            {
                IOHelper.DeleteDirectory(mod.Path);
            }

            await RefreshInstalledMods();

        }

        public async Task DownloadInstallModById(int modId)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            SBMod mod = await _api.GetMod(modId);

            if (mod != null)
            {

                bool result = await DownloadInstallMod(SBModToUserMod(mod));

                //if (result)
                //    await metroWindow.ShowMessageAsync("Success", "Mod installed!");

            }
            else
            {
                await metroWindow.ShowMessageAsync("Error", String.Format("Mod not found! ({0})", modId));
            }

        }

        public async Task<bool> DownloadInstallMod(UserMod mod)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            if (string.IsNullOrEmpty(Settings.Default.SBInstallFolder) || !Directory.Exists(Settings.Default.SBInstallFolder))
            {

                await metroWindow.ShowMessageAsync("Error", "Could not find StarBound's installation folder!");

                return false;

            }

            ModVersion latest = mod.Versions.OrderByDescending(p => p.DateAdded).FirstOrDefault();



            if (latest == null)
            {

                await metroWindow.ShowMessageAsync("Error", "An error has occurred!");

                return false;

            }

            if (string.IsNullOrEmpty(latest.File.FileName))
            {

                await metroWindow.ShowMessageAsync("Error", "This mod does not have a file associated with it!");

                return false;

            }

            var prog = await metroWindow.ShowProgressAsync("Working....", "Downloading & Installing Mod");
            prog.SetIndeterminate();

            string fileName = Path.Combine(TempDir, TempFileDir, latest.File.FileName);

            Dispatcher dispatch = Dispatcher.CurrentDispatcher;

            string modOrderJson = Settings.Default.ModOrder;

            bool result = await Task.Run(async () =>
            {

                try
                {

                    List<ModBuildOrder> modOrder = new List<ModBuildOrder>();

                    if (!string.IsNullOrEmpty(modOrderJson))
                        modOrder = await JsonConvert.DeserializeObjectAsync<List<ModBuildOrder>>(modOrderJson);

                    await _api.DownloadModFile(mod.ID, latest.Version, latest.File.FileName, fileName);

                    ModInstallResult res = StarBound.InstallMod(Settings.Default.SBInstallFolder, fileName, modOrder, Settings.Default.MergeMods);

                    var depsNeeded = res.DependenciesNeeded.ToList();

                    dispatch.Invoke(async () => await prog.CloseAsync());

                    if (!res.Result && depsNeeded.Count > 0)
                    {

                        if (await dispatch.Invoke(async () => await InstallRequiredDependencies(depsNeeded)))
                        {
                            res = StarBound.InstallMod(Settings.Default.SBInstallFolder, fileName, modOrder, Settings.Default.MergeMods);
                        }

                    }

                    return res.Result;

                }
                catch (Exception)
                {
                    return false;
                }

            });

            File.Delete(fileName);

            if (!result)
            {
                await metroWindow.ShowMessageAsync("Error", "An error has occurrred!");
            }
            else
            {
                await RefreshInstalledMods();
                await RefreshModOrder(Settings.Default.MergeMods);
            }

            return result;

        }

        public async void SearchForMod()
        {
            await RefreshMods();
        }

        public async void NextPage()
        {

            _pageCtr++;

            await RefreshMods();

        }

        public async void PreviousPage()
        {
            if (_pageCtr > 0)
            {

                _pageCtr--;

                await RefreshMods();

            }
        }

        public async void OpenModInfo(UserMod mod)
        {

            SelectedMod = mod;

            if (mod.ID == 0)
                return;

            IsRefreshingSelectedMod = true;

            IsModInfoOpen = true;

            SBMod sbMod = await _api.GetMod(mod.ID);

            UserMod uMod = SBModToUserMod(sbMod);

            mod.ID = uMod.ID;
            mod.Name = uMod.Name;
            mod.Description = uMod.Description;
            mod.Author = uMod.Author;
            mod.ModTypes = uMod.ModTypes;
            mod.Version = uMod.Version;

            mod.Versions = new ObservableCollection<ModVersion>(uMod.Versions);

            var latestVer = mod.Versions.OrderByDescending(p => p.DateAdded).FirstOrDefault();

            if (latestVer != null)
            {

                string tmpFileDir = Path.Combine(TempDir, TempFileDir);

                if (!Directory.Exists(tmpFileDir))
                    Directory.CreateDirectory(tmpFileDir);

                foreach (var screenshot in latestVer.ScreenShots.Where(p => !string.IsNullOrEmpty(p.FileName)))
                {

                    string sfName = screenshot.FileName.Replace("\"", String.Empty);

                    string fName = Path.Combine(TempDir, TempFileDir, String.Format("{0}_{1}_{2}", mod.ID, latestVer.Version, sfName));

                    if (!File.Exists(fName))
                        await _api.DownloadModFile(mod.ID, latestVer.Version, screenshot.FileName, fName);

                    BitmapImage src = new BitmapImage();
                    src.BeginInit();
                    src.UriSource = new Uri(fName);
                    src.EndInit();

                    screenshot.Image = src;

                }

            }

            SelectedModVersion = latestVer;

            IsRefreshingSelectedMod = false;

        }

        public async void SaveMod()
        {

            if (MySelectedMod == null)
                return;

            if (MySelectedModVersion == null)
                return;

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            var prog = await metroWindow.ShowProgressAsync("Working....", "Saving Mod");
            prog.SetIndeterminate();

            ModVersion latest = MyModVersions.OrderByDescending(p => p.DateAdded).First();

            ModTypes mTypes = 0; //TODO

            var response = await _api.PostMod(MySelectedMod.ID, MySelectedMod.Name, MySelectedMod.Description, latest.Version, latest.Changes, mTypes);

            int modId = response.ModId;

            if (modId == 0)
            {

                await prog.CloseAsync();

                string errors = String.Empty;

                if (response.Errors != null)
                    errors = string.Join("\n", response.Errors);

                await metroWindow.ShowMessageAsync("Error", "An error has occurred!\n" + errors);

                return;

            }

            List<ApiResponse> responses = new List<ApiResponse>();

            foreach (ModVersion ver in MyModVersions)
            {

                if (!string.IsNullOrEmpty(ver.File.FileName) && !ver.File.IsLocked)
                {

                    if (new FileInfo(ver.File.FileName).Length > 15728640)
                    {
                        responses.Add(new ApiResponse { ResponseCode = ResponseCodes.Error, Errors = new[] { "The mod file is too large! (greater than 15mb)" } });
                    }
                    else
                    {
                        responses.Add(await _api.PostModFiles(modId, ver.Version, new[] { ver.File.FileName }));
                    }

                }

                if (ver.ScreenShots.Count(p => !string.IsNullOrEmpty(p.FileName) && p.FileName.Length > 0) > 0)
                {
                    responses.Add(await _api.PostModFiles(modId, ver.Version, ver.ScreenShots.Where(p => !p.IsLocked).Select(p => p.FileName).ToArray(), true));
                }

            }

            await prog.CloseAsync();

            if (responses.Any(p => p.ResponseCode == ResponseCodes.Error))
            {

                var errors = new List<string>();

                foreach (ApiResponse apiRes in responses)
                {
                    if (apiRes.ResponseCode == ResponseCodes.Error && apiRes.Errors != null)
                        errors.Add(string.Join("ERROR: \n - ", apiRes.Errors));
                }

                await metroWindow.ShowMessageAsync("Error", String.Format("An error has occurred!\n{0}", string.Join("\n", errors)));

            }


            await RefreshMyMods();
            await RefreshMods();

            MySelectedMod = MyMods.SingleOrDefault(p => p.ID == modId);

        }

        public async void SyncMod(InstalledMod mod)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            if (mod.ID != 0)
            {

                if (!mod.IsSynced)
                {

                    var prog = await metroWindow.ShowProgressAsync("Working....", "Syncing mod!");

                    await _api.AddSyncedMod(mod.ID);

                    await RefreshInstalledMods();

                    await prog.CloseAsync();

                }
                else
                {

                    var prog = await metroWindow.ShowProgressAsync("Working....", "Unsyncing mod!");

                    await _api.RemoveSyncedMod(mod.ID);

                    await RefreshInstalledMods();

                    await prog.CloseAsync();

                }

            }
            else
            {
                await metroWindow.ShowMessageAsync("Error", "Cannot sync this mod!");
            }

        }

        private async Task<bool> InstallRequiredDependencies(IEnumerable<string> depsNeeded)
        {

            var metroWindow = (MetroWindow)Application.Current.MainWindow;

            StringBuilder sb = new StringBuilder("The following dependencies are required:" + Environment.NewLine + Environment.NewLine);

            var modsNeeded = new List<SBMod>();

            bool canInstallAll = true;

            foreach (string dep in depsNeeded)
            {

                List<SBMod> modsFound = await _api.GetMods(0, dep, false);

                if (modsFound.Count > 0)
                {

                    modsNeeded.Add(modsFound.First());

                    sb.AppendLine(dep);

                }
                else
                {

                    canInstallAll = false;

                    sb.AppendLine(String.Format("{0} (manual installation required)", dep));

                }

            }

            sb.AppendLine(Environment.NewLine + "Install them now?");

            var mRes = await metroWindow.ShowMessageAsync("Confirm", sb.ToString(), MessageDialogStyle.AffirmativeAndNegative);

            if (mRes == MessageDialogResult.Affirmative)
            {

                foreach (SBMod mod in modsNeeded)
                {
                    await DownloadInstallMod(SBModToUserMod(mod));
                }

                if (!canInstallAll)
                {

                    await metroWindow.ShowMessageAsync("Error", "Could not install all of the required dependencies. Installation Failed.");

                    return false;

                }

            }


            return true;

        }

        public async void SortAvailableModsByType(ModSortType type, bool desc)
        {

            _modSortDesc = desc;

            IsRefreshingMods = true;

            List<SBMod> mods = await _api.GetMods(_pageCtr, SearchModText, desc, type);

            AvailableMods = new ObservableCollection<UserMod>(mods.Select(SBModToUserMod));

            IsRefreshingMods = false;

        }

        public void DragOver(IDropInfo dropInfo)
        {

            if (dropInfo.Data is DataObject || dropInfo.Data is InstalledMod)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }

        }

        public async void Drop(IDropInfo dropInfo)
        {

            var o = dropInfo.Data as DataObject;

            var source = dropInfo.TargetItem as InstalledMod;
            var target = dropInfo.Data as InstalledMod;

            if (o != null && o.GetDataPresent(DataFormats.FileDrop))
            {

                string[] data = (string[])o.GetData(DataFormats.FileDrop);

                InstallMod(data[0]);

            }
            else if (source != null && target != null)
            {

                string modOrderJson = Settings.Default.ModOrder;

                if (!string.IsNullOrEmpty(modOrderJson))
                {

                    List<ModBuildOrder> modOrder = await JsonConvert.DeserializeObjectAsync<List<ModBuildOrder>>(modOrderJson);

                    var sOrder = modOrder.SingleOrDefault(p => p.ModName.Equals(source.Name, StringComparison.OrdinalIgnoreCase));
                    var tOrder = modOrder.SingleOrDefault(p => p.ModName.Equals(target.Name, StringComparison.OrdinalIgnoreCase));

                    if (sOrder != null && tOrder != null)
                    {

                        int temp = tOrder.BuildOrder;

                        tOrder.BuildOrder = sOrder.BuildOrder;
                        sOrder.BuildOrder = temp;

                    }

                    Settings.Default.ModOrder = await JsonConvert.SerializeObjectAsync(modOrder);
                    Settings.Default.Save();

                }

                await RefreshModOrder(Settings.Default.MergeMods);

            }

        }

        private int _amountOfModsInstalled;

        public int AmountOfModsInstalled
        {
            get
            {
                return _amountOfModsInstalled;
            }
            set
            {

                if (_amountOfModsInstalled == value)
                    return;

                _amountOfModsInstalled = value;

                NotifyOfPropertyChange(() => AmountOfModsInstalled);

            }
        }

        private ObservableCollection<UserMod> _availableMods;
        private ObservableCollection<InstalledMod> _installedMods;

        private ObservableCollection<UserMod> _myMods;
        private ObservableCollection<ModVersion> _myModVersions;

        private ModVersion _selectedModVersion;
        private ModVersion _mySelectedModVersion;

        private AccountInfo _myAccountInfo;

        public ObservableCollection<UserMod> AvailableMods
        {
            get
            {
                return _availableMods;
            }
            set
            {

                if (_availableMods == value)
                    return;

                _availableMods = value;


                NotifyOfPropertyChange(() => AvailableMods);

            }
        }

        public ObservableCollection<InstalledMod> InstalledMods
        {
            get
            {
                return _installedMods;
            }
            set
            {

                if (_installedMods == value)
                    return;

                _installedMods = value;

                NotifyOfPropertyChange(() => InstalledMods);

            }
        }

        public ObservableCollection<UserMod> MyMods
        {
            get
            {
                return _myMods;
            }
            set
            {

                if (_myMods == value)
                    return;

                _myMods = value;

                NotifyOfPropertyChange(() => MyMods);

            }
        }

        public ObservableCollection<ModVersion> MyModVersions
        {
            get
            {
                return _myModVersions;
            }
            set
            {

                if (_myModVersions == value)
                    return;

                _myModVersions = value;

                NotifyOfPropertyChange(() => MyModVersions);

            }
        }

        public UserMod SelectedMod
        {
            get
            {
                return _selectedMod;
            }
            set
            {

                if (_selectedMod == value)
                    return;

                _selectedMod = value;

                IsModInfoOpen = false;

                if (value != null)
                    SelectedModVersion = value.Versions.LastOrDefault();

                NotifyOfPropertyChange(() => SelectedMod);

            }
        }

        public ModVersion SelectedModVersion
        {
            get
            {
                return _selectedModVersion;
            }
            set
            {

                if (_selectedModVersion == value)
                    return;

                _selectedModVersion = value;

                NotifyOfPropertyChange(() => SelectedModVersion);

            }
        }

        public override string WindowTitle
        {
            get
            {

                Version ver = Assembly.GetExecutingAssembly().GetName().Version;

                return String.Format("ModBound - Version {0}.{1}.{2}", ver.Major, ver.Minor, ver.Build);

            }
        }

        public UserMod MySelectedMod
        {
            get
            {
                return _mySelectedMod;
            }
            set
            {

                if (_mySelectedMod == value)
                    return;

                _mySelectedMod = value;

                if (value != null)
                {

                    MyModVersions.Clear();

                    foreach (var version in value.Versions)
                    {
                        MyModVersions.Add(version);
                    }

                    MySelectedModVersion = value.Versions.LastOrDefault();

                }

                NotifyOfPropertyChange(() => MySelectedMod);

            }
        }

        public ModVersion MySelectedModVersion
        {
            get
            {
                return _mySelectedModVersion;
            }
            set
            {

                if (_mySelectedModVersion == value)
                    return;

                _mySelectedModVersion = value;

                NotifyOfPropertyChange(() => MySelectedModVersion);

            }
        }

        public string Status
        {
            get { return _status; }
            set
            {

                if (_status == value)
                    return;

                _status = value;

                NotifyOfPropertyChange(() => Status);

            }
        }

        public bool IsModInfoOpen
        {
            get
            {
                return _isModInfoOpen;
            }
            set
            {

                if (_isModInfoOpen == value)
                    return;

                _isModInfoOpen = value;

                NotifyOfPropertyChange(() => IsModInfoOpen);

            }
        }

        public string SbInstallFolder
        {
            get
            {
                return _sbInstallFolder;
            }
            set
            {

                if (_sbInstallFolder == value)
                    return;

                _sbInstallFolder = value;

                NotifyOfPropertyChange(() => SbInstallFolder);

            }
        }

        public string SearchModText
        {
            get
            {
                return _searchModText;
            }
            set
            {

                if (_searchModText == value)
                    return;

                _searchModText = value;

                NotifyOfPropertyChange(() => SearchModText);

            }
        }

        public bool IsRefreshingSelectedMod
        {
            get
            {
                return _isRefreshingSelectedMod;
            }
            set
            {

                if (_isRefreshingSelectedMod == value)
                    return;

                _isRefreshingSelectedMod = value;

                NotifyOfPropertyChange(() => IsRefreshingSelectedMod);

            }
        }

        public bool IsRefreshingMyMods
        {
            get
            {
                return _isRefreshingMyMods;
            }
            set
            {

                if (_isRefreshingMyMods == value)
                    return;

                _isRefreshingMyMods = value;

                NotifyOfPropertyChange(() => IsRefreshingMyMods);

            }
        }

        public bool IsRefreshingMods
        {
            get
            {
                return _isRefreshingMods;
            }
            set
            {

                if (_isRefreshingMods == value)
                    return;

                _isRefreshingMods = value;

                NotifyOfPropertyChange(() => IsRefreshingMods);

            }
        }

        public bool IsRefreshingInstalledMods
        {
            get
            {
                return _isRefreshingInstalledMods;
            }
            set
            {

                if (_isRefreshingInstalledMods == value)
                    return;

                _isRefreshingInstalledMods = value;

                NotifyOfPropertyChange(() => IsRefreshingInstalledMods);

            }
        }

        public string LoginButtonText
        {
            get
            {
                return _loginButtonText;
            }
            set
            {

                if (_loginButtonText == value)
                    return;

                _loginButtonText = value;

                NotifyOfPropertyChange(() => LoginButtonText);

            }
        }

        public Visibility RegisterButtonVisible
        {
            get
            {
                return _registerButtonVisible;
            }
            set
            {

                if (_registerButtonVisible == value)
                    return;

                _registerButtonVisible = value;

                NotifyOfPropertyChange(() => RegisterButtonVisible);

            }
        }

        public bool IsSignedIn
        {
            get
            {
                return _isSignedIn;
            }
            set
            {

                if (_isSignedIn == value)
                    return;

                _isSignedIn = value;

                NotifyOfPropertyChange(() => IsSignedIn);

            }
        }

        public AccountInfo MyAccountInfo
        {
            get
            {
                return _myAccountInfo;
            }
            set
            {

                if (_myAccountInfo == value)
                    return;

                _myAccountInfo = value;

                NotifyOfPropertyChange(() => MyAccountInfo);

            }
        }

    }
}
