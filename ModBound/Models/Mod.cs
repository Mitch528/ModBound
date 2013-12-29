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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModBound.Models
{
    public class Mod : INotifyPropertyChanged
    {

        private int _id;

        public int ID
        {
            get
            {
                return _id;
            }
            set
            {

                if (_id == value)
                    return;

                _id = value;

                OnPropertyChanged();

            }
        }

        private string _name;

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {

                if (_name == value)
                    return;

                _name = value;

                OnPropertyChanged();

            }
        }

        private string _author;

        public string Author
        {
            get
            {
                return _author;
            }
            set
            {

                if (_author == value)
                    return;

                _author = value;

                OnPropertyChanged();

            }
        }

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

        private string _description;

        public string Description
        {
            get
            {
                return _description;
            }
            set
            {

                if (_description == value)
                    return;

                _description = value;

                OnPropertyChanged();

            }
        }

        private string _modTypes;

        public string ModTypes
        {
            get
            {
                return _modTypes;
            }
            set
            {

                if (_modTypes == value)
                    return;

                _modTypes = value;

                OnPropertyChanged();

            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
