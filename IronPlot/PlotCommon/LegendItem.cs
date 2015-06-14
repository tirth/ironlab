﻿using System;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class LegendItem : ContentControl
    {
        public static DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
            typeof(string), typeof(LegendItem),
            new FrameworkPropertyMetadata(String.Empty, OnTitlePropertyChanged));


        public string Title
        {
            set { SetValue(TitleProperty, value); }
            get { return (string)GetValue(TitleProperty); }
        }
        
        protected static void OnTitlePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var parent = LogicalTreeHelper.GetParent(obj) as Legend;
            if (parent != null) parent.UpdateLegendVisibility();
        }

        /// <summary>
        /// Gets or sets the owner of the LegendItem.
        /// </summary>
        public object Owner { get; set; }

#if !SILVERLIGHT
        /// <summary>
        /// Initializes the static members of the LegendItem class.
        /// </summary>
        static LegendItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LegendItem), new FrameworkPropertyMetadata(typeof(LegendItem)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            //TextBlock itemTitle =
            //    (TextBlock)this.Template.FindName("PART_ItemTitle", this);
            //Binding binding = new Binding();
            //binding.Source = this;
            //binding.Path = new PropertyPath("Title");
            //itemTitle.SetBinding(TextBlock.TextProperty, binding);
        }

#endif
    }
}
