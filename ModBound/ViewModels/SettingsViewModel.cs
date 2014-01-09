using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModBound.Properties;

namespace ModBound.ViewModels
{
    public class SettingsViewModel : MBViewModel
    {

        public SettingsViewModel()
        {
            MergeMods = Settings.Default.MergeMods;
        }

        public void Save()
        {

            Settings.Default.MergeMods = MergeMods;
            Settings.Default.Save();

            TryClose(true);

        }

        public void Cancel()
        {
            TryClose(false);
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
