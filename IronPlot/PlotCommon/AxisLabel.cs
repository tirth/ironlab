﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IronPlot
{
    /// <summary>
    /// A label used to display data in an axis.
    /// </summary>
    public class AxisLabel : Control
    {
        #region public string StringFormat
        /// <summary>
        /// Gets or sets the text string format.
        /// </summary>
        public string StringFormat
        {
            get { return GetValue(StringFormatProperty) as string; }
            set { SetValue(StringFormatProperty, value); }
        }

        /// <summary>
        /// Identifies the StringFormat dependency property.
        /// </summary>
        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register(
                "StringFormat",
                typeof(string),
                typeof(AxisLabel),
                new PropertyMetadata(null, OnStringFormatPropertyChanged));

        /// <summary>
        /// StringFormatProperty property changed handler.
        /// </summary>
        /// <param name="d">AxisLabel that changed its StringFormat.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnStringFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (AxisLabel)d;
            var newValue = (string)e.NewValue;
            source.OnStringFormatPropertyChanged(newValue);
        }

        /// <summary>
        /// StringFormatProperty property changed handler.
        /// </summary>
        /// <param name="newValue">New value.</param>        
        protected virtual void OnStringFormatPropertyChanged(string newValue)
        {
            UpdateFormattedContent();
        }
        #endregion public string StringFormat

        #region public string FormattedContent
        /// <summary>
        /// Gets the formatted content property.
        /// </summary>
        public string FormattedContent
        {
            get { return GetValue(FormattedContentProperty) as string; }
            protected set { SetValue(FormattedContentProperty, value); }
        }

        /// <summary>
        /// Identifies the FormattedContent dependency property.
        /// </summary>
        public static readonly DependencyProperty FormattedContentProperty =
            DependencyProperty.Register(
                "FormattedContent",
                typeof(string),
                typeof(AxisLabel),
                new PropertyMetadata(null));
        #endregion public string FormattedContent

#if !SILVERLIGHT
        /// <summary>
        /// Initializes the static members of the AxisLabel class.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Dependency properties are initialized in-line.")]
        static AxisLabel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AxisLabel), new FrameworkPropertyMetadata(typeof(AxisLabel)));
        }

#endif
        /// <summary>
        /// Instantiates a new instance of the AxisLabel class.
        /// </summary>
        public AxisLabel()
        {
#if SILVERLIGHT
            this.DefaultStyleKey = typeof(AxisLabel);
#endif
            SetBinding(FormattedContentProperty, new Binding { Converter = new StringFormatConverter(), ConverterParameter = StringFormat ?? "{0}" });
        }

        /// <summary>
        /// Updates the formatted text.
        /// </summary>
        protected virtual void UpdateFormattedContent()
        {
            SetBinding(FormattedContentProperty, new Binding { Converter = new StringFormatConverter(), ConverterParameter = StringFormat ?? "{0}" });
        }
    }

    /// <summary>
    /// Converts a value to a string using a format string.
    /// </summary>
    public class StringFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value to a string by formatting it.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">The format string.</param>
        /// <param name="culture">The culture to use for conversion.</param>
        /// <returns>The formatted string.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.CurrentCulture, (parameter as string) ?? "{0}", value);
        }

        /// <summary>
        /// Converts a value from a string to a target type.
        /// </summary>
        /// <param name="value">The value to convert to a string.</param>
        /// <param name="targetType">The target type of the conversion.</param>
        /// <param name="parameter">A parameter used during the conversion
        /// process.</param>
        /// <param name="culture">The culture to use for the conversion.</param>
        /// <returns>The converted object.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
