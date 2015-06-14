using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace IronPlot
{
    /// <summary>
    /// Class to deal with common non-System.Array arrays, e.g. NumpPy arrays.
    /// </summary>
    public class GeneralArray
    {
        public static int[] GetDimensions(object generalArray)
        {
            int[] dimensions = null;
            var arrayType = generalArray.GetType();
            var testArray = generalArray as Array;
            if (testArray != null) 
            {
                var array = testArray;
                dimensions = new int[array.Rank];
                for (var i = 0; i < array.Rank; ++i) dimensions[i] = array.GetLength(i);
            }
            else if (arrayType.Name == "ndarray")
            {
                dynamic dynamicArray = generalArray;
                dimensions = new int[dynamicArray.Dims.Length];
                for (var i = 0; i < dimensions.Length; ++i) dimensions[i] = (int)dynamicArray.Dims[i];
            }
            // for jagged lists or enumerables
            else if (generalArray is IEnumerable<object>)
            {
                dimensions = DimensionsOfJaggedIEnumerable(generalArray as IEnumerable<object>);
            }
            return dimensions;
        }

        private static int[] DimensionsOfJaggedIEnumerable(IEnumerable<object> enumerable)
        {
            var rank = 0;
            var parent = enumerable;
            var dimensionList = new List<int>();
            while (parent != null)
            {
                rank++;
                var count = parent.FastCount();
                dimensionList.Add(count);
                parent = count == 0 ? null : parent.First() as IEnumerable<object>;
            }
            return dimensionList.ToArray();
        }
        
        /// <summary>
        /// Converts object to a
        /// </summary>
        /// <param name="generalArray"></param>
        /// <returns></returns>
        public static Array ToDoubleArray(object generalArray)
        {
            var arrayType = generalArray.GetType();
            Array managedArray = null;

            #region ILArray
            switch (arrayType.Name)
            {
                case "ILArray`1":
                    dynamic dynamicArray = generalArray;

                    if ((dynamicArray.Dimensions.NumberOfDimensions == 1) ||
                        ((dynamicArray.Dimensions.NumberOfDimensions == 2) && (dynamicArray.Dimensions[1] == 1)))
                    {
                        int n = dynamicArray.Dimensions[0];
                        managedArray = new double[n];
                        var managedArrayCast = managedArray as double[];
                        for (var i = 0; i < n; ++i) managedArrayCast[i] = dynamicArray.GetValue(i);
                        return managedArray;
                    }
                    if (dynamicArray.Dimensions.NumberOfDimensions == 2)
                    {
                        int n1 = dynamicArray.Dimensions[0];
                        int n2 = dynamicArray.Dimensions[1];
                        managedArray = new double[n1, n2];
                        var managedArrayCast = managedArray as double[,];
                        for (var i = 0; i < n1; ++i)
                        {
                            for (var j = 0; j < n2; ++j)
                            {
                                managedArrayCast[i, j] = dynamicArray.GetValue(i, j);
                            }
                        }
                        return managedArray;
                    }
                    throw new Exception("Array must be one or two dimensionsal.");
                case "ndarray":
                    managedArray = ManagedArrayFromNumpyArray(generalArray);
                    break;
                default:
                    // not Array, ILArray or Numpy array
                    var dimensions = GetDimensions(generalArray);
                    if (dimensions.Length > 2) throw new Exception("More than two dimensions found.");
                    if (dimensions.Length == 1)
                    {
                        var enumerable = generalArray as IEnumerable<object>;
                        var tempArray = new double[enumerable.Count()];
                        var index = 0;
                        foreach (var item in enumerable)
                        {
                            tempArray[index] = Convert.ToDouble(item);
                            index++;
                        }
                        managedArray = tempArray;
                    }
                    else
                    {
                        var parent = generalArray as IEnumerable<object>;
                        var minLength = int.MaxValue; var maxLength = int.MinValue;
                        var count = parent.FastCount();
                        foreach (IEnumerable<object> child in parent)
                        {
                            if (child == null) minLength = 0;
                            else
                            {
                                minLength = Math.Min(minLength, child.FastCount());
                                maxLength = Math.Max(maxLength, child.FastCount());
                            }
                        }
                        var i = 0;
                        var tempArray = new double[count, maxLength];
                        foreach (IEnumerable<object> child in parent)
                        {
                            var j = 0;
                            foreach (var element in child)
                            {
                                tempArray[i, j] = Convert.ToDouble(element);
                                j++;
                            }
                            while (j < maxLength)
                            {
                                tempArray[i, j] = Double.NaN; ++j;
                            }
                            i++;
                        }
                        managedArray = tempArray;
                    }
                    break;
            }
            #endregion

            return managedArray;
        }

        public unsafe static Array ManagedArrayFromNumpyArray(object numpyArray)
        {
            Array managedArray;
            dynamic dynamicArray = numpyArray;
            dynamic newArray = null;
            var dimensions = new int[dynamicArray.Dims.Length];
            var length = 1;
            for (var i = 0; i < dimensions.Length; ++i)
            {
                dimensions[i] = (int)dynamicArray.Dims[i];
                length *= dimensions[i];
            }

            // Treat double precision data as special case and keep everything fast.
            if (dynamicArray.dtype.name == "float64")
            {
                try
                {
                    // If necessary get a contiguous, C-type double precision array:
                    if (!dynamicArray.flags.contiguous)
                    {
                        newArray = dynamicArray.copy("C");
                    }
                }
                catch (Exception)
                {
                    throw new Exception("Failed to change input array into contiguous array.");
                }
                IntPtr start;
                if (newArray == null) start = dynamicArray.UnsafeAddress;
                else start = newArray.UnsafeAddress;

                if (dimensions.Length == 1)
                {
                    managedArray = new double[dimensions[0]];
                    fixed (double* newArrayPointer = (double[])managedArray)
                    {
                        var currentPosition = newArrayPointer;
                        var numpyPointer = (double*)start;
                        var endPointer = newArrayPointer + length;
                        while (currentPosition != endPointer)
                        {
                            *currentPosition = *numpyPointer;
                            currentPosition++;
                            numpyPointer++;
                        }
                    }
                }
                else if (dimensions.Length == 2)
                {
                    managedArray = new double[dimensions[0], dimensions[1]];
                    fixed (double* newArrayPointer = (double[,])managedArray)
                    {
                        var currentPosition = newArrayPointer;
                        var numpyPointer = (double*)start;
                        var endPointer = newArrayPointer + length;
                        while (currentPosition != endPointer)
                        {
                            *currentPosition = *numpyPointer;
                            currentPosition++;
                            numpyPointer++;
                        }
                    }
                }
                else
                {
                    throw new Exception("Array must be one or two dimensionsal.");
                }
                if (newArray != null) newArray.Dispose();
            }
            else
            {
                managedArray = Array.CreateInstance(typeof(double), dimensions);
                var enumerator = new IndexEnumerator(dimensions);
                while (enumerator.MoveNext())
                {
                    managedArray.SetValue(dynamicArray.item(enumerator.CurrentObjectIndices), enumerator.CurrentIndices);
                }
            }
            return managedArray;
        }

        /// <summary>
        /// For fast enumerations, this must be a IList of ILists, not a IList of IEnumerables
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static bool IsIListOfILists(IEnumerable<object> enumerable)
        {
            if (!(enumerable is IList)) return false; 
            var parent = enumerable as IList<object>;
            var dimensionList = new List<int>();
            for (var i = 0; i < parent.Count; ++i)
            {
                if (!(parent[i] is IList<object>)) return false; 
            }
            return true;
        }

        /// <summary>
        /// Create an enumerator suitable for constructing images (i.e. travels along the first dimension,
        /// then the second). If not 2D, first element is null.
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static IEnumerable<double> ToImageEnumerator(IEnumerable<object> enumerable, out int xLength, out int yLength)
        {
            var dimensions = GetDimensions(enumerable);
            xLength = dimensions[0];
            yLength = 0;
            if (dimensions.Length > 2) return null;

            // If this is a list of lists, then create enumerator, otherwise convert to a rectangular array first.
            // This gives better performance if not indexable or if this is a Numpy array.

            var arrayType = enumerable.GetType();
            if (dimensions.Length == 2 && arrayType.Name != "ndarray" && IsIListOfILists(enumerable))
            {
                var dimensionList = new List<int>();
                var parent = enumerable as IList;
                var minLength = int.MaxValue; var maxLength = int.MinValue;
                var count = parent.FastCount();

                for (var i = 0; i < count; ++i)
                {
                    var child = parent[i] as IList;
                    if (child == null) minLength = 0;
                    else
                    {
                        minLength = Math.Min(minLength, child.Count);
                        maxLength = Math.Max(maxLength, child.Count);
                    }
                }
                yLength = maxLength;
                if (minLength == 0) return null;
                return Jagged2DImageEnumerable(parent, minLength, maxLength);
            }
            if (dimensions.Length == 1)
            {
                return Enumerable1D(enumerable);
            }
            var array = (double[,])ToDoubleArray(enumerable);
            yLength = dimensions[1];
            return array.ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
        }

        private static IEnumerable<double> Enumerable1D(IEnumerable<object> enumerable)
        {
            foreach (var item in enumerable) yield return Convert.ToDouble(item);
        }

        private static IEnumerable<double> Jagged2DImageEnumerable(IList array, int minLength, int maxLength)
        {
            var parent = array;
            var count = parent.Count;
            for (var j = 0; j < maxLength; ++j)
            {
                for (var i = 0; i < count; ++i)
                {
                    var child = parent[i] as IList;
                    if (j > child.Count - 1) yield return Double.NaN;
                    else yield return Convert.ToDouble((parent[i] as IList)[j]);
                }
            }
        }
    }

    public class IndexEnumerator
    {
        public int[] CurrentIndices;

        public object[] CurrentObjectIndices;

        public int[] Dimensions;

        public IndexEnumerator(int[] dimensions)
        {
            Dimensions = dimensions;
            CurrentIndices = new int[dimensions.Length];
            CurrentObjectIndices = new object[dimensions.Length];
            for (var i = 0; i < dimensions.Length; ++i) CurrentObjectIndices[i] = 0;
            CurrentIndices[0] = -1;
        }

        public bool MoveNext()
        {
            var index = 0;
            CurrentIndices[index]++;
            CurrentObjectIndices[index] = CurrentIndices[index];
            while (CurrentIndices[index] == Dimensions[index])
            {
                if (index == (Dimensions.Length - 1)) return false;
                CurrentIndices[index] = 0;
                CurrentObjectIndices[index] = 0;
                index++;
                CurrentIndices[index]++;
                CurrentObjectIndices[index] = CurrentIndices[index];
            }
            return true;
        }
    }
}
