// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.ObjectModel;

namespace IronPlot.Plotting3D
{
    public class ItemEventArgs : EventArgs
    {
        public Model3D Model3D;

        public ItemEventArgs(Model3D model3D)
        {
            Model3D = model3D;
        }
    }
    
    public class Model3DCollection : Collection<Model3D>
    {   
        const int MaximumLevels = 10; 
        // The ViewportImage class responsible for rendering the Model3Ds in the collection.
        // There is one and only one.
        internal ViewportImage ViewportImage;
        // The owner can be the ViewportImage
        object _owner;

        public delegate void ItemEventHandler(object sender, ItemEventArgs e);

        // Events
        public event ItemEventHandler Changed;

        protected virtual void OnChanged(ItemEventArgs e)
        {
            if (Changed != null)
                Changed(this, e);
        }

        public Model3DCollection(object owner)
        {
            _owner = owner;
            if (owner is ViewportImage) ViewportImage = (owner as ViewportImage);
            else if (owner is Model3D) ViewportImage = (owner as Model3D).ViewportImage;
            else ViewportImage = null;
        }

        protected override void InsertItem(int index, Model3D newItem)
        {
            base.InsertItem(index, newItem);
            newItem.RecursiveSetViewportImage(ViewportImage);
            OnChanged(new ItemEventArgs(newItem));
        }

        protected override void SetItem(int index, Model3D newItem)
        {
            base.SetItem(index, newItem);
            newItem.RecursiveSetViewportImage(null);
            newItem.RecursiveSetViewportImage(ViewportImage);
            OnChanged(new ItemEventArgs(newItem));
        }

        protected override void RemoveItem(int index)
        {
            this[index].RecursiveSetViewportImage(null);
            base.RemoveItem(index);
            OnChanged(new ItemEventArgs(null));
        }

        internal void SetModelResolution(int dpi)
        {
            foreach (var model in this)
            {
                model.RecursiveSetResolution(dpi);
            }
        }
    }
}
