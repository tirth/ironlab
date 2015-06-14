// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows.Media.Media3D;

namespace IronPlot.Plotting3D
{
    public interface IBoundable3D
    {
        Cuboid Bounds
        {
            get;
        }
    }
    
    public struct Cuboid 
    {
        Point3D _minimum;
        Point3D _maximum;

        public Point3D Minimum => _minimum;

        public Point3D Maximum => _maximum;

        public Cuboid(Point3D minimum, Point3D maximum)
        {
            _minimum = minimum;
            _maximum = maximum;
        }

        public Cuboid(double xmin, double ymin, double zmin, double xmax, double ymax, double zmax)
        {
            _minimum = new Point3D(xmin, ymin, zmin);
            _maximum = new Point3D(xmax, ymax, zmax);
        }

        public bool IsPhysical
        {
            get
            {
                if (((_maximum.X - _minimum.X) != 0) && ((_maximum.Y - _minimum.Y) != 0) && ((_maximum.Z - _minimum.Z) != 0)) return true;
                return false;
            }
        }

        public static Cuboid Union(Cuboid bounds3D1, Cuboid bounds3D2)
        {
            if (!bounds3D1.IsPhysical) return bounds3D2;
            if (!bounds3D2.IsPhysical) return bounds3D1;
            var minimum1 = bounds3D1._minimum;
            var minimum2 = bounds3D2._minimum;
            var maximum1 = bounds3D1._maximum;
            var maximum2 = bounds3D2._maximum;
            return new Cuboid(Math.Min(minimum1.X, minimum2.X), Math.Min(minimum1.Y, minimum2.Y), Math.Min(minimum1.Z, minimum2.Z),
                Math.Max(maximum1.X, maximum2.X), Math.Max(maximum1.Y, maximum2.Y), Math.Max(maximum1.Z, maximum2.Z));
        }

        /// <summary>
        /// Provides transform that will map one set of bounds to another
        /// </summary>
        public static MatrixTransform3D BoundsMapping(Cuboid sourceBounds, Cuboid destinationBounds)
        {
            var graphMin = sourceBounds._minimum;
            var graphMax = sourceBounds._maximum;
            var modelMin = destinationBounds._minimum;
            var modelMax = destinationBounds._maximum;
            var scaleX = (modelMax.X - modelMin.X) / (graphMax.X - graphMin.X);
            var scaleY = (modelMax.Y - modelMin.Y) / (graphMax.Y - graphMin.Y);
            var scaleZ = (modelMax.Z - modelMin.Z) / (graphMax.Z - graphMin.Z);
            var offX = -graphMin.X * scaleX + modelMin.X;
            var offY = -graphMin.Y * scaleY + modelMin.Y;
            var offZ = -graphMin.Z * scaleZ + modelMin.Z;
            var transform = new Matrix3D(scaleX, 0, 0, 0, 0, scaleY, 0, 0,
                0, 0, scaleZ, 0, offX, offY, offZ, 1);
            var matrixTransform = new MatrixTransform3D(transform);
            return matrixTransform;
        }
    }
}
