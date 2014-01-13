using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Caliburn.Micro.Contrib.Results;
using ModBound.Properties;
using ModBoundLib;

namespace ModBound.ViewModels
{
    public class SettingsViewModel : MBViewModel
    {

        public SettingsViewModel()
        {
            StarboundInstallFolder = Settings.Default.SBInstallFolder;
            MergeMods = Settings.Default.MergeMods;
        }

        public void Save()
        {

            Settings.Default.MergeMods = MergeMods;
            Settings.Default.SBInstallFolder = StarboundInstallFolder;
            Settings.Default.Save();

            TryClose(true);

        }

        public void Cancel()
        {
            TryClose(false);
        }

        public async void Browse()
        {

            var browseResult = new BrowseFolderResult("Select the Starbound installation folder")
                .In(Environment.SpecialFolder.MyComputer)
                .WithSelectedPathDo(x =>
                {

                    DirectoryInfo dInfo = new DirectoryInfo(x);

                    if (dInfo.Exists && dInfo.GetDirectories().Any(p => p.Name.Equals(StarBound.WindowsFolder, StringComparison.OrdinalIgnoreCase)))
                    {
                        StarboundInstallFolder = x;
                    }
                    
                });

            try
            {
                await browseResult.ExecuteAsync();
            }
            catch (Exception)
            {
            }

        }

        private string _starboundInstallFolder;

        public string StarboundInstallFolder
        {
            get
            {
                return _starboundInstallFolder;
            }
            set
            {

                if (_starboundInstallFolder == value)
                    return;

                _starboundInstallFolder = value;

                NotifyOfPropertyChange(() => StarboundInstallFolder);

            }
        }

        private bool _mergeMods;

        public bool MergeMods
        {
            get
            {
                return _mergeMods;
            }
            set
            {

                if (_mergeMods == value)
                    return;

                _mergeMods = value;

                NotifyOfPropertyChange(() => MergeMods);

            }
        }

        public override string WindowTitle
        {
            get { return "Settings"; }
        }

    }
}
