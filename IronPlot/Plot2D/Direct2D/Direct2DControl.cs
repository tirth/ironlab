// Copyright (c) 2010 Joe Moorhouse

using System.Collections.Generic;

namespace IronPlot
{
    public class Direct2DControl : DirectControl
    {
        public List<DirectPath> Paths => (DirectImage as Direct2DImage).Paths;

        public void AddPath(DirectPath path)
        {
            (DirectImage as Direct2DImage).Paths.Add(path);
            path.DirectImage = DirectImage;
            path.RecreateDisposables();
        }

        public void RemovePath(DirectPath path)
        {
            (DirectImage as Direct2DImage).Paths.Remove(path);
            path.Dispose();
        }

        protected override void CreateDirectImage()
        {
            DirectImage = new Direct2DImage();
        }

        protected override void OnVisibleChanged_Visible()
        {
            foreach (var path in Paths) path.RecreateDisposables();
        }

        protected override void OnVisibleChanged_NotVisible()
        {
            foreach (var path in Paths) path.DisposeDisposables();
        }
    }
}

