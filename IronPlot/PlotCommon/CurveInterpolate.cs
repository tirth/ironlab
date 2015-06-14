// Copyright (c) 2010 Joe Moorhouse

using System;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{
    /// <summary>Boundary types for Curve interpolation</summary>
    /// <remarks><para></para>
    /// </remarks>
    public enum BoundaryType { Parabolic, FirstDerivativeSpecified, SecondDerivativeSpecified }

    public partial class Curve
    {
        /// <summary>Sort a curve ascending in preparation for interpolation</summary>
        /// <remarks><para></para>
        /// </remarks>
        public void Sort()
        {
            Array.Sort(x, y);
        }
        
        // Calculation coefficients once, interpolate multiple times
        // Different sets for different interpolation types to allow simultaneous use
        protected double[] LinearSplineCoefficients;
        protected double[] CubicSplineCoefficients;
        protected double[] MonotoneCubicSplineCoefficients;
        protected double[] HermiteSplineCoefficients = null;
        
        /// <summary>Calculates interpolated values using Linear Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesLinear(double[] xi)
        {
            if (LinearSplineCoefficients == null) UpdateLinearSplineCoefficients();
            
            var interpolatedValues = new double[xi.Length];
            for (var i = 0; i < xi.Length; ++i)
            {
                var xit = xi[i];
                var p = 0; var r = N - 1; var q = 0;       
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 2;
                interpolatedValues[i] = LinearSplineCoefficients[q] + xit * LinearSplineCoefficients[q + 1];
            }
            return interpolatedValues;
        }

        /// <summary>
        /// Return the lower index of the two indices of x that xi lies between.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xi"></param>
        /// <returns></returns>
        public static int GetInterpolatedIndex(double[] x, double xi)
        {
            if (xi < x[0]) return 0;
            if (xi >= x[x.Length - 1]) return x.Length - 1;
            var p = 0; var r = x.Length - 1; var q = 0;
            while (p != r - 1)
            {
                q = (p + r) / 2;
                if (x[q] >= xi) r = q;
                else p = q;
            }
            return p;
        }

        /// <summary>Calculates interpolated values using default cubic interpolation which is Monotone Piecewise Cubic Hermite Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubic(double[] xv)
        {
            return GetValuesMonotoneCubicSpline(xv);
        }

        /// <summary>Calculates interpolated values using Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated Y values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesSpline(double[] xv)
        {
            return GetValuesCubicSpline(xv);
        }

        /// <summary>Calculates interpolated values using 'natural' Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubicSpline(double[] xv)
        {
            return GetValuesCubicSpline(xv, BoundaryType.SecondDerivativeSpecified, 0.0,
            BoundaryType.SecondDerivativeSpecified, 0.0);
        }

        /// <summary>Calculates interpolated values using Cubic Spline Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <param name="leftBoundaryType">BoundaryType enumeration (Parabolic, FirstDerivative Specified, SecondDerivative Specified)</param>
        /// <param name="leftBoundaryTypeParameter">Parameter required (ignored if Parabolic)</param>
        /// <param name="rightBoundaryType">BoundaryType enumeration (Parabolic, FirstDerivative Specified, SecondDerivative Specified)</param>
        /// <param name="rightBoundaryTypeParameter">Parameter required (ignored if Parabolic)</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesCubicSpline(double[] xi, BoundaryType leftBoundaryType, double leftBoundaryTypeParameter,
            BoundaryType rightBoundaryType, double rightBoundaryTypeParameter)
        {
            if (CubicSplineCoefficients == null) UpdateCubicSplineCoefficients(leftBoundaryType, leftBoundaryTypeParameter, rightBoundaryType, rightBoundaryTypeParameter);
            
            var interpolatedValues = new double[xi.Length];
            for (var i = 0; i < xi.Length; ++i)
            {
                var xit = xi[i];
                var p = 0; var r = N - 1; var q = 0;
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 4;
                interpolatedValues[i] = CubicSplineCoefficients[q] + xit * (CubicSplineCoefficients[q + 1]
                    + xit * (CubicSplineCoefficients[q + 2] + xit * CubicSplineCoefficients[q + 3]));
            }
            return interpolatedValues;
        }

        /// <summary>Calculates interpolated values using Monotone Piecewise Cubic Hermite Interpolation</summary>
        /// <param name="x">X values at which interpolated values are required</param>
        /// <returns>Interpolated values </returns>
        /// <remarks><para></para>
        /// </remarks>
        public double[] GetValuesMonotoneCubicSpline(double[] xi)
        {
            if (MonotoneCubicSplineCoefficients == null)  UpdateMonotoneCubicSplineCoefficients();

            var interpolatedValues = new double[xi.Length];
            for (var i = 0; i < xi.Length; ++i)
            {
                var xit = xi[i];
                var p = 0; var r = N - 1; var q = 0;
                while (p != r - 1)
                {
                    q = (p + r) / 2;
                    if (x[q] >= xit) r = q;
                    else p = q;
                }
                xit = xit - x[p];
                q = p * 4;
                interpolatedValues[i] = MonotoneCubicSplineCoefficients[q] + xit * (MonotoneCubicSplineCoefficients[q + 1]
                    + xit * (MonotoneCubicSplineCoefficients[q + 2] + xit * MonotoneCubicSplineCoefficients[q + 3]));
            }
            return interpolatedValues;
        }

        /// <summary>Update or create coefficients for linear spline</summary>
        public void UpdateLinearSplineCoefficients()
        {
            LinearSplineCoefficients = new double[2 * N];
            for (var i = 0; i <= N - 2; i++)
            {
                LinearSplineCoefficients[2 * i + 0] = y[i];
                LinearSplineCoefficients[2 * i + 1] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
            }
        }

        /// <summary>Update or create coefficients for cubic spline</summary>
        public void UpdateCubicSplineCoefficients(BoundaryType leftBoundaryType, double leftBoundaryTypeParameter,
            BoundaryType rightBoundaryType, double rightBoundaryTypeParameter)
        {
            // TODO Raise error if < 2 points
            // Sort if points are unsorted?

            var a1 = new double[N];
            var a2 = new double[N];
            var a3 = new double[N];
            var b = new double[N];
            var deriv = new double[N];

            // If 2 points, apply parabolic end conditions
            if (N == 2)
            {
                leftBoundaryType = BoundaryType.Parabolic;
                rightBoundaryType = BoundaryType.Parabolic;
            }

            #region LeftBoundary
            if (leftBoundaryType == BoundaryType.Parabolic)
            {
                a1[0] = 0;
                a2[0] = 1;
                a3[0] = 1;
                b[0] = 2 * (y[1] - y[0]) / (x[1] - x[0]);
            }
            if (leftBoundaryType == BoundaryType.FirstDerivativeSpecified)
            {
                a1[0] = 0;
                a2[0] = 1;
                a3[0] = 0;
                b[0] = leftBoundaryTypeParameter;
            }
            if (leftBoundaryType == BoundaryType.SecondDerivativeSpecified)
            {
                a1[0] = 0;
                a2[0] = 2;
                a3[0] = 1;
                b[0] = 3 * (y[1] - y[0]) / (x[1] - x[0]) - 0.5 * leftBoundaryTypeParameter * (x[1] - x[0]);
            }
            #endregion

            for (var i = 1; i <= N - 2; i++)
            {
                a1[i] = x[i + 1] - x[i];
                a2[i] = 2 * (x[i + 1] - x[i - 1]);
                a3[i] = x[i] - x[i - 1];
                b[i] = 3 * ((y[i] - y[i - 1]) / a3[i]) * a1[i]
                    + 3 * ((y[i + 1] - y[i]) / a1[i]) * a3[i];
            }

            #region RightBoundary
            if (rightBoundaryType == BoundaryType.Parabolic)
            {
                a1[N - 1] = 1;
                a2[N - 1] = 1;
                a3[N - 1] = 0;
                b[N - 1] = 2 * (y[N - 1] - y[N - 2]) / (x[N - 1] - x[N - 2]);
            }
            if (rightBoundaryType == BoundaryType.FirstDerivativeSpecified)
            {
                a1[N - 1] = 0;
                a2[N - 1] = 1;
                a3[N - 1] = 0;
                b[N - 1] = rightBoundaryTypeParameter;
            }
            if (rightBoundaryType == BoundaryType.SecondDerivativeSpecified)
            {
                a1[N - 1] = 1;
                a2[N - 1] = 2;
                a3[N - 1] = 0;
                b[N - 1] = 3 * (y[N - 1] - y[N - 2]) / (x[N - 1] - x[N - 2]) 
                    + 0.5 * rightBoundaryTypeParameter * (x[N - 1] - x[N - 2]);
            }
            #endregion

            double temp = 0;
            
            a1[0] = 0;
            a3[N - 1] = 0;
            for (var i = 1; i <= N - 1; i++)
            {
                temp = a1[i] / a2[i - 1];
                a2[i] = a2[i] - temp * a3[i - 1];
                b[i] = b[i] - temp * b[i - 1];
            }
            deriv[N - 1] = b[N - 1] / a2[N - 1];
            for (var i = N - 2; i >= 0; i--)
            {
                deriv[i] = (b[i] - a3[i] * deriv[i + 1]) / a2[i];
            }

            CubicSplineCoefficients = GetHermiteSplineCoefficients(deriv);
        }

        protected double[] GetHermiteSplineCoefficients(double[] deriv)
        {
            double delta = 0;
            double delta2 = 0;
            double delta3 = 0;
            var coeffs = new double[(N - 1) * 4];
            for (var i = 0; i <= N - 2; i++)
            {
                delta = x[i + 1] - x[i];
                delta2 = delta * delta;
                delta3 = delta * delta2;
                coeffs[4 * i + 0] = y[i];
                coeffs[4 * i + 1] = deriv[i];
                coeffs[4 * i + 2] = (3 * (y[i + 1] - y[i]) - 2 * deriv[i] * delta - deriv[i + 1] * delta) / delta2;
                coeffs[4 * i + 3] = (2 * (y[i] - y[i + 1]) + deriv[i] * delta + deriv[i + 1] * delta) / delta3;
            }
            return coeffs;
        }


        public void UpdateMonotoneCubicSplineCoefficients()
        {
            var a1 = new double[N]; // secant
            var a2 = new double[N]; // derivative
            a1[0] = (y[1] - y[0]) / (x[1] - x[0]);
            a2[0] = a1[0];
            for (var i = 1; i < N - 3; i++)
            {
                a1[i] = (y[i + 1] - y[i]) / (x[i + 1] - x[i]);
                a2[i] = (a1[i - 1] + a1[i]) / 2.0;
            }
            a1[N - 2] = (y[N-1] - y[N-2]) / (x[N-1] - x[N-2]);
            a2[N - 2] = (a1[N - 3] + a1[N - 2]) / 2.0;
            a2[N - 1] = a1[N - 2];
            double alpha, beta, dist, tau;
            for (var i = 0; i < N - 2; i++)
            {
                alpha = a2[i] / a1[i];
                beta = a2[i + 1] / a1[i];
                dist = alpha * alpha + beta * beta;
                if (dist > 9.0)
                {
                    tau = 3.0 / Math.Sqrt(dist);
                    a2[i] = tau * alpha * a1[i];
                    a2[i + 1] = tau * beta * a1[i];
                }
            }
            MonotoneCubicSplineCoefficients = GetHermiteSplineCoefficients(a2);
        }
    }
}
