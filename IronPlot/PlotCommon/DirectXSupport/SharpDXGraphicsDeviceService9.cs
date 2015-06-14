// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D9;

namespace IronPlot
{
    // Summary:
    //     Defines a mechanism for retrieving GraphicsDevice objects. Reference page
    //     contains links to related code samples.
    public interface ISharpDxGraphicsDeviceService
    {
        // Summary:
        //     Retrieves a graphcs device.
        Device GraphicsDevice { get; }

        // Summary:
        //     The event that occurs when a graphics device is created.
        event EventHandler DeviceCreated;
        //
        // Summary:
        //     The event that occurs when a graphics device is disposing.
        event EventHandler DeviceDisposing;
        //
        // Summary:
        //     The event that occurs when a graphics device is reset.
        event EventHandler DeviceReset;
        //
        // Summary:
        //     The event that occurs when a graphics device is in the process of resetting.
        event EventHandler DeviceResetting;
    }
    
    public enum DirectXStatus
    {
        Available,
        UnavailableRemoteSession,
        UnavailableLowTier,
        UnavailableMissingDirectX,
        UnavailableUnknown
    };

    public class Direct3Dx9NotFoundException : Exception { }

    // The IGraphicsDeviceService interface requires a DeviceCreated event, but we
    // always just create the device inside our constructor, so we have no place to
    // raise that event. The C# compiler warns us that the event is never used, but
    // we don't care so we just disable this warning.
#pragma warning disable 67

    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    public class SharpDxGraphicsDeviceService9 : ISharpDxGraphicsDeviceService
    {
        #region Fields
        // device settings
        readonly Format _adapterFormat = Format.X8R8G8B8;
        readonly Format _backbufferFormat = Format.A8R8G8B8; // SurfaceFormat.Color XNA
        readonly Format _depthStencilFormat = Format.D16; // DepthFormat.Depth24 XNA
        CreateFlags _createFlags = CreateFlags.Multithreaded | CreateFlags.FpuPreserve;
        private PresentParameters _presentParameters;

        // Singleton device service instance.
        static SharpDxGraphicsDeviceService9 _singletonInstance;

        // Keep track of how many controls are sharing the singletonInstance.
        static int _referenceCount;

        private Direct3D _direct3D;
        private Direct3DEx _direct3DEx;
        private Device _device;
        private DeviceEx _deviceEx;


        #endregion

        internal DirectImageTracker Tracker = new DirectImageTracker();

        public DirectXStatus DirectXStatus
        {
            get;
            private set;
        }

        public bool UseDeviceEx
        {
            get;
            private set;
        }

        public bool IsAntialiased
        {
            get;
            private set;
        }

        public Direct3D Direct3D
        {
            get
            {
                if (UseDeviceEx)
                    return _direct3DEx;
                return _direct3D;
            }
        }

        /// <summary>
        /// Gets the current graphics device.
        /// </summary>
        public Device GraphicsDevice
        {
            get
            {
                if (UseDeviceEx)
                    return _deviceEx;
                return _device;
            }
        }

        /// <summary>
        /// Gets the current presentation parameters.
        /// </summary>
        public PresentParameters PresentParameters => _presentParameters;

        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        SharpDxGraphicsDeviceService9(int width, int height)
        {
            InitializeDirect3D();
            InitializeDevice(width, height);
            if (DirectXStatus != DirectXStatus.Available)
            {
                ReleaseDevice();
                ReleaseDirect3D();
                throw new Exception("Direct3D device unavailable.");
            }
        }

        /// <summary>
        /// Initializes the Direct3D objects and sets the Available flag
        /// </summary>
        private void InitializeDirect3D()
        {
            DirectXStatus = DirectXStatus.UnavailableUnknown;

            ReleaseDevice();
            ReleaseDirect3D();

            // assume that we can't run at all under terminal services
            //if (GetSystemMetrics(SM_REMOTESESSION) != 0)
            //{
            //    DirectXStatus = DirectXStatus.Unavailable_RemoteSession;
            //    return;
            //}

            //int renderingTier = (RenderCapability.Tier >> 16);
            //if (renderingTier < 2)
            //{
                //DirectXStatus = DirectXStatus.Unavailable_LowTier;
                //return;
            //}

#if USE_XP_MODE
         _direct3D = new Direct3D();
         UseDeviceEx = false;
#else
            try
            {
                _direct3DEx = new Direct3DEx();
                UseDeviceEx = true;
            }
            catch
            {
                try
                {
                    _direct3D = new Direct3D();
                    UseDeviceEx = false;
                }
                catch (Direct3Dx9NotFoundException) 
                {
                    DirectXStatus = DirectXStatus.UnavailableMissingDirectX;
                    return;
                }
                catch
                {
                    DirectXStatus = DirectXStatus.UnavailableUnknown;
                    return;
                }
            }
#endif

            bool ok;
            Result result;

            ok = Direct3D.CheckDeviceType(0, DeviceType.Hardware, _adapterFormat, _backbufferFormat, true, out result);
            if (!ok)
            {
                //const int D3DERR_NOTAVAILABLE = -2005530518;
                //if (result.Code == D3DERR_NOTAVAILABLE)
                //{
                //   ReleaseDirect3D();
                //   Available = Status.Unavailable_NotReady;
                //   return;
                //}
                ReleaseDirect3D();
                return;
            }

            ok = Direct3D.CheckDepthStencilMatch(0, DeviceType.Hardware, _adapterFormat, _backbufferFormat, _depthStencilFormat, out result);
            if (!ok)
            {
                ReleaseDirect3D();
                return;
            }

            var deviceCaps = Direct3D.GetDeviceCaps(0, DeviceType.Hardware);
            if ((deviceCaps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
                _createFlags |= CreateFlags.HardwareVertexProcessing;
            else
                _createFlags |= CreateFlags.SoftwareVertexProcessing;

            DirectXStatus = DirectXStatus.Available;
        }

        /// <summary>
        /// Initializes the Device
        /// </summary>
        private void InitializeDevice(int width, int height)
        {
            if (DirectXStatus != DirectXStatus.Available)
                return;

            Debug.Assert(Direct3D != null);

            ReleaseDevice();

            var windowHandle = (new Form()).Handle;
            //HwndSource hwnd = new HwndSource(0, 0, 0, 0, 0, width, height, "SharpDXControl", IntPtr.Zero);

            _presentParameters = new PresentParameters();
            if (UseDeviceEx) _presentParameters.SwapEffect = SwapEffect.Discard;
            else _presentParameters.SwapEffect = SwapEffect.Copy;
            
            _presentParameters.DeviceWindowHandle = windowHandle;
            _presentParameters.Windowed = true;
            _presentParameters.BackBufferWidth = Math.Max(width, 1);
            _presentParameters.BackBufferHeight = Math.Max(height, 1);
            _presentParameters.BackBufferFormat = _backbufferFormat;
            _presentParameters.AutoDepthStencilFormat = _depthStencilFormat;
            _presentParameters.EnableAutoDepthStencil = true;
            _presentParameters.PresentationInterval = PresentInterval.Immediate;
            _presentParameters.MultiSampleType = MultisampleType.None;
            IsAntialiased = false;
            int qualityLevels;
            if (Direct3D.CheckDeviceMultisampleType(0, DeviceType.Hardware, _backbufferFormat, true, MultisampleType.EightSamples, out qualityLevels))
            {
                _presentParameters.MultiSampleType = MultisampleType.EightSamples;
                _presentParameters.MultiSampleQuality = qualityLevels - 1;
                IsAntialiased = true;
            }
            else if (Direct3D.CheckDeviceMultisampleType(0, DeviceType.Hardware, _backbufferFormat, true, MultisampleType.FourSamples, out qualityLevels))
            {
                _presentParameters.MultiSampleType = MultisampleType.FourSamples;
                _presentParameters.MultiSampleQuality = qualityLevels - 1;
                IsAntialiased = false;
            }


            try
            {
                if (UseDeviceEx)
                {
                    _deviceEx = new DeviceEx((Direct3DEx)Direct3D, 0,
                       DeviceType.Hardware,
                       windowHandle,
                       _createFlags,
                       _presentParameters);
                }
                else
                {
                    _device = new Device(Direct3D, 0,
                       DeviceType.Hardware,
                       windowHandle,
                       _createFlags,
                       _presentParameters);
                }
            }
            catch (Exception) //Direct3D9Exception
            {
                DirectXStatus = DirectXStatus.UnavailableUnknown;
            }
        }

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static SharpDxGraphicsDeviceService9 AddRef(int width, int height)
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref _referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                _singletonInstance = new SharpDxGraphicsDeviceService9(width, height);
            }

            return _singletonInstance;
        }

        /// <summary>
        /// Create a new GraphicsDeviceService and return reference to it
        /// </summary>
        public static SharpDxGraphicsDeviceService9 RefToNew(int width, int height)
        {
            return new SharpDxGraphicsDeviceService9(width, height);
        }

        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                if (disposing)
                {
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);
                    //graphicsDevice.Dispose();
                    ReleaseDevice();
                }

