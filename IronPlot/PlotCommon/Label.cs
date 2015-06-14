using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class Label : TextBlock
    {
        public Label()
        {
            Visibility = Visibility.Collapsed;
            var descriptor =
                DependencyPropertyDescriptor.FromProperty(
                TextProperty, typeof(TextBlock));
            descriptor.AddValueChanged(this, OnTextChanged);
        }

        private void OnTextChanged(object sender, EventArgs args)
        {
            if (Text == String.Empty && Text == "") Visibility = Visibility.Collapsed;
            else Visibility = Visibility.Visible;
        }
    }
}
