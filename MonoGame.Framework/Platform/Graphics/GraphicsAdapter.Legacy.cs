// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// NOTE: This is the legacy graphics adapter implementation
// which should no longer be updated.  All new development
// should go into the new one.

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Provides methods to retrieve and manipulate graphics adapters.
    /// </summary>
    public sealed class GraphicsAdapter : IDisposable
    {
        /// <summary>
        /// Defines the driver type for graphics adapter. Usable only on DirectX platforms for now.
        /// </summary>
        public enum DriverType
        {
            /// <summary>
            /// Hardware device been used for rendering. Maximum speed and performance.
            /// </summary>
            Hardware,
            /// <summary>
            /// Emulates the hardware device on CPU. Slowly, only for testing.
            /// </summary>
            Reference,
            /// <summary>
            /// Useful when <see cref="DriverType.Hardware"/> acceleration does not work.
            /// </summary>
            FastSoftware
        }

        private static ReadOnlyCollection<GraphicsAdapter> _adapters;

        static GraphicsAdapter()
        {
            int displayCount = Sdl.Display.GetNumVideoDisplays();
            var list = new GraphicsAdapter[displayCount];

            for (int i = 0; i < displayCount; i++)
                list[i] = new GraphicsAdapter(i);

            _adapters = new ReadOnlyCollection<GraphicsAdapter>(list);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the default adapter.
        /// </summary>
        public static GraphicsAdapter DefaultAdapter
        {
            get { return _adapters[0]; }
        }

        /// <summary>
        /// Gets a read-only collection of available adapters on the system.
        /// </summary>
        public static ReadOnlyCollection<GraphicsAdapter> Adapters
        {
            get  { return _adapters; }
        }

        /// <summary>
        /// Gets the GraphicsAdapter (Monitor) that the specified SDL window is currently sitting on.
        /// </summary>
        public static GraphicsAdapter GetWindowGraphicsAdapter(IntPtr windowHandle)
        {
            int displayIndex = Sdl.Window.GetDisplayIndex(windowHandle);

            if (displayIndex < 0)
                displayIndex = 0;

            return GraphicsAdapter.Adapters[displayIndex];
        }

        /// <summary>
        /// Used to request creation of the reference graphics device, 
        /// or the default hardware accelerated device (when set to false).
        /// </summary>
        /// <remarks>
        /// This only works on DirectX platforms where a reference graphics
        /// device is available and must be defined before the graphics device
        /// is created. It defaults to false.
        /// </remarks>
        public static bool UseReferenceDevice
        {
            get { return UseDriverType == DriverType.Reference; }
            set { UseDriverType = value ? DriverType.Reference : DriverType.Hardware; }
        }

        /// <summary>
        /// Used to request creation of a specific kind of driver.
        /// </summary>
        /// <remarks>
        /// These values only work on DirectX platforms and must be defined before the graphics device
        /// is created. <see cref="DriverType.Hardware"/> by default.
        /// </remarks>
        public static DriverType UseDriverType { get; set; }

        /// <summary>
        /// Gets a string used for presentation to the user.
        /// </summary>
        public string Description {
            get {
                try {
                    return MonoGame.OpenGL.GL.GetString(MonoGame.OpenGL.StringName.Renderer);
                } catch {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets a string used for presentation to the user.
        /// </summary>
        public string DisplayName {
            get
            {
                return Sdl.Display.GetDisplayName(_displayIndex);
            }
        }

        /// <summary>
        /// Gets the current display mode.
        /// </summary>
        public DisplayMode CurrentDisplayMode
        {
            get
            {
                return SupportedDisplayModes[_modeIndex];
            }
        }

        /// <summary>
        /// Gets a graphics adapter display index.
        /// </summary>
        public int DisplayIndex
        {
            get
            {
                return _displayIndex;
            }
        }


        /// <summary>
        /// Gets or sets the index of the graphics adapter display mode.
        /// </summary>
        public int DisplayModeIndex
        {
            get
            {
                return _modeIndex;
            }
            set
            {
                _modeIndex = value;   
            }
        }

        private DisplayModeCollection _supportedDisplayModes;
        private DisplayMode _currentDisplayMode;

        private int _displayIndex;
        private int _modeIndex;

        internal GraphicsAdapter(int displayIndex)
        {
            _displayIndex = displayIndex;

            var modes = new List<DisplayMode>();
            modes.Clear();

            var modeCount = Sdl.Display.GetNumDisplayModes(_displayIndex);

            for (int i = 0; i < modeCount; i++)
            {
                Sdl.Display.Mode mode;
                Sdl.Display.GetDisplayMode(_displayIndex, i, out mode);

                var displayMode = new DisplayMode(mode.Width, mode.Height, mode.RefreshRate, SurfaceFormat.Color);
                if (!modes.Contains(displayMode))
                    modes.Add(displayMode);
            }

            modes.Sort(delegate (DisplayMode a, DisplayMode b)
            {
                if (a == b) return 0;

                int result = a.Width.CompareTo(b.Width);
                if (result != 0) return result;

                result = a.Height.CompareTo(b.Height);
                if (result != 0) return result;

                result = a.RefreshRate.CompareTo(b.RefreshRate);
                if (result != 0) return result;

                return a.Format.CompareTo(b.Format);
            });
            modes.Reverse();
            _supportedDisplayModes = new DisplayModeCollection(modes);
        }
        
        /// <summary>
        /// Queries for support of the requested render target format on the adaptor.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile.</param>
        /// <param name="format">The requested surface format.</param>
        /// <param name="depthFormat">The requested depth stencil format.</param>
        /// <param name="multiSampleCount">The requested multisample count.</param>
        /// <param name="selectedFormat">Set to the best format supported by the adaptor for the requested surface format.</param>
        /// <param name="selectedDepthFormat">Set to the best format supported by the adaptor for the requested depth stencil format.</param>
        /// <param name="selectedMultiSampleCount">Set to the best count supported by the adaptor for the requested multisample count.</param>
        /// <returns>True if the requested format is supported by the adaptor. False if one or more of the values was changed.</returns>
		public bool QueryRenderTargetFormat(
			GraphicsProfile graphicsProfile,
			SurfaceFormat format,
			DepthFormat depthFormat,
			int multiSampleCount,
			out SurfaceFormat selectedFormat,
			out DepthFormat selectedDepthFormat,
			out int selectedMultiSampleCount)
		{
			selectedFormat = format;
            selectedDepthFormat = depthFormat;
            selectedMultiSampleCount = multiSampleCount;

            // fallback for unsupported renderTarget surface formats.
            if (selectedFormat == SurfaceFormat.Alpha8 ||
                selectedFormat == SurfaceFormat.NormalizedByte2 ||
                selectedFormat == SurfaceFormat.NormalizedByte4 ||
                selectedFormat == SurfaceFormat.Dxt1 ||
                selectedFormat == SurfaceFormat.Dxt3 ||
                selectedFormat == SurfaceFormat.Dxt5 ||
                selectedFormat == SurfaceFormat.Dxt1a ||
                selectedFormat == SurfaceFormat.Dxt1SRgb ||
                selectedFormat == SurfaceFormat.Dxt3SRgb ||
                selectedFormat == SurfaceFormat.Dxt5SRgb)
                selectedFormat = SurfaceFormat.Color;


            return (format == selectedFormat) && (depthFormat == selectedDepthFormat) && (multiSampleCount == selectedMultiSampleCount);
		}

        /// <summary>
        /// Gets a collection of supported display modes for the current adapter.
        /// </summary>
        public DisplayModeCollection SupportedDisplayModes
        {
            get
            {
                return _supportedDisplayModes;
            }
        }
        /// <summary>
        /// Returns true if the <see cref="GraphicsAdapter.CurrentDisplayMode"/> is widescreen.
        /// </summary>
        /// <remarks>
        /// Common widescreen modes include 16:9, 16:10 and 2:1.
        /// </remarks>
        public bool IsWideScreen
        {
            get
            {
                const float minWideScreenAspect = 16.0f / 10.0f;
                return CurrentDisplayMode.AspectRatio >= minWideScreenAspect;
            }
        }

        /// <summary>
        /// Returns a value that indicates whether the specified graphics profile is supported by the current adapter.
        /// </summary>
        /// <param name="graphicsProfile">The graphics profile to check for support.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// The <paramref name="graphicsProfile"/> parameter is not a valid <see cref="GraphicsProfile"/> enum value.
        /// </exception>
        public bool IsProfileSupported(GraphicsProfile graphicsProfile)
        {
            if(UseReferenceDevice)
                return true;

            switch(graphicsProfile)
            {
                case GraphicsProfile.Reach:
                    return true;
                case GraphicsProfile.HiDef:
                    bool result = true;
                    // TODO: check adapter capabilities...
                    return result;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
