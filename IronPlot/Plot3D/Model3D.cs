// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using SharpDX.Direct3D9;

namespace IronPlot.Plotting3D
{
    internal interface I3DDrawable
    {
        void Initialize();
        /// <summary>
        /// Render the primitive using the GraphicsDevice and the Effect (if any).
        /// </summary>
        void Draw();
    }

    public delegate void OnDrawEventHandler(object sender, EventArgs e);
    //public delegate void OnRequestRenderEventHandler(object sender, EventArgs e);

    public class Model3D : DependencyObject, IViewportImage, IBoundable3D
    {
        internal ViewportImage ViewportImage;
        protected Device graphicsDevice;
        protected I2DLayer layer2D;
        protected Cuboid bounds;

        #region TreeStructure

        public static readonly DependencyProperty ChildrenProperty =
            DependencyProperty.Register("Children",
            typeof(Model3DCollection), typeof(Model3D),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible",
            typeof(bool), typeof(Model3D),
            new PropertyMetadata(true, OnUpdateIsVisibleProperty));

        protected static void OnUpdateIsVisibleProperty(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Model3D)obj).RequestRender(EventArgs.Empty);
        }

        public Model3DCollection Children
        {
            get { return (Model3DCollection)GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        internal void RecursiveSetViewportImage(ViewportImage viewportImage)
        {
            if (viewportImage == null)
            {
                DisposeDisposables();
                ViewportImage = null;
                graphicsDevice = null;
                layer2D = null;
                Children.ViewportImage = null;
            }
            else
            {                
                graphicsDevice = viewportImage.GraphicsDevice;
                layer2D = viewportImage.Layer2D;
                OnViewportImageChanged(viewportImage);
                BindToViewportImage();
                Children.ViewportImage = viewportImage;
                RecreateDisposables();
            }
            foreach (var model in Children)
            {
                model.RecursiveSetViewportImage(viewportImage);
            }
            // TODO Set target on all children, checking for maximum depth or
            // circular dependencies
        }

        internal void RecursiveSetResolution(int dpi)
        {
            if (this is IResolutionDependent) (this as IResolutionDependent).SetResolution(dpi);
            foreach (var model in Children)
            {
                model.RecursiveSetResolution(dpi);
            }
        }

        internal void RecursiveDisposeDisposables()
        {
            DisposeDisposables();
            foreach (var model in Children)
            {
                model.RecursiveDisposeDisposables();
            }
        }

        internal void RecursiveRecreateDisposables()
        {
            RecreateDisposables();
            foreach (var model in Children)
            {
                model.RecursiveRecreateDisposables();
            }
        }

        internal void BindToViewportImage()
        {
            var bindingTransform = new Binding("ModelToWorld");
            bindingTransform.Source = ViewportImage;
            bindingTransform.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(this, ModelToWorldProperty, bindingTransform);
            RenderRequested += ViewportImage.RequestRender;
        }

        internal void RemoveBindToViewportImage()
        {
            if (ViewportImage != null)
            {
                BindingOperations.ClearBinding(this, ModelToWorldProperty);
                RenderRequested -= ViewportImage.RequestRender;
            }
        }
        #endregion

        public I2DLayer Layer2D => layer2D;

        public Cuboid Bounds => bounds;

        public Device GraphicsDevice => graphicsDevice;

        protected bool GeometryChanged = true;

        static object _drawLock = new object();

        public event OnDrawEventHandler OnDraw;

        public event EventHandler RenderRequested;

        // Invoke the OnDraw event
        protected virtual void RaiseOnDrawEvent(EventArgs e)
        {
            if (OnDraw != null)
                OnDraw(this, e);
        }

        // Invoke the OnRequestRender event
        public virtual void RequestRender(EventArgs e)
        {
            if (RenderRequested != null)
                RenderRequested(this, e);
        }

        public static readonly DependencyProperty ModelToWorldProperty =
            DependencyProperty.Register("ModelToWorld",
            typeof(MatrixTransform3D), typeof(Model3D),
            new PropertyMetadata((MatrixTransform3D)Transform3D.Identity,
                OnModelToWorldChanged));

        public MatrixTransform3D ModelToWorld
        {
            get { return (MatrixTransform3D)GetValue(ModelToWorldProperty); }
            set { SetValue(ModelToWorldProperty, value); }
        }

        protected static void OnModelToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Model3D)obj).OnModelToWorldChanged();
        }

        protected virtual void OnModelToWorldChanged()
        {
            GeometryChanged = true;
            RequestRender(EventArgs.Empty);
        }

        public Model3D()
        {
            SetValue(ChildrenProperty, new Model3DCollection(this));
        }

        //public Model3D(Device graphicsDevice)
        //{
        //    SetValue(ChildrenProperty, new Model3DCollection(this));
        //    Initialize();
        //}

        internal virtual void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            RemoveBindToViewportImage();
            ViewportImage = newViewportImage;
            graphicsDevice = (ViewportImage == null) ? null : ViewportImage.GraphicsDevice;
            layer2D = (ViewportImage == null) ? null : ViewportImage.Layer2D;
            BindToViewportImage();
            Children.ViewportImage = (ViewportImage == null) ? null : ViewportImage;
            GeometryChanged = false;
        }

        /// <summary>
        /// Update geometry (vertices and indices) 
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            // Do nothing in base
        }

        /// <summary>
        /// Draw the model in GraphicsDevice
        /// </summary>
        public virtual void Draw()
        {
            if (GeometryChanged)
            {
                GeometryChanged = false;
                UpdateGeometry();
            }
            foreach (var child in Children)
            {
                if (child.IsVisible) child.Draw();
            }
            RaiseOnDrawEvent(EventArgs.Empty);
        }

        /// <summary>
        /// Dispose any disposable (wholly owned) members.
        /// </summary>
        protected virtual void DisposeDisposables()
        {
            // No disposables in base.
        }

        /// <summary>
        /// Reinstate any disposable (wholly owned) members.
        /// </summary>
        protected virtual void RecreateDisposables()
        {
            // No disposables in base.
        }
    }
}

