// Copyright (c) 2010 Joe Moorhouse

using System.Windows;
using SharpDX;

namespace IronPlot.ManagedD3D
{
    public static class Extensions
    {
        public static Vector3 ToVector3(this Vector4 vector4)
        {
            return new Vector3(vector4.X, vector4.Y, vector4.Z);
        }
    }
}

namespace IronPlot
{
    public static class Extensions
    {
        public static Vector ToVector(this Point point)
        {
            return new Vector(point.X, point.Y);
        }
    }

    public struct Line
    {
        public Vector PointOnLine;
        public Vector Direction;

        public Line(Vector pointOnLine, Vector direction)
        {
            PointOnLine = pointOnLine;
            Direction = direction;
        }

        public Vector GetNormalDirection()
        {
            return new Vector(Direction.Y, -Direction.X);
        }

        public Vector IntersectionWithLine(Line line)
        {
            var a = Direction;
            var b = line.Direction;
            var c = line.PointOnLine - PointOnLine;
            var lamdba = Vector.CrossProduct(c, b) / Vector.CrossProduct(a, b);
            return PointOnLine + lamdba * Direction;
        }
    }

    public static class Geomery
    {
        public static double PointToLineDistance(Vector point, Line line)
        {
            var normal = line.GetNormalDirection();
            normal.Normalize();
            return Vector.Multiply(normal, line.PointOnLine - point);
        }
    }
}
