// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{
    public enum ColourMapMode { Rgb, Hsv }

    public enum ColourMapType { Jet, Hsv, Gray }

    public enum ColorBarOrientation { Horizontal, Vertical };

    public class ColourMap
    {
        private readonly int _colorMapLength = 64;
        private byte[,] _colourMap;
        private int[] _intColourMap;
        private readonly object _locker = new object();
        private double[] _interpPoints;
        private Curve _red, _blue, _green;
        private Curve _hue, _saturation, _value;
        private ColourMapMode _colourMapMode;
        private readonly int _alphaValue = 255;
        private ColourMapType _colourMapType;
    
        public ColourMap(ColourMapType colourMapType, int length)
        {
            _colourMapType = colourMapType;
            _colorMapLength = length;
            switch (colourMapType)
            {
                case ColourMapType.Jet:
                    Jet();
                    break;
                case ColourMapType.Hsv:
                    Hsv();
                    break;
                case ColourMapType.Gray:
                    Gray();
                    break;
                default:
                    Jet();
                    break;
            }
            UpdateColourMap();
        }

        public ColourMap(int length)
        {
            _colourMapType = ColourMapType.Jet;
            _colorMapLength = length;
            Jet();
            UpdateColourMap();
        }

        public int Length => _colorMapLength;

        public double[] InterpolationPoints => _interpPoints;

        public byte[,] ToByteArray()
        {
            return _colourMap;
        }

        public int[] ToIntArray()
        {
            lock (_locker)
            {
                return _intColourMap;
            }
        }

        // If either the colourMapType or the colorMapLength or the interpPoints
        // have changed, then recalculate the colourMap
        public void UpdateColourMap()
        {
            lock (_locker)
            {
                var pixelPositions = MathHelper.Counter(_colorMapLength);
                for (var i = 0; i < pixelPositions.Length; ++i) pixelPositions[i] = (pixelPositions[i] - 0.5) / _colorMapLength;
                switch (_colourMapMode)
                {
                    case ColourMapMode.Rgb:
                        _red.UpdateLinearSplineCoefficients();
                        _green.UpdateLinearSplineCoefficients();
                        _blue.UpdateLinearSplineCoefficients();
                        var interpolatedRed = _red.GetValuesLinear(pixelPositions);
                        interpolatedRed.MultiplyBy(255.0);
                        var interpolatedGreen = _green.GetValuesLinear(pixelPositions);
                        interpolatedGreen.MultiplyBy(255.0);
                        var interpolatedBlue = _blue.GetValuesLinear(pixelPositions);
                        interpolatedBlue.MultiplyBy(255.0);
                        _colourMap = new byte[_colorMapLength, 4];
                        _intColourMap = new int[_colorMapLength];
                        for (var i = 0; i < _colorMapLength; i++)
                        {
                            _colourMap[i, 0] = (byte)_alphaValue;
                            _colourMap[i, 1] = (byte)interpolatedRed[i];
                            _colourMap[i, 2] = (byte)interpolatedGreen[i];
                            _colourMap[i, 3] = (byte)interpolatedBlue[i];
                            _intColourMap[i] = ((int)interpolatedRed[i] << 16) // R
                                    | ((int)interpolatedGreen[i] << 8)     // G
                                    | ((int)interpolatedBlue[i] << 0);     // B
                        }
                        break;
                    case ColourMapMode.Hsv:
                        _hue.UpdateLinearSplineCoefficients();
                        _saturation.UpdateLinearSplineCoefficients();
                        _value.UpdateLinearSplineCoefficients();
                        var interpolatedHue = _hue.GetValuesLinear(pixelPositions);
                        var interpolatedSaturation = _saturation.GetValuesLinear(pixelPositions);
                        var interpolatedValue = _value.GetValuesLinear(pixelPositions);
                        _colourMap = new byte[_colorMapLength, 4];
                        _intColourMap = new int[_colorMapLength];
                        double r = 0, g = 0, b = 0;
                        for (var i = 0; i < _colorMapLength; i++)
                        {
                            var hi = (int)(Math.Floor(interpolatedHue[i] * 6));
                            var f = interpolatedHue[i] * 6 - hi;
                            var v = interpolatedSaturation[i];
                            var s = interpolatedValue[i];
                            var p = v * (1 - s);
                            var q = v * (1 - f * s);
                            var t = v * (1 - (1 - f) * s);
                            switch (hi)
                            {
                                case 0:
                                    r = v; g = t; b = p; break;
                                case 1:
                                    r = q; g = v; b = p; break;
                                case 2:
                                    r = p; g = v; b = t; break;
                                case 3:
                                    r = p; g = q; b = v; break;
                                case 4:
                                    r = t; g = p; b = v; break;
                                case 5:
                                    r = v; g = p; b = q; break;
                            }
                            r *= 255; g *= 255; b *= 255;
                            _colourMap[i, 0] = (byte)_alphaValue;
                            _colourMap[i, 1] = (byte)r;
                            _colourMap[i, 2] = (byte)g;
                            _colourMap[i, 3] = (byte)b;
                            _intColourMap[i] = ((int)r << 16) // H
                                    | ((int)g << 8)     // S
                                    | ((int)b << 0);     // V
                        }
                        break;
                }
            }
        }

        public int[,] Spring()
        {
            var cmap = new int[_colorMapLength, 4];
            var spring = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                spring[i] = 1.0f * i / (_colorMapLength - 1);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = 255;
                cmap[i, 2] = (int)(255 * spring[i]);
                cmap[i, 3] = 255 - cmap[i, 1];
            }
            return cmap;
        }

        public int[,] Summer()
        {
            var cmap = new int[_colorMapLength, 4];
            var summer = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                summer[i] = 1.0f * i / (_colorMapLength - 1);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = (int)(255 * summer[i]);
                cmap[i, 2] = (int)(255 * 0.5f * (1 + summer[i]));
                cmap[i, 3] = (int)(255 * 0.4f);
            }
            return cmap;
        }

        public int[,] Autumn()
        {
            var cmap = new int[_colorMapLength, 4];
            var autumn = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                autumn[i] = 1.0f * i / (_colorMapLength - 1);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = 255;
                cmap[i, 2] = (int)(255 * autumn[i]);
                cmap[i, 3] = 0;
            }
            return cmap;
        }

        public int[,] Winter()
        {
            var cmap = new int[_colorMapLength, 4];
            var winter = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                winter[i] = 1.0f * i / (_colorMapLength - 1);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = 0;
                cmap[i, 2] = (int)(255 * winter[i]);
                cmap[i, 3] = (int)(255 * (1.0f - 0.5f * winter[i]));
            }
            return cmap;
        }

        public void Gray()
        {
            _colourMapMode = ColourMapMode.Rgb;
            _interpPoints = new[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 };
            _red = new Curve(_interpPoints, new[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
            _green = new Curve(_interpPoints, new[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
            _blue = new Curve(_interpPoints, new[] { 0.0, 0.125, 0.375, 0.625, 0.875, 1.0 });
        }

        public void Jet()
        {
            // RGB format used
            // Assume that pixel color is colour at pixel centre
            // 0 Dark blue (0,0,0.5) to blue (0,0,1) 1/8
            // 1 Blue (0,0,1) to cyan (0,1,1) 2/8
            // 2 Cyan (0,1,1) to yellow (1,1,0) 2/8
            // 3 Yellow (1,1,0) to red (1,0,0) 2/8
            // 4 Red (1,0,0) to dark red (0.5,0,0) 1/8
            // Dark Blue, blue, cyan, yellow, red, dark red
            _colourMapMode = ColourMapMode.Rgb;
            _interpPoints = new[] { 0.0, 0.01, 0.125, 0.375, 0.625, 0.875, 0.99, 1.0 };
            _red = new Curve(_interpPoints, new[] { 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.5, 0.5 });
            _green = new Curve(_interpPoints, new[] { 0.0, 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, 0.0 });
            _blue = new Curve(_interpPoints, new[] { 0.5, 0.5, 1.0, 1.0, 0.0, 0.0, 0.0, 0.0 });
        }

        public void Hsv()
        {
            _colourMapMode = ColourMapMode.Hsv;
            _interpPoints = new[] { 0.0, 0.25, 0.5, 0.75, 1.0 };
            _hue = new Curve(_interpPoints, new[] { 0.0, 0.25, 0.5, 0.75, 1.0 });
            _saturation = new Curve(_interpPoints, new[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
            _value = new Curve(_interpPoints, new[] { 1.0, 1.0, 1.0, 1.0, 1.0 });
        }

        public int[,] Hot()
        {
            var cmap = new int[_colorMapLength, 4];
            var n = 3 * _colorMapLength / 8;
            var red = new float[_colorMapLength];
            var green = new float[_colorMapLength];
            var blue = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                if (i < n)
                    red[i] = 1.0f * (i + 1) / n;
                else
                    red[i] = 1.0f;
                if (i < n)
                    green[i] = 0f;
                else if (i >= n && i < 2 * n)
                    green[i] = 1.0f * (i + 1 - n) / n;
                else
                    green[i] = 1f;
                if (i < 2 * n)
                    blue[i] = 0f;
                else
                    blue[i] = 1.0f * (i + 1 - 2 * n) / (_colorMapLength - 2 * n);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = (int)(255 * red[i]);
                cmap[i, 2] = (int)(255 * green[i]);
                cmap[i, 3] = (int)(255 * blue[i]);
            }
            return cmap;
        }

        public int[,] Cool()
        {
            var cmap = new int[_colorMapLength, 4];
            var cool = new float[_colorMapLength];
            for (var i = 0; i < _colorMapLength; i++)
            {
                cool[i] = 1.0f * i / (_colorMapLength - 1);
                cmap[i, 0] = _alphaValue;
                cmap[i, 1] = (int)(255 * cool[i]);
                cmap[i, 2] = (int)(255 * (1 - cool[i]));
                cmap[i, 3] = 255;
            }
            return cmap;
        }

        public BitmapSource ToBitmapSource(ColorBarOrientation colorBarOrientation)
        {
            // Define parameters used to create the BitmapSource.
            int width, height;
            if (colorBarOrientation == ColorBarOrientation.Vertical)
            {
                width = 1;
                height = _colorMapLength;
            }
            else 
            {
                width = _colorMapLength;
                height = 1;
            }
            var pf = PixelFormats.Bgr32;
            var bytes = (pf.BitsPerPixel + 7) / 8;
            var rawStride = (width * bytes);
            var rawImage = new byte[rawStride * height];
            var cmap = ToByteArray();
            var index = 0;
            for (var c = _colorMapLength - 1; c > 0; --c)
            {
                rawImage[index] = cmap[c, 3];
                rawImage[index + 1] = cmap[c, 2];
                rawImage[index + 2] = cmap[c, 1];
                rawImage[index + 3] = cmap[c, 0];
                index += bytes;
            }
            // Create a BitmapSource.
            var bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        public void UpdateWriteableBitmap(WriteableBitmap bitmap)
        {
            bitmap.Lock();
            unsafe
            {
                var backBuffer = bitmap.BackBuffer;
                var pBackBuffer = (int*)backBuffer;
                var pf = PixelFormats.Bgr32;
                var bytes = (pf.BitsPerPixel + 7) / 8;
                var cmap = ToIntArray();
                for (var c = _colorMapLength - 1; c > 0; --c)
                {
                    *pBackBuffer = cmap[c];
                    pBackBuffer++;
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0,0, bitmap.PixelWidth, bitmap.PixelHeight));
            bitmap.Unlock();
        }
    }
}
