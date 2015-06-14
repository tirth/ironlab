using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace IronPlot
{
    public class DataGrid : System.Windows.Controls.DataGrid
    {
        static DataGrid()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(DataGrid),
                new CommandBinding(ApplicationCommands.Paste,
                    OnExecutedPaste,
                    OnCanExecutePaste));
        }

        #region Clipboard Paste

        private static void OnCanExecutePaste(object target, CanExecuteRoutedEventArgs args)
        {
            ((DataGrid)target).OnCanExecutePaste(args);
        }

        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command query its state.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanExecutePaste(CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = CurrentCell != null;
            args.Handled = true;
        }

        private static void OnExecutedPaste(object target, ExecutedRoutedEventArgs args)
        {
            ((DataGrid)target).OnExecutedPaste(args);
        }

        /// <summary> 
        /// This virtual method is called when ApplicationCommands.Paste command is executed. 
        /// </summary> 
        /// <param name="args"></param> 
        protected virtual void OnExecutedPaste(ExecutedRoutedEventArgs args)
        {
            Debug.WriteLine("OnExecutedPaste begin");

            // parse the clipboard data             
            var rowData = ClipboardHelper.ParseClipboardData();

            // call OnPastingCellClipboardContent for each cell 
            //int nSelectedCells = 
            var minRowIndex = Items.IndexOf(CurrentItem);
            var maxRowIndex = Items.Count - 1;
            var minColumnDisplayIndex = (SelectionUnit != DataGridSelectionUnit.FullRow) ? Columns.IndexOf(CurrentColumn) : 0;
            var maxColumnDisplayIndex = Columns.Count - 1;
            if (SelectedCells.Count > 1) GetSelectionBounds(ref minRowIndex, ref maxRowIndex, ref minColumnDisplayIndex, ref maxColumnDisplayIndex);
            var rowDataIndex = 0;
            for (var i = minRowIndex; i <= maxRowIndex && rowDataIndex < rowData.Count; i++, rowDataIndex++)
            {
                var columnDataIndex = 0;
                for (var j = minColumnDisplayIndex; j <= maxColumnDisplayIndex && columnDataIndex < rowData[rowDataIndex].Length; j++, columnDataIndex++)
                {
                    var column = ColumnFromDisplayIndex(j);
                    column.OnPastingCellClipboardContent(Items[i], rowData[rowDataIndex][columnDataIndex]);
                }
            }
        }

        private void GetSelectionBounds(ref int minRow, ref int maxRow, ref int minColumn, ref int maxColumn)
        {
            foreach (var cell in SelectedCells)
            {
                var row = Items.IndexOf(cell.Item);
                var column = Columns.IndexOf(cell.Column);
                if (row < minRow) minRow = row;
                if (row > maxRow) maxRow = row;
                if (column < minColumn) minColumn = column;
                if (column > maxColumn) maxColumn = column;
            }
        } 
        
        #endregion Clipboard Paste

    }
}
