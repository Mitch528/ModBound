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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModBound.Models
{
    public class ModVersion : INotifyPropertyChanged
    {

        private string _version;

        public string Version
        {
            get
            {
                return _version;
            }
            set
            {

                if (_version == value)
                    return;

                _version = value;

                OnPropertyChanged();

            }
        }

        private string _changes;

        public string Changes
        {
            get
            {
                return _changes;
            }
            set
            {

                if (_changes == value)
                    return;
                
                _changes = value;

                OnPropertyChanged();

            }
        }

        private DateTime _dateAdded;

        public DateTime DateAdded
        {
            get
            {
                return _dateAdded;
            }
            set
            {

                if (_dateAdded == value)
                    return;

                _dateAdded = value;

                OnPropertyChanged();

            }
        }

        public ModVersionFile File { get; set; }

        public ObservableCollection<ModVersionFile> ScreenShots { get; set; }

        public ModVersion()
        {
            File = new ModVersionFile();
            ScreenShots = new ObservableCollection<ModVersionFile>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
