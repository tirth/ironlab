using System;
using System.Collections.Generic;
using System.Linq;

namespace IronPlot
{
    /// <summary>
    /// Class to keep track of visible DirectImage classes
    /// </summary>
    public class DirectImageTracker
    {
        readonly List<DirectImage> _imageList = new List<DirectImage>();

        public void Register(DirectImage image)
        {
            if (_imageList.Contains(image)) throw new Exception("Multiple registration attempted."); 
            _imageList.Add(image);
        }

        public void Unregister(DirectImage image)
        {
            _imageList.Remove(image);
        }

        public void GetSizeForMembers(out int width, out int height)
        {
            if (_imageList.Count == 0) width = height = 0;
            else
            {
                width = _imageList.Max(t => t.ViewportWidth);
                height = _imageList.Max(t => t.ViewportHeight);
            }
        }
    }
}
