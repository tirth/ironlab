using System.Collections.Generic;
using System.Linq;

namespace IronPlot
{
    public class MathHelper
    {
        /// <summary>
        /// Iterates over X MeshGrid for row-major 2D array. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="nRows"></param>
        /// <returns></returns>
        public static IEnumerable<double> MeshGridX(IEnumerable<double> x, int yLength)
        {
            // Row major:
            for (var j = 0; j < yLength; ++j)
            {
                foreach (var value in x)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Iterates over Y MeshGrid for row-major 2D array. 
        /// </summary>
        /// <param name="y"></param>
        /// <param name="nColumns"></param>
        /// <returns></returns>
        public static IEnumerable<double> MeshGridY(IEnumerable<double> y, int xLength)
        {
            // Row major:
            foreach (var value in y)
            {
                for (var i = 0; i < xLength; ++i)
                {
                    yield return value;
                }
            }
        }

        public static double[] Counter(int n)
        {
            var output = new double[n];
            for (var i = 0; i < n; ++i)
            {
                output[i] = i + 1;
            }
            return output;
        }

        public static double[,] Counter(int width, int height)
        {
            var output = new double[width, height];
            double index = 1;
            for (var i = 0; i < width; ++i)
            {
                for (var j = 0; j < height; ++j)
                {
                    output[i, j] = index;
                    index += 1;
                }
            }
            return output;
        }

        public void Example()
        {
            var evens = Enumerable
                .Range(1, 100)
                .Where(x => (x % 2) == 0)
                .ToList();
        }

        public class Slice2D
        {
            int? _start0;
            int? _stop0;
            int? _step0;
            int? _start1;
            int? _stop1;
            int? _step1;

            public Slice2D(int? start0, int? stop0, int? step0, int? start1, int? stop1, int? step1)
            {
                _start0 = start0; _stop0 = stop0; _step0 = step0;
                _start1 = start1; _stop1 = stop1; _step1 = step1;
            }
        }
    }
}
