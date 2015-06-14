using System;
using System.Collections;
using System.Collections.Generic;

namespace IronPlot.PlotCommon
{
    public class SliceTest
    {
        public static void Test()
        {
            var array = new double[10,10];
            var slice = new Slice2D<double>(array, new DimensionSlice(2, 2, 6), new DimensionSlice(0,1,4));
            slice.Set(u => u + 4);
        }
    }
    
    public class Slice2D<T> : IEnumerable<T>
    {
        DimensionSlice _x;
        DimensionSlice _y;
        T[,] _array;
        int _xLength;
        int _yLength;

        public Slice2D(T[,] array, DimensionSlice x, DimensionSlice y)
        {
            CommonConstructor(array, x, y);
        }

        public Slice2D(T[,] array)
        {
            CommonConstructor(array, DimensionSlice.CreateBasic(array.GetLength(0)), DimensionSlice.CreateBasic(array.GetLength(0)));
        }

        public void CommonConstructor(T[,] array, DimensionSlice x, DimensionSlice y)
        {
            _x = x; _y = y;
            _array = array;
            if (_x.Stop == null) _x.Stop = array.GetLength(0);
            if (_y.Stop == null) _y.Stop = array.GetLength(1);
            _xLength = ((int)x.Stop - x.Start) / x.Step;
            _yLength = ((int)y.Stop - y.Start) / y.Step;
        }

        public T[,] Array => _array;

        /// <summary>
        /// Indexing: likely to be slower than using Update method.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _array[index % _xLength, index / _xLength]; }
            set { _array[index % _xLength, index / _xLength] = value; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var xIndex in _x)
            {
                foreach (var yIndex in _y)
                {
                    yield return _array[xIndex, yIndex];
                }
            }
        }


        public IEnumerator<T> GetEnumerator()
        {
            foreach (var xIndex in _x)
            {
                foreach (var yIndex in _y)
                {
                    yield return _array[xIndex, yIndex];
                }
            }
        }

        public void Set(Func<T, T> function)
        {
            var xIndex = _x.Start;
            var yIndex = _y.Start;
            while (true)
            {
                _array[xIndex, yIndex] = function(_array[xIndex, yIndex]);
                xIndex += _x.Step;
                if (xIndex > _x.Stop)
                {
                    xIndex = _x.Start;
                    yIndex += _y.Step;
                    if (yIndex > _y.Stop) break;
                }
            }
        }

        public void Set(IEnumerable<T> input1, Func<T, T, T> function)
        {
            var xIndex = _x.Start;
            var yIndex = _y.Start;
            foreach (var item in input1)
            {
                _array[xIndex, yIndex] = function(_array[xIndex, yIndex], item);
                xIndex += _x.Step;
                if (xIndex > _x.Stop) 
                {
                    xIndex = _x.Start;
                    yIndex += _y.Step;
                    if (yIndex > _y.Stop) break;
                }
            }
        }

        public void Set(IEnumerable<T> input1, IEnumerable<T> input2, Func<T, T, T, T> function)
        {
            var xIndex = _x.Start;
            var yIndex = _y.Start;
            var item1 = input1.GetEnumerator();
            var item2 = input2.GetEnumerator();
            while (item1.MoveNext() && item2.MoveNext()) 
            {
                _array[xIndex, yIndex] = function(_array[xIndex, yIndex], item1.Current, item2.Current);
                xIndex += _x.Step;
                if (xIndex > _x.Stop)
                {
                    xIndex = _x.Start;
                    yIndex += _y.Step;
                    if (yIndex > _y.Stop) break;
                }
            }
        }
    }

    public class DimensionSlice : IEnumerable<int>
    {
        public int Start;
        public int Step;
        public int? Stop;

        public DimensionSlice(int start, int step, int? stop)
        {
            Start = start;
            Step = step;
            Stop = stop;
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (var i = Start; i < Stop; i += Step) yield return i;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (var i = Start; i < Stop; i += Step) yield return i;
        }

        internal int this[int index] => Start + index * Step;

        public static DimensionSlice CreateBasic(int length)
        {
            return new DimensionSlice(0, 1, length - 1);
        }
    }
}
