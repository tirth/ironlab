// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace IronPlot.Plotting3D
{
    public enum GraphSides { MinusX, MinusY, MinusZ, PlusX, PlusY, PlusZ };
    
    public class Axes3D : Model3D
    {
        public static readonly DependencyProperty GraphMinProperty =
            DependencyProperty.Register("GraphMin",
                typeof(Point3D), typeof(Axes3D),
                new PropertyMetadata(new Point3D(-10, -10, -10), OnUpdateGraphMaxMin));

        public static readonly DependencyProperty GraphMaxProperty =
            DependencyProperty.Register("GraphMax",
                typeof(Point3D), typeof(Axes3D),
                new PropertyMetadata(new Point3D(10, 10, 10), OnUpdateGraphMaxMin));

        private static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register("LineThickness",
            typeof(double),
            typeof(Axes3D),
                new PropertyMetadata(1.5, LineThicknessChanged));

        private readonly LabelProperties _labelProperties;

        public LabelProperties Labels => _labelProperties;

        public Point3D GraphMin
        {
            get { return (Point3D)GetValue(GraphMinProperty); }
            set { SetValue(GraphMinProperty, value); }
        }

        public Point3D GraphMax
        {
            get { return (Point3D)GetValue(GraphMaxProperty); }
            set { SetValue(GraphMaxProperty, value); }
        }

        public double LineThickness
        {
            set { SetValue(LineThicknessProperty, value); }
            get { return (double)GetValue(LineThicknessProperty); }
        }

        protected static void OnUpdateGraphMaxMin(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axes3D)obj).Generate();
            ((Axes3D)obj).RedrawAxesLines();
            ((Axes3D)obj).UpdateLabels();
        }

        protected static void LineThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var axes = obj as Axes3D;
            foreach (LinesModel3D model in axes.Children)
            {
                model.LineThickness = (double)args.NewValue;
            }
        }

        public enum Face { MinusX, PlusX, MinusY, PlusY, MinusZ, PlusZ };
        public enum Ticks { XTicks, YTicks, ZTicks };
        public enum OpenSides { PlusXPlusY, MinusXPlusY, MinusXMinusY, PlusXMinusY  }

        public LinesModel3D MinusX, PlusX, MinusY, PlusY, MinusZ, PlusZ, Base;

        protected MatrixTransform3D modelToWorld = (MatrixTransform3D)Transform3D.Identity;

        private readonly Axis3DCollection _xAxisCollection = new Axis3DCollection();
        private readonly Axis3DCollection _yAxisCollection = new Axis3DCollection();
        private readonly Axis3DCollection _zAxisCollection = new Axis3DCollection();

        private readonly XAxis3D[] _xAxes;
        private readonly YAxis3D[] _yAxes;
        private readonly ZAxis3D[] _zAxes;
        private readonly LinesModel3D[] _sides;

        public Axis3DCollection XAxes => _xAxisCollection;

        public Axis3DCollection YAxes => _yAxisCollection;

        public Axis3DCollection ZAxes => _zAxisCollection;

        public Axes3D()
        {
            _labelProperties = new LabelProperties();
            _xAxisCollection.TickLabels.SetParent(_labelProperties);
            _yAxisCollection.TickLabels.SetParent(_labelProperties);
            _zAxisCollection.TickLabels.SetParent(_labelProperties);
            _xAxisCollection.AxisLabels.Text = "X";
            _yAxisCollection.AxisLabels.Text = "Y";
            _zAxisCollection.AxisLabels.Text = "Z";

            _sides = new LinesModel3D[6]; 
            MinusX = new LinesModel3D(); _sides[(int)GraphSides.MinusX] = MinusX;
            MinusY = new LinesModel3D(); _sides[(int)GraphSides.MinusY] = MinusY;
            MinusZ = new LinesModel3D(); _sides[(int)GraphSides.MinusZ] = MinusZ;
            PlusX = new LinesModel3D(); _sides[(int)GraphSides.PlusX] = PlusX;
            PlusY = new LinesModel3D(); _sides[(int)GraphSides.PlusY] = PlusY;
            PlusZ = new LinesModel3D(); _sides[(int)GraphSides.PlusZ] = PlusZ;

            Children.Add(MinusX);
            Children.Add(PlusX);
            Children.Add(MinusY);
            Children.Add(PlusY);
            Children.Add(MinusZ);
            Children.Add(PlusZ);

            _xAxes = new XAxis3D[2];
            _yAxes = new YAxis3D[2];
            _zAxes = new ZAxis3D[4];
            // X Axes
            _xAxes[(int)XAxisType.MinusY] = new XAxis3D(this, _xAxisCollection, XAxisType.MinusY);
            _xAxes[(int)XAxisType.PlusY] = new XAxis3D(this, _xAxisCollection, XAxisType.PlusY);
            _xAxisCollection.AddAxis(_xAxes[(int)XAxisType.MinusY]); _xAxisCollection.AddAxis(_xAxes[(int)XAxisType.PlusY]); 

            // Y Axes
            _yAxes[(int)YAxisType.MinusX] = new YAxis3D(this, _yAxisCollection, YAxisType.MinusX);
            _yAxes[(int)YAxisType.PlusX] = new YAxis3D(this, _yAxisCollection, YAxisType.PlusX);
            _yAxisCollection.AddAxis(_yAxes[(int)YAxisType.MinusX]); _yAxisCollection.AddAxis(_yAxes[(int)YAxisType.PlusX]); 

            // Z Axes
            _zAxes[(int)ZAxisType.MinusXMinusY] = new ZAxis3D(this, _zAxisCollection, ZAxisType.MinusXMinusY);
            _zAxes[(int)ZAxisType.MinusXPlusY] = new ZAxis3D(this, _zAxisCollection, ZAxisType.MinusXPlusY);
            _zAxes[(int)ZAxisType.PlusXMinusY] = new ZAxis3D(this, _zAxisCollection, ZAxisType.PlusXMinusY);
            _zAxes[(int)ZAxisType.PlusXPlusY] = new ZAxis3D(this, _zAxisCollection, ZAxisType.PlusXPlusY);
            _zAxisCollection.AddAxis(_zAxes[(int)ZAxisType.MinusXMinusY]); _zAxisCollection.AddAxis(_zAxes[(int)ZAxisType.MinusXPlusY]);
            _zAxisCollection.AddAxis(_zAxes[(int)ZAxisType.PlusXMinusY]); _zAxisCollection.AddAxis(_zAxes[(int)ZAxisType.PlusXPlusY]);

            Base = new LinesModel3D(); 
            Children.Add(Base); // Add base last so that this can overwrite other lines.
            PlusZ.IsVisible = false;
            // Note axes are already added as Children
            Generate();
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
 	        base.OnViewportImageChanged(newViewportImage);
            UpdateLabels();
            OnDraw -= OnDrawUpdate;
            OnDraw += OnDrawUpdate;
        }

        /// <summary>
        /// Called on draw: used to update WPF elements on the 2D layer
        /// </summary>
        protected void OnDrawUpdate(object sender, EventArgs e)
        {
            UpdateLabelPositions();
        }

        readonly double _piBy2 = Math.PI / 2;
        readonly double _pi = Math.PI;

        internal void UpdateOpenSides(double phi)
        {
            if (phi > 0 && phi < _piBy2) SetVisibleSidesAndAxes(OpenSides.PlusXPlusY);
            else if (phi >= _piBy2 && phi <= _pi) SetVisibleSidesAndAxes(OpenSides.MinusXPlusY);
            else if (phi <= 0 && phi > -_piBy2) SetVisibleSidesAndAxes(OpenSides.PlusXMinusY);
            else SetVisibleSidesAndAxes(OpenSides.MinusXMinusY);
        }

        protected void SetVisibleSidesAndAxes(OpenSides openSides)
        {
            int openSide1;
            int openSide2;
            int visibleXAxis;
            int visibleYAxis;
            int visibleZAxis;
            switch (openSides)
            {
                case OpenSides.MinusXMinusY:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.MinusXPlusY;
                    break;
                case OpenSides.MinusXPlusY:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.PlusY;
                    visibleXAxis = (int)XAxisType.PlusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.PlusXPlusY;
                    break;
                case OpenSides.PlusXMinusY:
                    openSide1 = (int)GraphSides.PlusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.PlusX;
                    visibleZAxis = (int)ZAxisType.MinusXMinusY;
                    break;
                case OpenSides.PlusXPlusY:
                    openSide1 = (int)GraphSides.PlusX;
                    openSide2 = (int)GraphSides.PlusY;
                    visibleXAxis = (int)XAxisType.PlusY;
                    visibleYAxis = (int)YAxisType.PlusX;
                    visibleZAxis = (int)ZAxisType.PlusXMinusY;
                    break;
                default:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.PlusXMinusY;
                    break;
            }
            foreach (var axis in _xAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            foreach (var axis in _yAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            foreach (var axis in _zAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            _xAxes[visibleXAxis].LabelsVisible = true; _xAxes[visibleXAxis].TicksVisible = true;
            _yAxes[visibleYAxis].LabelsVisible = true; _yAxes[visibleYAxis].TicksVisible = true;
            _zAxes[visibleZAxis].LabelsVisible = true; _zAxes[visibleZAxis].TicksVisible = true;
            foreach (var side in _sides) { side.IsVisible = true; }
            _sides[openSide1].IsVisible = false; _sides[openSide2].IsVisible = false;
            PlusZ.IsVisible = false;
        }

        internal void RedrawAxesLines()
        {
            foreach (var model in Children)
            {
                (model as LinesModel3D).UpdateFromPoints();
            }
        }

        internal void AddBase(Point3D graphMin, Point3D graphMax)
        {
            Base.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
        }

        internal void AddGridLines(List<Point3DColor> points, Face face, Ticks ticks, Color gridColor)
        {
            var axisList = new List<Axis3D>(3);
            axisList.Add(_xAxes[0]); axisList.Add(_yAxes[0]); axisList.Add(_zAxes[0]);
            int constIndex; // Point dimension with all constant values
            double constValue;
            int ticksIndex; // Point dimension spanned by ticks
            var lineIndex = 0; // Point dimension spanned by single line 
            constIndex = (int)face / 2;
            constValue = ((int)face % 2) == 0 ? axisList[constIndex].Min : axisList[constIndex].Max;
            ticksIndex = (int)ticks;
            if (ticksIndex == constIndex)
            {
                return;
            }
            for (var i = 0; i < 3; ++i)
            {
                if ((i != constIndex) && (i != ticksIndex)) { lineIndex = i; break; }
            }
            var ticksValue = axisList[ticksIndex].Ticks;
            var min = axisList[ticksIndex].Min;
            var max = axisList[ticksIndex].Max;
            var lineValue = new double[2];
            lineValue[0] = axisList[lineIndex].Min;
            lineValue[1] = axisList[lineIndex].Max;
            var startPoint = new Double[3];
            var endPoint = new Double[3];
            startPoint[constIndex] = constValue;
            endPoint[constIndex] = constValue;
            startPoint[lineIndex] = lineValue[0];
            endPoint[lineIndex] = lineValue[1];
            for (var i = 0; i < ticksValue.Length; ++i)
            {
                if (ticksValue[i] == min || ticksValue[i] == max) continue;
                startPoint[ticksIndex] = endPoint[ticksIndex] = ticksValue[i];
                points.Add(new Point3DColor(startPoint[0], startPoint[1], startPoint[2], gridColor));
                points.Add(new Point3DColor(endPoint[0], endPoint[1], endPoint[2], gridColor));
            }
        }

        internal void UpdateLabels()
        {
            if (layer2D != null)
            {
                foreach (var axis in _xAxes) axis.UpdateLabels();
                foreach (var axis in _yAxes) axis.UpdateLabels();
                foreach (var axis in _zAxes) axis.UpdateLabels();
            }
        }

        internal void UpdateLabelPositions()
        {
            if (layer2D != null)
            {
                foreach (var axis in _xAxes) axis.UpdateLabelPositions(true);
                foreach (var axis in _yAxes) axis.UpdateLabelPositions(true);
                foreach (var axis in _zAxes) axis.UpdateLabelPositions(true);
            }
        }

        protected void Generate()
        {
            Base.Points.Clear();
            var graphMin = GraphMin;
            var graphMax = GraphMax;
            foreach (var axis in _xAxes) axis.DeriveTicks();
            foreach (var axis in _yAxes) axis.DeriveTicks();
            foreach (var axis in _zAxes) axis.DeriveTicks();
            UpdateLabels();
            //

            PlusX.Points.Clear();
            AddGridLines(PlusX.Points, Face.PlusX, Ticks.ZTicks, Colors.Gray);
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMax.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMax.Z));
            //
            PlusY.Points.Clear();
            AddGridLines(PlusY.Points, Face.PlusY, Ticks.ZTicks, Colors.Gray);
            PlusY.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            PlusY.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMax.Z));
            PlusY.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            PlusY.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMax.Z));
            //
            MinusX.Points.Clear();
            AddGridLines(MinusX.Points, Face.MinusX, Ticks.ZTicks, Colors.Gray);
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMax.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMax.Z)); 
            //
            MinusY.Points.Clear();
            AddGridLines(MinusY.Points, Face.MinusY, Ticks.ZTicks, Colors.Gray);
            MinusY.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            MinusY.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMax.Z));
            MinusY.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            MinusY.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMax.Z));
            //
            MinusZ.Points.Clear();
            AddGridLines(MinusZ.Points, Face.MinusZ, Ticks.XTicks, Colors.Gray);
            AddGridLines(MinusZ.Points, Face.MinusZ, Ticks.YTicks, Colors.Gray);
            //
            PlusZ.Points.Clear();
            AddBase(graphMin, graphMax);

            RedrawAxesLines();
        }
    }
}
