// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace IronPlot
{
    public enum SortedValues { X, Y }
    
    public partial class Curve
    {
        internal double[] x, y;
        internal double[] XTransformed, YTransformed;
        internal SortedValues SortedValues;
        internal double[] TransformedSorted;
        internal int[] SortedToUnsorted;

        internal bool[] IncludeLinePoint; // Whether or not to include the point in the line Geometry.
        internal bool[] IncludeMarker; // Whether or not to include the marker in the Geometry.
        protected byte[] PointRegion;
        protected int N;

        protected Matrix CachedTransform = Matrix.Identity;
        protected Rect CachedRegion = new Rect(0, 0, 0, 0);

        public double[] X => x;

        public double[] Y => y;

        public Curve(double[] x, double[] y)
        {
            this.x = x; this.y = y;
            Validate();
            Transform(null, null);
            PrepareLineData(x.Length);
            DetermineSorted();
        }

        public Curve(IEnumerable<double> x, IEnumerable<double> y)
        {
            var count = Math.Min(x.Count(), y.Count());
            this.x = new double[count];
            this.y = new double[count];
            for (var i = 0; i < count; ++i)
            {
                this.x[i] = x.ElementAt(i);
                this.y[i] = y.ElementAt(i);
            }
            Transform(null, null);
            PrepareLineData(count);
            DetermineSorted();
        }

        public Rect Bounds()
        {
            return new Rect(new Point(XTransformed.Min(), YTransformed.Min()), new Point(XTransformed.Max(), YTransformed.Max()));
        }

        private void PrepareLineData(int length)
        {
            N = length;
            IncludeLinePoint = new bool[length];
            IncludeMarker = new bool[length];
            PointRegion = new byte[length];
            for (var i = 0; i < length; ++i)
            {
                IncludeLinePoint[i] = true;
                IncludeMarker[i] = true;
            }
        }

        /// <summary>
        /// Determine if either x or y values are sorted; if neither, sort x.
        /// </summary>
        private void DetermineSorted()
        {
            SortedToUnsorted = Enumerable.Range(0, XTransformed.Length).ToArray();
            if (IsSorted(XTransformed))
            {
                TransformedSorted = XTransformed;
                SortedValues = SortedValues.X;
            }
            else if (IsSorted(YTransformed))
            {
                TransformedSorted = YTransformed;
                SortedValues = SortedValues.Y;
            }
            else
            {
                TransformedSorted = (double[])XTransformed.Clone();
                Array.Sort(TransformedSorted, SortedToUnsorted);
            }
        }

        private bool IsSorted(double[] array)
        {
            for (var i = 1; i < array.Length - 1; ++i)
            {
                if (array[i] < array[i - 1]) return false;
            }
            return true;
        }

        protected void Validate()
        {
            if (x.Length != y.Length)
            {
                throw new ArgumentException("Component vectors' lengths must be equal");
            }
        }

        internal void Transform(Func<double, double> graphTransformX, Func<double, double> graphTransformY)
        {
            if (graphTransformX == null)
            {
                XTransformed = x;
            }
            else
            {
                var length = x.Length;
                XTransformed = new double[length];
                for (var i = 0; i < length; ++i) XTransformed[i] = graphTransformX(x[i]);
            }
            if (graphTransformY == null)
            {
                YTransformed = y;
            }
            else
            {
                var length = y.Length;
                YTransformed = new double[length];
                for (var i = 0; i < length; ++i) YTransformed[i] = graphTransformY(y[i]);
            }
            DetermineSorted();
        }

        public void FilterMinMax(MatrixTransform canvasToGraph, Rect viewBounds)
        {
            if (XTransformed.Length <= 2) return;
            // We do not need to re-evaluate the set of lines if the view is contained by the cached region
            // and the size of the region is not significantly changed.
            var width = Math.Max(viewBounds.Width, canvasToGraph.Matrix.M11 * 500);
            var height = Math.Max(viewBounds.Height, canvasToGraph.Matrix.M22 * 500);
            var xViewMin = viewBounds.Left - width / 2;
            var xViewMax = viewBounds.Right + width / 2;
            var yViewMin = viewBounds.Top - height / 2;
            var yViewMax = viewBounds.Bottom + height / 2;
            var widthRatio = canvasToGraph.Matrix.M11 /CachedTransform.M11; 
            var heightRatio = canvasToGraph.Matrix.M22 /CachedTransform.M22; 
            if (ContainsRegion(CachedRegion, viewBounds) && (widthRatio > 0.9) && (widthRatio < 1.1)
                && (heightRatio > 0.9) && (heightRatio < 1.1)) return;
            CachedRegion = new Rect(new Point(xViewMin, yViewMin), new Point(xViewMax, yViewMax));
            CachedTransform.M11 = canvasToGraph.Matrix.M11;
            CachedTransform.M22 = canvasToGraph.Matrix.M22;

            // Exclude all line points by default: these will subsequently be added as necessary.
            // Include those marker points that are in the cached region and make note of region.
            var nPoints = IncludeLinePoint.Length;
            for (var j = 0; j < nPoints; ++j)
            {
                IncludeLinePoint[j] = false;
                IncludeMarker[j] = false;
                var newX = XTransformed[j]; var newY = YTransformed[j];
                if (newX < xViewMin) PointRegion[j] = 1;
                else if (newX > xViewMax) PointRegion[j] = 2;
                else if (newY < yViewMin) PointRegion[j] = 4;
                else if (newY > yViewMax) PointRegion[j] = 8;
                else
                {
                    PointRegion[j] = 0;
                    IncludeMarker[j] = true;
                }
            }
            double xStart, yStart;
            double xMax, xMin, yMax, yMin;
            double xMax2, xMin2, yMax2, yMin2;
            int xMaxIndex, xMinIndex, yMaxIndex, yMinIndex;
            bool withinXBound, withinYBound;
            var deltaX = Math.Abs(canvasToGraph.Matrix.M11) * 0.25; 
            var deltaY = Math.Abs(canvasToGraph.Matrix.M22) * 0.25;
            var deltaX2 = Math.Abs(canvasToGraph.Matrix.M11) * 0.75;
            var deltaY2 = Math.Abs(canvasToGraph.Matrix.M22) * 0.75;
            var i = 0;
            var region = PointRegion[i]; byte newRegion;
            while (true)
            {
                newRegion = PointRegion[i + 1];
                // If the current point is outside the cached region, and the next point is in the same region, then we start to exclude points.
                if ((region > 0) && (region == newRegion))
                {
                    // Exclude until the current point is in a different region, or until we reach the penultimate point of the series.
                    while ((region == newRegion) && (i < nPoints - 2))
                    {
                        ++i;
                        newRegion = PointRegion[i + 1];
                    }
                    // This is the penultimate point and both this and the last point should be excluded:
                    if (region == newRegion) break;
                    // Otherwise we need to include both this and the next point.
                    IncludeLinePoint[i] = true;
                    IncludeLinePoint[i + 1] = true;
                    ++i;
                }
                else
                {
                    IncludeLinePoint[i] = true;
                }
                // Now do max-min filtration
                xStart = XTransformed[i]; yStart = YTransformed[i];
                ++i;
                if (i == nPoints) break;
                xMax = xStart + deltaX; xMin = xStart - deltaX;
                yMax = yStart + deltaY; yMin = yStart - deltaY;
                xMax2 = xStart + deltaX2; xMin2 = xStart - deltaX2;
                yMax2 = yStart + deltaY2; yMin2 = yStart - deltaY2;
                xMaxIndex = -1; xMinIndex = -1; yMaxIndex = -1; yMinIndex = -1;
                withinXBound = true; withinYBound = true;
                // Do max-min filtration:
                while (true)
                {
                    var newX = XTransformed[i]; var newY = YTransformed[i];
                    if (newX > xMax)
                    {
                        xMax = newX;
                        xMaxIndex = i;
                        if (newX > xMax2)
                        {
                            withinXBound = false;
                            if (!withinYBound)
                            {
                                if (yMaxIndex > -1) IncludeLinePoint[yMaxIndex] = true;
                                if (yMinIndex > -1) IncludeLinePoint[yMinIndex] = true;
                                break;
                            }
                        }
                    }
                    else if (newX < xMin)
                    {
                        xMin = newX;
                        xMinIndex = i;
                        if (newX < xMin2)
                        {
                            withinXBound = false;
                            if (!withinYBound)
                            {
                                if (yMaxIndex > -1) IncludeLinePoint[yMaxIndex] = true;
                                if (yMinIndex > -1) IncludeLinePoint[yMinIndex] = true;
                                break;
                            }
                        }
                    }
                    if (newY > yMax)
                    {
                        yMax = newY;
                        yMaxIndex = i;
                        if (newY > yMax2)
                        {
                            withinYBound = false;
                            if (!withinXBound)
                            {
                                if (xMaxIndex > -1) IncludeLinePoint[xMaxIndex] = true;
                                if (xMinIndex > -1) IncludeLinePoint[xMinIndex] = true;
                                break;
                            }
                        }
                    }
                    else if (newY < yMin)
                    {
                        yMin = newY;
                        yMinIndex = i;
                        if (newY < yMin2)
                        {
                            withinYBound = false;
                            if (!withinXBound)
                            {
                                if (xMaxIndex > -1) IncludeLinePoint[xMaxIndex] = true;
                                if (xMinIndex > -1) IncludeLinePoint[xMinIndex] = true;
                                break;
                            }
                        }
                    }
                    if (i == (nPoints - 1)) break;
                    ++i;
                }
                IncludeLinePoint[i] = true;
                IncludeLinePoint[i - 1] = true;
                if (i == (nPoints - 1)) break;
                region = PointRegion[i];
            }
            var included = 0;
            for (var j = 0; j < IncludeLinePoint.Length; ++j)
            {
                if (IncludeLinePoint[j]) ++included;
            }

        }

        protected bool ContainsRegion(Rect container, Rect contained)
        {
            var contains = true;
            contains = (contained.Left >= container.Left) && (contained.Right <= container.Right)
                && (contained.Top >= container.Top) && (contained.Bottom <= container.Bottom);
            return contains;
        }

        public void FilterLinInterp(MatrixTransform canvasToGraph)
        {
            for (var j = 0; j < IncludeLinePoint.Length; ++j)
            {
                IncludeLinePoint[j] = true;
            }
            double x1, y1, x2, y2, x3, y3;
            var i = 0;
            var nExcluded = 0;
            var newlyExcluded = true;
            x1 = x[0]; y1 = y[0];
            var toPotentiallyExclude = 0;
            var cutOffX = Math.Abs(canvasToGraph.Matrix.M11);
            var cutOffY = Math.Abs(canvasToGraph.Matrix.M22);
            while ((nExcluded < (X.Length - 100)) && newlyExcluded)
            {
                newlyExcluded = false;
                i = 0; x1 = x[0]; y1 = y[0];
                while (i < (X.Length - 4))
                {
                    ++i; // Find new point to potentially miss out
                    while (!IncludeLinePoint[i])
                    {
                        i += 1;
                    }
                    toPotentiallyExclude = i;
                    if (i > X.Length - 4) break;  
                    x2 = x[i];
                    y2 = y[i];
                    ++i; // Find new point to draw line to
                    while (!IncludeLinePoint[i])
                    {
                        i += 1;
                    }
                    x3 = x[i];
                    y3 = y[i];
                    if (((Math.Abs(x2 - x1) < cutOffX) || (Math.Abs(x3 - x2) < cutOffX)) ||
                        (Math.Abs((y1 + (x2 - x1) * (y3 - y1) / (x3 - x1) - y2)) < cutOffY))
                    {
                        IncludeLinePoint[toPotentiallyExclude] = false;
                        newlyExcluded = true;
                        nExcluded += 1;
                        x1 = x3; y1 = y3;
                    }
                    else
                    {
                        x1 = x2; y1 = y2;
                    }
                }
            }
            var totalExcluded = nExcluded;
            var remaining = X.Length - nExcluded;
        }

    }
}
