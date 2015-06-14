using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    /// <summary>
    /// Interaction logic for ColourBar.xaml
    /// </summary>
    public partial class ColourBar : UserControl
    {
        internal ColourBarPanel ColourBarPanel;
        internal ColourMap ColourMap;
        private double[] _interpolationPoints;
        private readonly DispatcherTimer _colourMapUpdateTimer;
        private bool _updateInProgress;
        private readonly FalseColourImage _image;
        private List<Slider> _sliderList;

        internal double Min
        {
            set
            {
                ColourBarPanel.Axes.YAxes[0].Min = value;
                var newBounds = _image.Bounds;
                _image.Bounds = new Rect(newBounds.Left, value, newBounds.Width, newBounds.Bottom - value);
            }
            get { return ColourBarPanel.Axes.YAxes[0].Min; }
        }

        internal double Max
        {
            set
            {
                ColourBarPanel.Axes.YAxes[0].Max = value;
                var newBounds = _image.Bounds;
                _image.Bounds = new Rect(newBounds.Left, newBounds.Top, newBounds.Width, value - newBounds.Top);
            }
            get { return ColourBarPanel.Axes.YAxes[0].Max; }
        }

        public static readonly RoutedEvent ColourMapChangedEvent =
            EventManager.RegisterRoutedEvent("ColourMapChangedEvent", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(ColourBar));

        public event RoutedEventHandler ColourMapChanged
        {
            add { AddHandler(ColourMapChangedEvent, value); }
            remove { RemoveHandler(ColourMapChangedEvent, value); }
        }

        void RaiseColourMapChangedEvent()
        {
            var newEventArgs = new RoutedEventArgs(ColourMapChangedEvent);
            RaiseEvent(newEventArgs);
        }

        public ColourBar(ColourMap colourMap)
        {
            InitializeComponent();
            ColourMap = colourMap;
            ColourBarPanel = new ColourBarPanel();
            _image = new FalseColourImage(new Rect(0, Min, 1, Max), MathHelper.Counter(1, colourMap.Length), false);
            ColourBarPanel.plotItems.Add(_image);
            _image.ColourMap = colourMap;
            ColourBarPanel.Margin = new Thickness(0, 0, 5, 0);
            Grid.Children.Add(ColourBarPanel);
            _colourMapUpdateTimer = new DispatcherTimer {Interval = new TimeSpan(1000)};
            // 1/10 s
            _colourMapUpdateTimer.Tick += OnColourMapUpdateTimerElapsed;
            AddSliders();
            AddContextMenu();
            FocusVisualStyle = null;
        }

        protected void AddSliders()
        {
            _interpolationPoints = (double[])ColourMap.InterpolationPoints.Clone();
            _sliderList = new List<Slider>();
            var colourMapArray = ColourMap.ToByteArray();
            ColourBarPanel.RemoveSliders();
            for (var i = 1; i < ColourMap.InterpolationPoints.Length - 1; ++i)
            {
                var slider = new Slider();
                slider.Orientation = Orientation.Vertical;
                slider.VerticalAlignment = VerticalAlignment.Stretch;
                slider.Minimum = 0.0;                
                slider.Maximum = 1.0;
                slider.ValueChanged += slider_ValueChanged;
                _sliderList.Add(slider);
                slider.Value = ColourMap.InterpolationPoints[i];
                var index = (int)(slider.Value * (ColourMap.Length - 1));
                slider.Foreground = new SolidColorBrush
                    (Color.FromRgb(colourMapArray[index, 1], colourMapArray[index, 2], colourMapArray[index, 3]));
                slider.Template = (ControlTemplate)(Resources["colourBarVerticalSlider"]);
            }
            ColourBarPanel.AddSliders(_sliderList);
        }

        protected void slider_ValueChanged(object obj, RoutedPropertyChangedEventArgs<double> args)
        {
            var index = _sliderList.IndexOf((Slider)obj);
            if (index < (_sliderList.Count - 1))
            {
                if (args.NewValue > _sliderList[index + 1].Value)
                {
                    ((Slider)obj).Value = _sliderList[index + 1].Value;
                    args.Handled = true;
                }
            }
            if (index > 0)
            {
                if (args.NewValue < _sliderList[index - 1].Value)
                {
                    ((Slider)obj).Value = _sliderList[index - 1].Value;
                    args.Handled = true;
                }
            }
            _interpolationPoints.SetValue(((Slider)obj).Value, index + 1);
            _colourMapUpdateTimer.Start();
            RaiseColourMapChangedEvent();
        }

        private void OnColourMapUpdateTimerElapsed(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                _colourMapUpdateTimer.Start();
                return;
            }
            _colourMapUpdateTimer.Stop();
            _updateInProgress = true;
            var state = new object();
            ThreadPool.QueueUserWorkItem(UpdateColourMapAndBar, state);
        }

        private void UpdateColourMapAndBar(Object state)    
        {
            var i = 0;
            foreach (var value in _interpolationPoints)
            {
                ColourMap.InterpolationPoints[i] = value;
                ++i;
            }
            ColourMap.UpdateColourMap();
            _image.OnColourMapChanged(null, new RoutedEventArgs());
            _updateInProgress = false;
        }

        protected void AddContextMenu()
        {
            var mainMenu = new ContextMenu();

            var item1 = new MenuItem {Header = "Colourmap"};
            mainMenu.Items.Add(item1);

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);

            var item1A = new MenuItem {Header = "Jet"};
            item1.Items.Add(item1A);
            item1A.Click += Jet;

            var item1B = new MenuItem {Header = "HSV"};
            item1.Items.Add(item1B);
            item1B.Click += Hsv;

            var item1C = new MenuItem {Header = "Gray"};
            item1.Items.Add(item1C);
            item1C.Click += Gray;

            var item2 = new MenuItem {Header = "Show/Hide handles"};
            mainMenu.Items.Add(item2);
            item2.Click += ShowHideHandles;

            ContextMenu = mainMenu;
        }

        private void ShowHideHandles(object sender, EventArgs args)
        {
            foreach (var slider in ColourBarPanel.SliderList)
                slider.Visibility = slider.Visibility == Visibility.Visible
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            ColourBarPanel.InvalidateMeasure();
        }

        private void Gray(object sender, EventArgs args)
        {
            ColourMap.Gray();
            ResetHandles();
        }

        private void Jet(object sender, EventArgs args)
        {
            ColourMap.Jet();
            ResetHandles();
        }

        private void Hsv(object sender, EventArgs args)
        {
            ColourMap.Hsv();
            ResetHandles();
        }

        private void ResetHandles()
        {
            ColourMap.UpdateColourMap();
            AddSliders();
            _colourMapUpdateTimer.Start();
            RaiseColourMapChangedEvent();
            ColourBarPanel.InvalidateMeasure();
        }
    }
}
