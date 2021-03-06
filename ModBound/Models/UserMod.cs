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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModBound.Models
{
    public class UserMod : Mod
    {

        private int _downloads;

        public int Downloads
        {
            get
            {
                return _downloads;
            }
            set
            {

                if (_downloads == value)
                    return;

                _downloads = value;

                OnPropertyChanged();

            }
        }

        private string _lastUpdated;

        public string LastUpdated
        {
            get
            {
                return _lastUpdated;
            }
            set
            {

                if (_lastUpdated == value)
                    return;

                _lastUpdated = value;

                OnPropertyChanged();

            }
        }

        public ObservableCollection<ModVersion> Versions { get; set; }

        public UserMod()
        {
            Versions = new ObservableCollection<ModVersion>();
        }

    }
}
