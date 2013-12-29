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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModBound.Models
{
    public class InstalledMod : Mod
    {

        private bool _outdated;

        public bool Outdated
        {
            get
            {
                return _outdated;
            }
            set
            {

                if (_outdated == value)
                    return;

                _outdated = value;

                OnPropertyChanged();

            }
        }


        private string _path;

        public string Path
        {
            get
            {
                return _path;
            }
            set
            {

                if (_path == value)
                    return;

                _path = value;

                OnPropertyChanged();

            }
        }

        public ObservableCollection<string> Files { get; set; }

        public InstalledMod()
        {
            Files = new ObservableCollection<string>();
        }

    }
}