                //graphicsDevice = null;
            }
        }

        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its clients.
        /// </summary>
        public void RatchetResetDevice(int width, int height)
        {
            ResetDevice(Math.Max(_presentParameters.BackBufferWidth, width), Math.Max(_presentParameters.BackBufferHeight, height));
        }

        public bool ResetIfNecessary()
        {
            int newWidth, newHeight;
            Tracker.GetSizeForMembers(out newWidth, out newHeight);
            var currentWidth = _presentParameters.BackBufferWidth;
            var currentHeight = _presentParameters.BackBufferHeight;
            var resetRequired = false;
            if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset) resetRequired = true;
            if (newWidth > currentWidth)
            {
                newWidth = Math.Max((int)(currentWidth * 1.1), newWidth);
                resetRequired = true;
            }
            else if (newWidth < currentWidth * 0.9)
            {
                resetRequired = true;
            }
            if (newHeight > currentHeight)
            {
                newHeight = Math.Max((int)(currentHeight * 1.1), newHeight);
                resetRequired = true;
            }
            else if (newHeight < currentHeight * 0.9)
            {
                resetRequired = true;
            }
            if (resetRequired) ResetDevice(newWidth, newHeight);
            return resetRequired;
        }

        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);

            _presentParameters.BackBufferWidth = width;
            _presentParameters.BackBufferHeight = height;

            if (UseDeviceEx) (GraphicsDevice as DeviceEx).ResetEx(ref _presentParameters);
            else GraphicsDevice.Reset(_presentParameters);

            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }

        public event EventHandler DeviceCreated;
        public event EventHandler DeviceDisposing;
        public event EventHandler DeviceReset;
        public event EventHandler DeviceResetting;
        public event EventHandler RecreateBuffers;

        private void ReleaseDevice()
        {
            if (_device != null)
            {
                if (!_device.IsDisposed)
                {
                    _device.Dispose();
                    _device = null;
                }
            }

            if (_deviceEx != null)
            {
                if (!_deviceEx.IsDisposed)
                {
                    _deviceEx.Dispose();
                    _device = null;
                }
            }

            //OnDeviceDisposing(EventArgs.Empty);
        }

        private void ReleaseDirect3D()
        {
            if (_direct3D != null)
            {
                if (!_direct3D.IsDisposed)
                {
                    _direct3D.Dispose();
                    _direct3D = null;
                }
            }

            if (_direct3DEx != null)
            {
                if (!_direct3DEx.IsDisposed)
                {
                    _direct3DEx.Dispose();
                    _direct3DEx = null;
                }
            }
        }

        #region DLL imports
        // can't figure out how to access remote session status through .NET
        [DllImport("user32")]
        private static extern int GetSystemMetrics(int smIndex);
        private const int SmRemotesession = 0x1000;
        #endregion
    }
}

