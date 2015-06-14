// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows.Media.Media3D;
using SharpDX;

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot.Plotting3D
{   
    /// <summary>
    /// Geometric primitive class for surfaces from ILArrays.
    /// </summary>
    public partial class SurfaceModel3D : Model3D
    {
        /// <summary>
        /// The general case: smooth or faceted and any colouring scheme
        /// </summary>
        protected void UpdateVertsAndIndsGeneral(bool updateVerticesOnly, bool oneSided)
        {
            Vector3[] tempVertexPositions;
            Vector3[] tempVertexNormals;
            var index = 0;
            Point3D worldPoint;
            int numVertices;
            numVertices = 6 * (_lengthU - 1) * (_lengthV - 1);
            if (_vertices.Length != numVertices) _vertices = new VertexPositionNormalColor[numVertices];
            // First, obtain all vertices in World space 
            tempVertexPositions = new Vector3[_lengthU * _lengthV];
            tempVertexNormals = new Vector3[_lengthU * _lengthV];
            var modelToWorld = ModelToWorld;
            for (var v = 0; v < _lengthV; v++)
            {
                for (var u = 0; u < _lengthU; u++)
                {
                    worldPoint = modelToWorld.Transform(_modelVertices[index]);
                    tempVertexPositions[index] = new Vector3((float)worldPoint.X, (float)worldPoint.Y, (float)worldPoint.Z);
                    index++;
                }
            }
            // Next, go through all triangles, assigning vertices and indices
            // If shading is faceted then also assign normals. If smooth, then work out contribution to the 
            // shared normals and then assign           
            index = 0;
            var currentVertInd = 0;
            Vector3 point1, point2, point3, point4;
            Vector3 normal1, normal2, normal3, normal4;
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        point1 = tempVertexPositions[index];
                        point2 = tempVertexPositions[index + 1];
                        point3 = tempVertexPositions[index + _lengthU + 1];
                        point4 = tempVertexPositions[index + _lengthU];
                        normal1 = Vector3.Cross(point3 - point2, point1 - point2);
                        normal2 = Vector3.Cross(point1 - point4, point3 - point4);
                        normal1 += normal2;
                        //
                        tempVertexNormals[index] += normal1 + normal2;
                        tempVertexNormals[index + 1] += normal1;
                        tempVertexNormals[index + _lengthU + 1] += normal1 + normal2;
                        tempVertexNormals[index + _lengthU] += normal2;
                        // First triangle
                        _vertices[currentVertInd + 0].Position = point1;
                        _vertices[currentVertInd + 1].Position = point2;
                        _vertices[currentVertInd + 2].Position = point3;
                        // Second triangle
                        _vertices[currentVertInd + 3].Position = point3;
                        _vertices[currentVertInd + 4].Position = point4;
                        _vertices[currentVertInd + 5].Position = point1;
                        currentVertInd += 6;
                        index++;
                    }
                    index++;
                }
                index = 0;
                currentVertInd = 0;
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        normal1 = tempVertexNormals[index];
                        normal2 = tempVertexNormals[index + 1];
                        normal3 = tempVertexNormals[index + _lengthU + 1];
                        normal4 = tempVertexNormals[index + _lengthU];
                        _vertices[currentVertInd + 0].Normal = normal1;
                        _vertices[currentVertInd + 1].Normal = normal2;
                        _vertices[currentVertInd + 2].Normal = normal3;
                        // Second triangle
                        _vertices[currentVertInd + 3].Normal = normal3;
                        _vertices[currentVertInd + 4].Normal = normal4;
                        _vertices[currentVertInd + 5].Normal = normal1;
                        currentVertInd += 6;
                        index++;
                    }
                }
            }
            else
            {
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        point1 = tempVertexPositions[index];
                        point2 = tempVertexPositions[index + 1];
                        point3 = tempVertexPositions[index + _lengthU + 1];
                        point4 = tempVertexPositions[index + _lengthU];
                        normal1 = Vector3.Cross(point3 - point2, point1 - point2);
                        normal2 = Vector3.Cross(point1 - point4, point3 - point4);
                        normal1 += normal2;
                        // First triangle
                        _vertices[currentVertInd].Position = point1; _vertices[currentVertInd].Normal = normal1;
                        _vertices[currentVertInd + 1].Position = point2; _vertices[currentVertInd + 1].Normal = normal1;
                        _vertices[currentVertInd + 2].Position = point3; _vertices[currentVertInd + 2].Normal = normal1;
                        // Second triangle
                        _vertices[currentVertInd + 3].Position = point3; _vertices[currentVertInd + 3].Normal = normal1;
                        _vertices[currentVertInd + 4].Position = point4; _vertices[currentVertInd + 4].Normal = normal1;
                        _vertices[currentVertInd + 5].Position = point1; _vertices[currentVertInd + 5].Normal = normal1;
                        currentVertInd += 6;
                        index++;
                    }
                    index++;
                }
            }
            currentVertInd = 0;
            if (!updateVerticesOnly)
            {
                var currentInd = 0;
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        // First triangle
                        _indices[currentInd] = currentVertInd + 2;
                        _indices[currentInd + 1] = currentVertInd + 1;
                        _indices[currentInd + 2] = currentVertInd + 0;
                        // Second triangle
                        _indices[currentInd + 3] = currentVertInd + 5;
                        _indices[currentInd + 4] = currentVertInd + 4;
                        _indices[currentInd + 5] = currentVertInd + 3;
                        currentVertInd += 6;
                        currentInd += 6;
                    }
                }
                if (!oneSided)
                {
                    currentVertInd = 0;
                    for (var v = 0; v < _lengthV - 1; v++)
                    {
                        for (var u = 0; u < _lengthU - 1; u++)
                        {
                            // First triangle
                            _indices[currentInd] = currentVertInd + 0;
                            _indices[currentInd + 1] = currentVertInd + 1;
                            _indices[currentInd + 2] = currentVertInd + 2;
                            // Second triangle
                            _indices[currentInd + 3] = currentVertInd + 3;
                            _indices[currentInd + 4] = currentVertInd + 4;
                            _indices[currentInd + 5] = currentVertInd + 5;
                            currentVertInd += 6;
                            currentInd += 6;
                        }
                    }
                }
            }
        }

        protected void UpdateVertsAndIndsSmooth(bool updateVerticesOnly, bool oneSided)
        {
            //oneSided = true;
            var index = 0;
            var indexOff = _lengthU * _lengthV;
            Point3D worldPoint;
            var modelToWorld = ModelToWorld;
            for (var i = 0; i < indexOff; ++i)
            {
                worldPoint = modelToWorld.Transform(_modelVertices[i]);
                _vertices[i].Position = new Vector3((float)worldPoint.X, (float)worldPoint.Y, (float)worldPoint.Z);
                _vertices[i].Normal = new Vector3(0f, 0f, 0f);
            }
            // Add triangles
            var reverseSideOffset = 6 * (_lengthU - 1) * (_lengthV - 1);
            if (!updateVerticesOnly)
            {
                index = 0;
                indexOff = 0;
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        _indices[index] = indexOff + u;
                        _indices[index + 1] = indexOff + u + _lengthU + 1;
                        _indices[index + 2] = indexOff + u + 1;
                        _indices[index + 3] = indexOff + u;
                        _indices[index + 4] = indexOff + u + _lengthU;
                        _indices[index + 5] = indexOff + u + _lengthU + 1;
                        index += 6;
                    }
                    indexOff += _lengthU;
                }
                if (!oneSided)
                {
                    index = 0;
                    indexOff = _lengthU * _lengthV;
                    for (var v = 0; v < _lengthV - 1; v++)
                    {
                        for (var u = 0; u < _lengthU - 1; u++)
                        {
                            _indices[index + reverseSideOffset] = indexOff + u + 1;
                            _indices[index + 1 + reverseSideOffset] = indexOff + u + _lengthU + 1;
                            _indices[index + 2 + reverseSideOffset] = indexOff + u;
                            _indices[index + 3 + reverseSideOffset] = indexOff + u + _lengthU;
                            _indices[index + 4 + reverseSideOffset] = indexOff + u;
                            _indices[index + 5 + reverseSideOffset] = indexOff + u + _lengthU + 1;
                            index += 6;
                        }
                        indexOff += _lengthU;
                    }
                }
            }
            // Go through triangles and add normal to all vertices
            Vector3 normal;
            for (var i = 0; i <= reverseSideOffset - 3; i += 3)
            {
                Vector3 vec1, vec2;
                vec1 = _vertices[_indices[i + 2]].Position - _vertices[_indices[i + 1]].Position;
                vec2 = _vertices[_indices[i + 2]].Position - _vertices[_indices[i]].Position;
                normal = Vector3.Cross(vec1, vec2);
                normal.Normalize();
                _vertices[_indices[i]].Normal += normal;
                _vertices[_indices[i + 1]].Normal += normal;
                _vertices[_indices[i + 2]].Normal += normal;
            }
            if (!oneSided)
            {
                indexOff = _lengthU * _lengthV;
                for (var i = 0; i < indexOff; ++i)
                {
                    _vertices[i + indexOff].Position = _vertices[i].Position;
                    _vertices[i + indexOff].Normal = -_vertices[i].Normal;
                }
            }
        }

        protected void SetColorFromIndices()
        {
            lock (_updateLocker)
            {
                var surfaceShading = SurfaceShading.Smooth;
                byte opacity = 255;
                Dispatcher.Invoke(new Action(delegate
                {
                    surfaceShading = SurfaceShading;
                    opacity = (byte)(255 - (byte)GetValue(TransparencyProperty));
                }));
                SetColorFromIndices(surfaceShading, opacity);
            }
        }

        protected void SetColorFromIndices(SurfaceShading surfaceShading, byte opacity)
        {
            var cmap = ColourMap.ToIntArray();
            if (surfaceShading == SurfaceShading.Smooth)
            {
                var index = 0;
                var indexOff = ColourMapIndices.Length;
                foreach (var magnitude in ColourMapIndices)
                {
                    _vertices[index].Color = (opacity << 24) | cmap[magnitude];
                    _vertices[index + indexOff].Color = (opacity << 24) | cmap[magnitude];
                    index++;
                }
            }
            else
            {
                var currentVertInd = 0;
                UInt16 magnitude;
                var index = 0;
                int colour1, colour2, colour3, colour4;
                for (var v = 0; v < _lengthV - 1; v++)
                {
                    for (var u = 0; u < _lengthU - 1; u++)
                    {
                        var interpolateColourInFacets = true;

                        if (interpolateColourInFacets)
                        {
                            magnitude = ColourMapIndices[index];
                            colour1 = (opacity << 24) | cmap[magnitude];
                            magnitude = ColourMapIndices[index + 1];
                            colour2 = (opacity << 24) | cmap[magnitude];
                            magnitude = ColourMapIndices[index + _lengthU + 1];
                            colour3 = (opacity << 24) | cmap[magnitude];
                            magnitude = ColourMapIndices[index + _lengthU];
                            colour4 = (opacity << 24) | cmap[magnitude];
                            _vertices[currentVertInd + 0].Color = colour1;
                            _vertices[currentVertInd + 1].Color = colour2;
                            _vertices[currentVertInd + 2].Color = colour3;
                            _vertices[currentVertInd + 3].Color = colour3;
                            _vertices[currentVertInd + 4].Color = colour4;
                            _vertices[currentVertInd + 5].Color = colour1;
                            currentVertInd += 6;
                        }
                        index++;
                    }
                    index++;
                }
            }
        }
    }
}
