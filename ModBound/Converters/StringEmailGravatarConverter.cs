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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModBound.Extensions;

namespace ModBound.Converters
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public class StringEmailGravatarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            
            if (targetType != typeof(ImageSource))
                throw new InvalidOperationException("The target must be of type System.Windows.Media.ImageSource");

            string email = value.ToString().ToLower();

            BitmapImage src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(String.Format("http://www.gravatar.com/avatar/{0}?s=152", email.CalculateMD5Hash().ToLower()));
            src.EndInit();

            return src;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
