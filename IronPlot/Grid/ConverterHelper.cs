using System;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows.Data;

namespace IronPlot
{
    #region Converters

    public class SortDirectionConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            switch ((ListSortDirection)value)
            {
                case ListSortDirection.Ascending:
                    return "Ascending";
                case ListSortDirection.Descending:
                    return "Descending";
                default:
                    break;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((string)value)
            {
                case "null":
                    return null;
                case "Ascending":
                    return ListSortDirection.Ascending;
                case "Descending":
                    return ListSortDirection.Descending;
                default:
                    break;
            }

            return null;
        }

        #endregion
    }

    #endregion Converters

    public class GridHelpers
    {
        public static DataView ArrayToView(double[,] array)
        {
            var rows = array.GetLength(0);
            var cols = array.GetLength(1);

            var table = new DataTable();
            for (var j = 0; j < cols; ++j)
                table.Columns.Add(new DataColumn(j.ToString(), typeof(Double)));

            for (var i = 0; i < rows; ++i)
            {
                var rowData = new object[cols];
                for (var j = 0; j < cols; ++j) rowData[j] = array[i, j];
                var row = table.LoadDataRow(rowData, false);
            }
            var view = table.DefaultView;
            view.AllowNew = false;
            return view;
        }
    }
}
