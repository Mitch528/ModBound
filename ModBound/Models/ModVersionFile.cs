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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModBound.Models
{
    public class ModVersionFile : INotifyPropertyChanged
    {

        private string _fileName;

        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {

                if (_fileName == value)
                    return;

                _fileName = value;

                OnPropertyChanged();

            }
        }

        private ImageSource _image;

        public ImageSource Image
        {
            get
            {
                return _image;
            }
            set
            {

                if (_image == value)
                    return;

                _image = value;

                OnPropertyChanged();

            }
        }

        private bool _isLocked;

        public bool IsLocked
        {
            get
            {
                return _isLocked;
            }
            set
            {

                if (_isLocked == value)
                    return;

                _isLocked = value;

                OnPropertyChanged();

            }
        }

        public ModVersionFile()
        {
            Image = new BitmapImage();
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
