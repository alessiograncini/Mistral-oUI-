// Copyright (c) 2024 Vuplex Inc. All rights reserved.
//
// Licensed under the Vuplex Commercial Software Library License, you may
// not use this file except in compliance with the License. You may obtain
// a copy of the License at
//
//     https://vuplex.com/commercial-library-license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    public abstract class BaseWebViewPrefab : MonoBehaviour {

        /// <summary>
        /// Indicates that the prefab was clicked. Note that the prefab automatically
        /// calls IWebView.Click() for you.
        /// </summary>
        /// <remarks>
        /// This event is not supported when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        /// <example>
        /// <code>
        /// webViewPrefab.Clicked += (sender, eventArgs) => {
        ///     Debug.Log("WebViewPrefab was clicked at point: " + eventArgs.Point);
        /// };
        /// </code>
        /// </example>
        public virtual event EventHandler<ClickedEventArgs> Clicked;

        /// <summary>
        /// Indicates that the prefab finished initializing,
        /// so its WebView property is ready to use.
        /// </summary>
        /// <seealso cref="WaitUntilInitialized"/>
        public event EventHandler Initialized;

        /// <summary>
        /// Indicates that the pointer (e.g. mouse cursor) entered the prefab.
        /// </summary>
        public event EventHandler PointerEntered;

        /// <summary>
        /// Indicates that the pointer (e.g. mouse cursor) exited the prefab.
        /// </summary>
        public event EventHandler PointerExited;

        /// <summary>
        /// Indicates that the prefab was scrolled. Note that the prefab automatically
        /// calls IWebView.Scroll() for you.
        /// </summary>
        /// <remarks>
        /// This event is not supported when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        /// <example>
        /// webViewPrefab.Scrolled += (sender, eventArgs) => {
        ///     Debug.Log($"WebViewPrefab was scrolled. Point: {eventArgs.Point}, scroll delta: {eventArgs.ScrollDelta}");
        /// };
        /// </example>
        public virtual event EventHandler<ScrolledEventArgs> Scrolled;

        /// <summary>
        /// Determines whether clicking is enabled. The default is `true`.
        /// </summary>
        /// <remarks>
        /// This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        public bool ClickingEnabled = true;

        /// <summary>
        /// Determines whether the mouse cursor icon is automatically updated based on interaction with
        /// the web page. For example, hovering over a link causes the mouse cursor icon to turn into a pointer hand.
        /// The default is `true`. CursorIconsEnabled is currently only supported by 3D WebView for Windows and macOS.
        /// </summary>
        /// <seealso cref="IWithCursorType"/>
        [Label("Cursor Icons Enabled (Windows and macOS only)")]
        [Tooltip("(Windows and macOS only) Sets whether the mouse cursor icon is automatically updated based on interaction with the web page. For example, hovering over a link causes the mouse cursor icon to turn into a pointer hand.")]
        public bool CursorIconsEnabled = true;

        /// <summary>
        /// Determines how the prefab handles drag interactions.
        /// </summary>
        /// <remarks>
        /// Important notes:
        /// <list type="bullet">
        ///   <item>This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).</item>
        ///   <item>
        ///     For information on the limitations of drag interactions on iOS and UWP, please see
        ///     [this article](https://support.vuplex.com/articles/hover-and-drag-limitations).
        ///   </item>
        ///   <item>
        ///     The Android Gecko package doesn't support the HTML Drag and Drop API (GeckoView limitation).
        ///   </item>
        /// </list>
        /// </remarks>
        /// <seealso href="https://support.vuplex.com/articles/dragging-scrollbar">When I drag a scrollbar, why does it scroll the wrong way?</seealso>
        [Tooltip("Determines how the prefab handles drag interactions. Note that This property is ignored when running in Native 2D Mode.")]
        public DragMode DragMode = DragMode.DragToScroll;

        /// <summary>
        /// Determines the threshold (in web pixels) for triggering a drag. The default is `20`.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item>
        ///     When the prefab's DragMode is set to DragToScroll, this property determines
        ///     the distance that the pointer must drag before it's no longer
        ///     considered a click.
        ///   </item>
        ///   <item>
        ///     When the prefab's DragMode is set to DragWithinPage, this property determines
        ///     the distance that the pointer must drag before it triggers
        ///     a drag within the page.
        ///   </item>
        ///   <item>This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).</item>
        /// </list>
        /// </remarks>
        [Label("Drag Threshold (px)")]
        [Tooltip("Determines the threshold (in web pixels) for triggering a drag.")]
        public float DragThreshold = 20;

        /// <summary>
        /// Determines whether hover interactions are enabled. The default is `true`.
        /// </summary>
        /// <remarks>
        /// Important notes:
        /// <list type="bullet">
        ///   <item>This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).</item>
        ///   <item>
        ///     For information on the limitations of hovering on iOS and UWP, please see
        ///     [this article](https://support.vuplex.com/articles/hover-and-drag-limitations).
        ///   </item>
        /// </list>
        /// </remarks>
        public bool HoveringEnabled = true;

        /// <summary>
        /// If you drag the prefab into the scene via the editor,
        /// you can set this property to make it so that the instance
        /// automatically loads the given URL after it initializes. To load a new URL
        /// at runtime, use IWebView.LoadUrl() instead.
        /// </summary>
        /// <seealso href="https://support.vuplex.com/articles/how-to-load-local-files">How to load local files</seealso>
        [Label("Initial URL (optional)")]
        [Tooltip("You can set this to the URL that you want to load, or you can leave it blank if you'd rather add a script to load content programmatically with IWebView.LoadUrl() or LoadHtml().")]
        [HideInInspector]
        public string InitialUrl;

        /// <summary>
        /// Determines whether the webview automatically receives keyboard input from the native keyboard and the Keyboard prefab. The default is `true`.
        /// </summary>
        /// <seealso cref="NativeOnScreenKeyboardEnabled">NativeOnScreenKeyboardEnabled</seealso>
        /// <seealso href="https://support.vuplex.com/articles/keyboard">How does keyboard input work?</seealso>
        [Tooltip("Determines whether the webview automatically receives keyboard input from the native keyboard and the Keyboard prefab.")]
        public bool KeyboardEnabled = true;

        /// <summary>
        /// Determines whether JavaScript console messages from IWebView.ConsoleMessageLogged
        /// are printed to the Unity logs. The default is `false`.
        /// </summary>
        [Tooltip("Determines whether JavaScript console messages are printed to the Unity logs.")]
        public bool LogConsoleMessages = false;

        /// <summary>
        /// Gets or sets prefab's material.
        /// </summary>
        /// <remarks>
        /// This property is unused when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        public Material Material {
            get { return _view.Material; }
            set { _view.Material = value; }
        }

        /// <summary>
        /// Sets the webview's pixel density, which is its number of physical pixels per logical pixel.
        /// The default value is `1`, but increasing it to `2` can make web content appear sharper
        /// or less blurry on high DPI displays. PixelDensity is currently only supported by
        /// 3D WebView for Windows and macOS.
        /// </summary>
        /// <example>
        /// <code>
        /// // Increase the pixel density to 2 for high DPI screens.
        /// webViewPrefab.PixelDensity = 2;
        /// </code>
        /// </example>
        /// <seealso cref="IWithPixelDensity"/>
        [Label("Pixel Density (Windows and macOS only)")]
        [Tooltip("(Windows and macOS only) Sets the webview's pixel density.")]
        public float PixelDensity = 1;

        /// <summary>
        /// Determines whether the prefab enables remote debugging by calling Web.EnableRemoteDebugging().
        /// The default is `false`.
        /// </summary>
        /// <seealso href="https://support.vuplex.com/articles/how-to-debug-web-content"/>
        [Header("Debugging")]
        [Tooltip("Determines whether remote debugging is enabled.")]
        public bool RemoteDebuggingEnabled = false;

        /// <summary>
        /// Determines whether scrolling is enabled. The default is `true`.
        /// </summary>
        /// <remarks>
        /// This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        public bool ScrollingEnabled = true;

        /// <summary>
        /// Gets or sets whether the instance is visible and responds to raycasts. The default is `true`.
        /// If you want to hide the webview, setting this property to false is often preferable to disabling
        /// the prefab's GameObject, because the latter stops the prefab's scripts from running, which may
        /// prevent it from initializating, for example.
        /// </summary>
        public virtual bool Visible {
            get { return _visible; }
            set {
                _visible = value;
                _view.gameObject.SetActive(value);
                if (_videoLayerIsEnabled) {
                    _videoLayer.gameObject.SetActive(value);
                }
            }
        }

        /// <summary>
        /// Returns the prefab's IWebView instance, or `null` if the prefab hasn't finished
        /// initializing yet. To detect when the WebView property is no longer null,
        /// please use WaitUntilInitialized().
        /// </summary>
        /// <example>
        /// <code>
        /// await webViewPrefab.WaitUntilInitialized();
        /// // Now the WebView property is ready.
        /// webViewPrefab.WebView.LoadUrl("https://vuplex.com");
        /// </code>
        /// </example>
        public IWebView WebView {
            get {
                if (_cachedWebView == null) {
                    if (_webViewGameObject == null) {
                        return null;
                    }
                    _cachedWebView = _webViewGameObject.GetComponent<IWebView>();
                }
                return _cachedWebView;
            }
            private set {
                var monoBehaviour = value as MonoBehaviour;
                if (monoBehaviour == null) {
                    throw new ArgumentException("The IWebView cannot be set successfully because it's not a MonoBehaviour.");
                }
                _webViewGameObject = monoBehaviour.gameObject;
                _cachedWebView = value;
            }
        }

        /// <summary>
        /// Converts the given point from web browser viewport coordinates to the
        /// corresponding Unity screen space coordinates of where the point is rendered
        /// in the application window. Note that web browser coordinates (the input parameters)
        /// treat the top left corner of the browser viewport as the (0, 0) origin. In contrast,
        /// Unity's screen space coordinates (the return value) treat the bottom left corner of the application's
        /// window as the (0, 0) origin.
        /// </summary>
        /// <example>
        /// <code>
        /// var screenPoint = webViewPrefab.BrowserToSreenPoint(200, 300);
        /// Debug.Log($"Point (200px, 300px) within the browser window is rendered at point {screenPoint} on the device screen.");
        /// </code>
        /// </example>
        public abstract Vector2 BrowserToScreenPoint(int xInPixels, int yInPixels);

        /// <summary>
        /// Destroys the instance and its children. Note that you don't have
        /// to call this method if you destroy the instance's parent with
        /// Object.Destroy().
        /// </summary>
        /// <example>
        /// <code>
        /// // The webview can no longer be used after it's destroyed.
        /// webViewPrefab.Destroy();
        /// </code>
        /// </example>
        public void Destroy() => Destroy(gameObject);

        public void SetCutoutRect(Rect rect) => _view.SetCutoutRect(rect);

        /// <summary>
        /// Sets options that can be used to alter the webview that the prefab creates
        /// during initialization. This method can only be called prior to
        /// when the prefab initializes (i.e. directly after instantiating it or setting it to active).
        /// </summary>
        /// <example>
        /// <code>
        /// using System;
        /// using UnityEngine;
        /// using Vuplex.WebView;
        ///
        /// class SetOptions : MonoBehaviour {
        ///
        ///     // IMPORTANT: With this approach, you must set your WebViewPrefab or CanvasWebViewPrefab to inactive
        ///     //            in the scene hierarchy so that this script can manually activate it. Otherwise, this
        ///     //            script won't be able to call SetOptionsForInitialization() before the prefab initializes.
        ///     //
        ///     // TODO: Set this webViewPrefab field to the inactive WebViewPrefab or CanvasWebViewPrefab in your scene
        ///     //       via the Editor's Inspector tab.
        ///     public BaseWebViewPrefab webViewPrefab;
        ///
        ///     void Awake() {
        ///
        ///         if (webViewPrefab.gameObject.activeInHierarchy) {
        ///             throw new Exception("The WebViewPrefab object is active in the Editor's scene view. Please set it to inactive so that this script can manually activate it.");
        ///         }
        ///         webViewPrefab.gameObject.SetActive(true);
        ///
        ///         webViewPrefab.SetOptionsForInitialization(new WebViewOptions {
        ///             preferredPlugins = new WebPluginType[] { WebPluginType.Android }
        ///         });
        ///     }
        /// }
        /// </code>
        /// </example>
        public void SetOptionsForInitialization(WebViewOptions options) {

            if (WebView != null) {
                throw new ArgumentException("SetOptionsForInitialization() was called after the prefab was already initialized. Please call it before initialization instead.");
            }
            _options = options;
        }

        /// <summary>
        /// By default, the prefab detects pointer input events like clicks through
        /// Unity's event system, but you can use this method to override the way that
        /// input events are detected.
        /// </summary>
        /// <example>
        /// <code>
        /// var yourCustomInputDetector = webViewPrefab.Collider.AddComponent&lt;YourCustomInputDetector&gt;();
        /// webViewPrefab.SetPointerInputDetector(yourCustomInputDetector);
        /// </code>
        /// </example>
        public void SetPointerInputDetector(IPointerInputDetector pointerInputDetector) {

            var previousPointerInputDetector = _pointerInputDetector;
            _pointerInputDetector = pointerInputDetector;
            // If WebView hasn't been set yet, then _initPointerInputDetector
            // will get called before it's set to initialize _pointerInputDetector.
            if (WebView != null) {
                _attachOrDetachPointerInputDetector(previousPointerInputDetector, false);
                _initPointerInputDetector(WebView);
            }
        }

        /// <summary>
        /// By default, the prefab creates a new IWebView during initialization. However,
        /// you can call this method before the prefab initializes to pass it an existing,
        /// initialized IWebView to use instead. This method can only be called prior to
        /// when the prefab initializes (i.e. directly after instantiating it or setting it to active).
        /// </summary>
        public void SetWebViewForInitialization(IWebView webView) {

            if (WebView != null) {
                throw new ArgumentException("SetWebViewForInitialization() was called after the prefab was already initialized. Please call it before initialization instead.");
            }
            if (webView != null && !webView.IsInitialized) {
                throw new ArgumentException("SetWebViewForInitialization(IWebView) was called with an uninitialized webview, but an initialized webview is required.");
            }
            _webViewForInitialization = webView;
        }

        /// <summary>
        /// Returns a task that completes when the prefab is initialized,
        /// which means that its WebView property is ready for use.
        /// </summary>
        /// <example>
        /// await webViewPrefab.WaitUntilInitialized();
        /// // Now the WebView property is ready.
        /// webViewPrefab.WebView.LoadUrl("https://vuplex.com");
        /// </example>
        public Task WaitUntilInitialized() {

            var taskSource = new TaskCompletionSource<bool>();
            var isInitialized = WebView != null;
            if (isInitialized) {
                taskSource.SetResult(true);
            } else {
                Initialized += (sender, e) => taskSource.SetResult(true);
            }
            return taskSource.Task;
        }

    #region Non-public members
        float _appliedResolution;
        [SerializeField]
        [HideInInspector]
        ViewportMaterialView _cachedVideoLayer;
        [SerializeField]
        [HideInInspector]
        ViewportMaterialView _cachedView;
        IWebView _cachedWebView;
        bool _consoleMessageLoggedHandlerAttached;
        bool _dragThresholdReached;
        bool _dragToScrollClickIsPending;
        bool _hasOverriddenCursorIcon;
        int _heightInPixels { get { return (int)(_sizeInUnityUnits.y * _appliedResolution); }}
        bool _keyboardHasBeenEnabled;
        bool _loggedDragWarning;
        static bool _loggedHoverWarning;
        protected WebViewOptions _options;
        [SerializeField]
        [HideInInspector]
        MonoBehaviour _pointerInputDetectorMonoBehaviour;
        IPointerInputDetector _pointerInputDetector {
            get {
                return _pointerInputDetectorMonoBehaviour as IPointerInputDetector;
            }
            set {
                var monoBehaviour = value as MonoBehaviour;
                if (monoBehaviour == null) {
                    throw new ArgumentException("The provided IPointerInputDetector can't be successfully set because it's not a MonoBehaviour");
                }
                _pointerInputDetectorMonoBehaviour = monoBehaviour;
            }
        }
        bool _pointerIsDown;
        Vector2 _pointerDownNormalizedPoint;
        Vector2 _previousNormalizedDragPoint;
        Vector2 _previousMovePointerPoint;
        static bool _remoteDebuggingEnabled;
        protected Vector2 _sizeInUnityUnits;
        protected ViewportMaterialView _videoLayer {
            get {
                if (_cachedVideoLayer == null) {
                    _cachedVideoLayer = _getVideoLayer();
                }
                return _cachedVideoLayer;
            }
        }
        bool _videoLayerIsEnabled {
            get {
                return _videoLayer != null && _videoLayer.gameObject.activeSelf;
            }
            set {
                if (_videoLayer != null) {
                    _videoLayer.gameObject.SetActive(value);
                }
            }
        }
        Material _videoMaterial;
        protected ViewportMaterialView _view {
            get {
                if (_cachedView == null) {
                    _cachedView = _getView();
                }
                return _cachedView;
            }
        }
        Material _viewMaterial;
        bool _visible = true;
        protected IWebView _webViewForInitialization;
        [SerializeField]
        [HideInInspector]
        GameObject _webViewGameObject;
        int _widthInPixels { get { return (int)(_sizeInUnityUnits.x * _appliedResolution); }}

        void _attachOrDetachPointerInputDetector(IPointerInputDetector detector, bool attach) {

            if (attach) {
                detector.BeganDrag += InputDetector_BeganDrag;
                detector.Dragged += InputDetector_Dragged;
                detector.PointerDown += InputDetector_PointerDown;
                detector.PointerEntered += InputDetector_PointerEntered;
                detector.PointerExited += InputDetector_PointerExited;
                detector.PointerMoved += InputDetector_PointerMoved;
                detector.PointerUp += InputDetector_PointerUp;
                detector.Scrolled += InputDetector_Scrolled;
            } else {
                detector.BeganDrag -= InputDetector_BeganDrag;
                detector.Dragged -= InputDetector_Dragged;
                detector.PointerDown -= InputDetector_PointerDown;
                detector.PointerEntered -= InputDetector_PointerEntered;
                detector.PointerExited -= InputDetector_PointerExited;
                detector.PointerMoved -= InputDetector_PointerMoved;
                detector.PointerUp -= InputDetector_PointerUp;
                detector.Scrolled -= InputDetector_Scrolled;
            }
        }

        void _attachWebViewEventHandlers(IWebView webView) {

            _enableConsoleMessagesIfNeeded(webView);
            // Needed for Vulkan support on Android.
            // See the comments in IWithChangingTexture.cs for details.
            var webViewWithChangingTexture = webView as IWithChangingTexture;
            if (webViewWithChangingTexture != null) {
                webViewWithChangingTexture.TextureChanged += WebView_TextureChanged;
            }
            // Needed for fallback video support on iOS.
            var webViewWithFallbackVideo = webView as IWithFallbackVideo;
            if (webViewWithFallbackVideo != null && !_options.disableVideo) {
                webViewWithFallbackVideo.VideoRectChanged += (sender, eventArgs) => _setVideoRect(eventArgs.Value);
            }
        }

        void _disableHoveringIfNeeded(bool preferNative2DMode) {

            #if (UNITY_IOS || UNITY_WSA) && !VUPLEX_NO_DISABLING_HOVER_FOR_PERFORMANCE
                if (!HoveringEnabled) {
                    return;
                }
                if (preferNative2DMode) {
                    // Hovering isn't detected in Native 2D Mode, so logging a warning is unnecessary.
                    return;
                }
                HoveringEnabled = false;
                if (!_loggedHoverWarning) {
                    _loggedHoverWarning = true;
                    WebViewLogger.LogWarning("WebViewPrefab.HoveringEnabled is automatically set to false by default on iOS and UWP in order to optimize performance. However, you can override this by adding the scripting symbol VUPLEX_NO_DISABLING_HOVER_FOR_PERFORMANCE in player settings. For more info, see <em>https://support.vuplex.com/articles/hover-and-drag-limitations</em>.");
                }
            #endif
        }

        void _enableConsoleMessagesIfNeeded(IWebView webView) {

            if (LogConsoleMessages && !_consoleMessageLoggedHandlerAttached && webView != null) {
                _consoleMessageLoggedHandlerAttached = true;
                webView.ConsoleMessageLogged += WebView_ConsoleMessageLogged;
            }
        }

        void _enableNativeOnScreenKeyboardIfNeeded(IWebView webView) {

            if (webView is IWithNativeOnScreenKeyboard) {
                var nativeOnScreenKeyboardEnabled = _getNativeOnScreenKeyboardEnabled();
                (webView as IWithNativeOnScreenKeyboard).SetNativeOnScreenKeyboardEnabled(nativeOnScreenKeyboardEnabled);
            }
        }

        void _enableOrDisableKeyboardIfNeeded() {

            if (WebView != null && _keyboardHasBeenEnabled != KeyboardEnabled) {
                Internal.KeyboardManager.Instance.SetKeyboardEnabled(this, KeyboardEnabled);
                _keyboardHasBeenEnabled = KeyboardEnabled;
            }
        }

        void _enableRemoteDebuggingIfNeeded() {

            // Remote debugging can only be enabled once, before any webviews are initialized.
            if (RemoteDebuggingEnabled && !_remoteDebuggingEnabled) {
                _remoteDebuggingEnabled = true;
                try {
                    Web.EnableRemoteDebugging();
                } catch (Exception ex) {
                    WebViewLogger.LogError("An exception occurred while enabling remote debugging. On Windows and macOS, this can happen if a prefab with RemoteDebuggingEnabled = true is created after a prior webview has already been initialized. Exception message: " + ex);
                }
            }
        }

        protected abstract float _getResolution();

        protected abstract bool _getNativeOnScreenKeyboardEnabled();

        protected abstract float _getScrollingSensitivity();

        IWithTouch _getTouchIfSupported() {

            var webViewWithTouch = WebView as IWithTouch;
            // Touch is currently disabled for the Android Gecko package because it currently has an
            // issue where scrolling doesn't work correctly with touch.
            if (webViewWithTouch != null && WebView.PluginType != WebPluginType.AndroidGecko) {
                return webViewWithTouch;
            }
            return null;
        }

        protected abstract ViewportMaterialView _getVideoLayer();

        protected abstract ViewportMaterialView _getView();

        void _handleTrialExpired() {

            _view.Material = new Material(Resources.Load<Material>("TrialExpiredMaterial"));
            _videoLayer.gameObject.SetActive(false);
        }

        protected async Task _initBase(Rect rect, bool preferNative2DMode = false) {

            _throwExceptionIfInitialized();
            _sizeInUnityUnits = rect.size;
            _updateResolutionIfNeeded();
            _disableHoveringIfNeeded(preferNative2DMode);
            _enableRemoteDebuggingIfNeeded();
            // Note: this.WebView is only set after the webview has been initialized to guarantee
            // that the property is ready to use as long as it's not null.
            var webView = await _initWebView(rect, preferNative2DMode);
            if (this == null) {
                // This prefab was destroyed while waiting for the webview to initialize.
                webView.Dispose();
                return;
            }
            _initViews(webView);
            _enableNativeOnScreenKeyboardIfNeeded(webView);
            _attachWebViewEventHandlers(webView);
            // Init the pointer input detector just before setting WebView so that
            // SetPointerInputDetector() will behave correctly if it's called before WebView is set.
            if (!_native2DModeEnabled(webView)) {
                _initPointerInputDetector(webView);
            }
            // The webview is now fully initialized, so we can now set WebView and raise the Initialized event.
            WebView = webView;
            // Intentionally call KeyboardManager.SetKeyboardEnabled() before raising the Initialized event so that it can attach a listener
            // to the IWebView.FocusChanged event before the application is able to call IWebView.SetFocused().
            _enableOrDisableKeyboardIfNeeded();
            Initialized?.Invoke(this, EventArgs.Empty);
            // Lastly, load the InitialUrl.
            if (!String.IsNullOrWhiteSpace(InitialUrl)) {
                if (_webViewForInitialization != null) {
                    WebViewLogger.LogWarning("Custom InitialUrl value will be ignored because an initialized webview was provided.");
                } else {
                    webView.LoadUrl(InitialUrl.Trim());
                }
            }
        }

        void _initViews(IWebView webView) {

            if (_native2DModeEnabled(webView)) {
                if (_view != null) {
                    _view.gameObject.SetActive(false);
                }
                _videoLayerIsEnabled = false;
                return;
            }
            // Initialize the main view.
            _viewMaterial = webView.CreateMaterial();
            _view.Material = _viewMaterial;

            // Initialize the video view (iOS only).
            var webViewWithFallbackVideo = webView as IWithFallbackVideo;
            if (webViewWithFallbackVideo != null && webViewWithFallbackVideo.FallbackVideoEnabled) {
                _videoMaterial = webViewWithFallbackVideo.CreateVideoMaterial();
                _videoLayer.Material = _videoMaterial;
                _setVideoRect(Rect.zero);
            } else {
                _videoLayerIsEnabled = false;
            }
        }

        async Task<IWebView> _initWebView(Rect rect, bool preferNative2DMode) {

            if (_webViewForInitialization != null) {
                return _webViewForInitialization;
            }
            var webView = Web.CreateWebView(_options.preferredPlugins);

            // Enable Native 2D Mode if needed.
            var enableNative2DMode = preferNative2DMode && webView is IWithNative2DMode;
            if (enableNative2DMode) {
                var native2DWebView = webView as IWithNative2DMode;
                try {
                    await native2DWebView.InitInNative2DMode(rect);
                } catch (TrialExpiredException ex) {
                    _handleTrialExpired();
                    throw ex;
                }
                // Hide the webview if Visible has already been set to false.
                native2DWebView.SetVisible(_visible);
                return webView;
            }

            _updatePixelDensityIfNeeded(webView);

            // (iOS only) Enable fallback video if needed.
            var webViewWithFallbackVideo = webView as IWithFallbackVideo;
            if (webViewWithFallbackVideo != null && !_options.disableVideo) {
                webViewWithFallbackVideo.SetFallbackVideoEnabled(true);
            }

            try {
                await webView.Init(_widthInPixels, _heightInPixels);
            } catch (TrialExpiredException ex) {
                _handleTrialExpired();
                throw ex;
            }

            // (Windows and macOS only) Enable cursor icons if needed.
            var webViewWithCursorType = webView as IWithCursorType;
            if (webViewWithCursorType != null && CursorIconsEnabled && !XRSettings.enabled) {
                webViewWithCursorType.CursorTypeChanged += (sender, eventArgs) => {
                    Internal.CursorHelper.SetCursorIcon(eventArgs.Value);
                    _hasOverriddenCursorIcon = eventArgs.Value != "default";
                };
            }

            return webView;
        }

        void _initPointerInputDetector(IWebView webView) {

            if (_pointerInputDetector == null) {
                // Pass the argument `true` to find the IPointerInputDetector even if it's disabled.
                // Otherwise, if BaseWebViewPrefab.Visible is set to `false` before initialization,
                // the IPointerInputDetector won't be found, which will lead to a NullReferenceException.
                _pointerInputDetector = GetComponentInChildren<IPointerInputDetector>(true);
            }
            // Only enable the PointerMoved event if the webview implementation has MovePointer().
            _pointerInputDetector.PointerMovedEnabled = (webView as IWithMovablePointer) != null;
            _attachOrDetachPointerInputDetector(_pointerInputDetector, true);
        }

        void InputDetector_BeganDrag(object sender, EventArgs<Vector2> eventArgs) {

            _dragThresholdReached = false;
            _previousNormalizedDragPoint = _pointerDownNormalizedPoint;
        }

        void InputDetector_Dragged(object sender, EventArgs<Vector2> eventArgs) {

            if (DragMode == DragMode.Disabled || WebView == null) {
                return;
            }
            var newNormalizedDragPoint = eventArgs.Value;
            var totalDragDeltaInPixels = WebView.NormalizedToPoint(_pointerDownNormalizedPoint - newNormalizedDragPoint);
            if (!_dragThresholdReached) {
                // _dragThresholdReached needs to be saved, otherwise it could flip from true back
                // to false if the user drags back to the original point where the drag started.
                _dragThresholdReached = totalDragDeltaInPixels.magnitude > DragThreshold;
            }
            if (DragMode == DragMode.DragWithinPage) {
                if (_dragThresholdReached) {
                    _movePointerIfNeeded(newNormalizedDragPoint);
                }
                return;
            }
            // DragMode is DragToScroll
            var webViewWithTouch = _getTouchIfSupported();
            if (webViewWithTouch != null && _dragThresholdReached && !_options.clickWithoutStealingFocus) {
                webViewWithTouch.SendTouchEvent(new TouchEvent {
                    TouchID = 1,
                    Point = newNormalizedDragPoint,
                    Type = TouchEventType.Move
                });
                return;
            }
            var normalizedDragDelta = _previousNormalizedDragPoint - newNormalizedDragPoint;
            if (WebView.NormalizedToPoint(normalizedDragDelta) == Vector2Int.zero) {
                // The latest drag delta was less than one pixel of difference, so wait until it reaches at
                // least one pixel of change before setting _previousNormalizedDragPoint. Otherwise, small
                // drags will be ignored, resulting in scrolling to not work if the drag is very slow.
                return;
            }
            _previousNormalizedDragPoint = newNormalizedDragPoint;
            _scrollIfNeeded(normalizedDragDelta, _pointerDownNormalizedPoint);
            // Check whether to cancel a pending viewport click so that drag-to-scroll
            // doesn't unintentionally trigger a click.
            if (_dragToScrollClickIsPending && _dragThresholdReached) {
                _dragToScrollClickIsPending = false;
            }
        }

        protected virtual void InputDetector_PointerDown(object sender, PointerEventArgs eventArgs) {

            _pointerIsDown = true;
            _pointerDownNormalizedPoint = eventArgs.Point;
            if (!ClickingEnabled || WebView == null) {
                return;
            }
            if (DragMode == DragMode.DragToScroll) {
                var webViewWithTouch = _getTouchIfSupported();
                if (webViewWithTouch != null && !_options.clickWithoutStealingFocus) {
                    webViewWithTouch.SendTouchEvent(new TouchEvent {
                        TouchID = 1,
                        Point = eventArgs.Point,
                        Type = TouchEventType.Start
                    });
                } else {
                    // For DragToScroll(), defer calling PointerDown() or Click() so that the click can
                    // be cancelled if the drag exceeds the threshold needed to become a scroll.
                    _dragToScrollClickIsPending = true;
                }
                return;
            }
            // For DragMode.DragWithinPage and DragMode.Disabled, call PointerDown() immediately if the webview supports IWithPointerDownAndUp.
            // Note that PointerDown() doesn't currently support an option to avoid stealing focus, so Click() must be used in that case.
            var webViewWithPointerDown = WebView as IWithPointerDownAndUp;
            if (webViewWithPointerDown != null) {
                webViewWithPointerDown.PointerDown(eventArgs.Point, eventArgs.ToPointerOptions(_options.clickWithoutStealingFocus));
            } else if (DragMode == DragMode.DragWithinPage && webViewWithPointerDown == null && !_loggedDragWarning) {
                _loggedDragWarning = true;
                WebViewLogger.LogWarning($"The WebViewPrefab's DragMode is set to DragWithinPage, but the webview implementation for this platform ({WebView.PluginType}) doesn't support the PointerDown() and PointerUp() methods needed for dragging within a page. For more info, see <em>https://developer.vuplex.com/webview/IWithPointerDownAndUp</em>.");
            }
        }

        void InputDetector_PointerEntered(object sender, EventArgs eventArgs) => PointerEntered?.Invoke(this, EventArgs.Empty);

        void InputDetector_PointerExited(object sender, EventArgs<Vector2> eventArgs) {

            if (HoveringEnabled) {
                // Remove the hover state when the pointer exits.
                _movePointerIfNeeded(eventArgs.Value, true);
            }
            if (_hasOverriddenCursorIcon) {
                Internal.CursorHelper.SetCursorIcon(null);
                _hasOverriddenCursorIcon = false;
            }
            PointerExited?.Invoke(this, EventArgs.Empty);
        }

        void InputDetector_PointerMoved(object sender, EventArgs<Vector2> eventArgs) {

            // InputDetector_Dragged handles calling MovePointer while dragging.
            if (_pointerIsDown || !HoveringEnabled) {
                return;
            }
            _movePointerIfNeeded(eventArgs.Value);
        }

        protected virtual void InputDetector_PointerUp(object sender, PointerEventArgs eventArgs) {

            _pointerIsDown = false;
            if (!ClickingEnabled || WebView == null) {
                return;
            }
            var pointerUpPoint = eventArgs.Point;
            var clickedEventArgs = new ClickedEventArgs(pointerUpPoint);
            if (DragMode == DragMode.DragToScroll) {
                var webViewWithTouch = _getTouchIfSupported();
                if (webViewWithTouch != null && !_options.clickWithoutStealingFocus) {
                    webViewWithTouch.SendTouchEvent(new TouchEvent {
                        TouchID = 1,
                        Point = pointerUpPoint,
                        Type = TouchEventType.End
                    });
                    Clicked?.Invoke(this, clickedEventArgs);
                    return;
                }
                if (!_dragToScrollClickIsPending) {
                    // The click was cancelled because it was processed as a scroll.
                    return;
                }
            }
            _dragToScrollClickIsPending = false;
            var webViewWithPointerDownAndUp = WebView as IWithPointerDownAndUp;
            if (webViewWithPointerDownAndUp == null) {
                // When clickWithoutStealingFocus is enabled, use Click() because PointerDown() and PointerUp() don't support the preventStealingFocus parameter.
                WebView.Click(eventArgs.Point, _options.clickWithoutStealingFocus);
            } else {
                var pointerOptions = eventArgs.ToPointerOptions(_options.clickWithoutStealingFocus);
                if (DragMode == DragMode.DragToScroll) {
                    // For DragToScroll, PointerDown() is deferred until just before PointerUp() in case the click is cancelled.
                    webViewWithPointerDownAndUp.PointerDown(eventArgs.Point, pointerOptions);
                } else if (DragMode == DragMode.DragWithinPage) {
                    // For DragWithinPage, use the point passed to PointerUp() if the DragThreshold wasn't reached.
                    var totalDragDeltaInPixels = WebView.NormalizedToPoint(_pointerDownNormalizedPoint - eventArgs.Point);
                    var dragThresholdReached = totalDragDeltaInPixels.magnitude > DragThreshold;
                    if (!dragThresholdReached) {
                        pointerUpPoint = _pointerDownNormalizedPoint;
                    }
                }
                webViewWithPointerDownAndUp.PointerUp(pointerUpPoint, pointerOptions);
            }
            Clicked?.Invoke(this, clickedEventArgs);
        }

        void InputDetector_Scrolled(object sender, ScrolledEventArgs eventArgs) {

            var sensitivity = _getScrollingSensitivity();
            // The ScrollingSensivity is measured in Unity units because the argument
            // passed to Scroll(Vector2) was originally in Unity units (but is now a normalized value).
            var scaledScrollDeltaInUnityUnits = eventArgs.ScrollDelta * sensitivity;
            var normalizedScrollDelta = new Vector2(scaledScrollDeltaInUnityUnits.x / _sizeInUnityUnits.x, scaledScrollDeltaInUnityUnits.y / _sizeInUnityUnits.y);
            _scrollIfNeeded(normalizedScrollDelta, eventArgs.Point);
        }

        void _movePointerIfNeeded(Vector2 point, bool pointerLeave = false) {

            var webViewWithMovablePointer = WebView as IWithMovablePointer;
            if (webViewWithMovablePointer == null) {
                return;
            }
            if (point != _previousMovePointerPoint) {
                _previousMovePointerPoint = point;
                webViewWithMovablePointer.MovePointer(point, pointerLeave);
            }
        }

        bool _native2DModeEnabled(IWebView webView) => webView is IWithNative2DMode && (webView as IWithNative2DMode).Native2DModeEnabled;

        protected virtual void OnDestroy() {

            if (WebView != null && !WebView.IsDisposed) {
                WebView.Dispose();
            }
            if (KeyboardEnabled) {
                var keyboardManager = Internal.KeyboardManager.Instance;
                if (keyboardManager != null) {
                    keyboardManager.SetKeyboardEnabled(this, false);
                }
            }
            if (_pointerInputDetector != null) {
                // Detach the pointer input detector in case the app has assigned a custom one that's not a child.
                _attachOrDetachPointerInputDetector(_pointerInputDetector, false);
                _pointerInputDetectorMonoBehaviour = null;
            }
            Destroy();
            // Unity doesn't automatically destroy materials and textures
            // when the GameObject is destroyed.
            if (_viewMaterial != null) {
                Destroy(_viewMaterial.mainTexture);
                Destroy(_viewMaterial);
            }
            if (_videoMaterial != null) {
                Destroy(_videoMaterial.mainTexture);
                Destroy(_videoMaterial);
            }
            if (_hasOverriddenCursorIcon) {
                Internal.CursorHelper.SetCursorIcon(null);
                _hasOverriddenCursorIcon = false;
            }
        }

        protected void _resizeWebViewIfNeeded() {

            if (WebView != null && WebView.Size != new Vector2(_widthInPixels, _heightInPixels)) {
                WebView.Resize(_widthInPixels, _heightInPixels);
            }
        }

        void _scrollIfNeeded(Vector2 scrollDelta, Vector2 point) {

            // scrollDelta can be zero when the user drags the cursor off the screen.
            if (!ScrollingEnabled || WebView == null || scrollDelta == Vector2.zero) {
                return;
            }
            WebView.Scroll(scrollDelta, point);
            Scrolled?.Invoke(this, new ScrolledEventArgs(scrollDelta, point));
        }

        protected abstract void _setVideoLayerPosition(Rect videoRect);

        void _setVideoRect(Rect videoRect) {

            if (_videoLayer == null) {
                return;
            }
            _view.SetCutoutRect(videoRect);
            _setVideoLayerPosition(videoRect);
            // This code applies a cropping rect to the video layer's shader based on what part of the video (if any)
            // falls outside of the viewport and therefore needs to be hidden. Note that the dimensions here are divided
            // by the videoRect's width or height, because in the videoLayer shader, the width of the videoRect is 1
            // and the height is 1 (i.e. the dimensions are normalized).
            float videoRectXMin = Math.Max(0, - 1 * videoRect.x / videoRect.width);
            float videoRectYMin = Math.Max(0, -1 * videoRect.y / videoRect.height);
            float videoRectXMax = Math.Min(1, (1 - videoRect.x) / videoRect.width);
            float videoRectYMax = Math.Min(1, (1 - videoRect.y) / videoRect.height);
            var videoCropRect = Rect.MinMaxRect(videoRectXMin, videoRectYMin, videoRectXMax, videoRectYMax);
            if (videoCropRect == new Rect(0, 0, 1, 1)) {
                // The entire video rect fits within the viewport, so set the cropt rect to zero to disable it.
                videoCropRect = Rect.zero;
            }
            _videoLayer.SetCropRect(videoCropRect);
        }

        void _throwExceptionIfInitialized() {

            if (WebView != null) {
                throw new InvalidOperationException("Init() cannot be called on a WebViewPrefab that has already been initialized.");
            }
        }

        protected virtual void Update() {

            _updateResolutionIfNeeded();
            _updatePixelDensityIfNeeded(WebView);
            _enableOrDisableKeyboardIfNeeded();
            _enableConsoleMessagesIfNeeded(WebView);
        }

        void _updatePixelDensityIfNeeded(IWebView webView) {

            var webViewWithPixelDensity = webView as IWithPixelDensity;
            if (webViewWithPixelDensity == null || PixelDensity == webViewWithPixelDensity.PixelDensity) {
                return;
            }
            try {
                webViewWithPixelDensity.SetPixelDensity(PixelDensity);
            } catch (ArgumentException ex) {
                WebViewLogger.LogError(ex.ToString());
                PixelDensity = 1;
            }
        }

        void _updateResolutionIfNeeded() {

            var resolution = _getResolution();
            if (_appliedResolution != resolution) {
                if (resolution > 0.0f) {
                    _appliedResolution = resolution;
                    _resizeWebViewIfNeeded();
                } else {
                    WebViewLogger.LogWarning("Ignoring invalid Resolution: " + resolution);
                }
            }
        }

        void WebView_ConsoleMessageLogged(object sender, ConsoleMessageEventArgs eventArgs) {

            if (!LogConsoleMessages) {
                return;
            }
            var message = "[Web Console] " + eventArgs.Message;
            if (eventArgs.Source != null) {
                message += $" ({eventArgs.Source}:{eventArgs.Line})";
            }
            switch (eventArgs.Level) {
                case ConsoleMessageLevel.Error:
                    WebViewLogger.LogError(message, false);
                    break;
                case ConsoleMessageLevel.Warning:
                    WebViewLogger.LogWarning(message, false);
                    break;
                default:
                    WebViewLogger.Log(message, false);
                    break;
            }
        }

        void WebView_TextureChanged(object sender, EventArgs<Texture2D> eventArgs) {

            var oldTexture = _view.Texture;
            if (oldTexture is RenderTexture) {
                // The application replaced WebViewPrefab.Material.mainTexture with a RenderTexture
                // (for example, to implement MipMaps), so don't change the prefab's texture.
                return;
            }
            _view.Texture = eventArgs.Value;
            Destroy(oldTexture);
        }
    #endregion

        // Added in v3.5, removed in v3.7.
        [Obsolete("The WebViewPrefab.DragToScrollThreshold property has been removed. Please use DragThreshold instead: https://developer.vuplex.com/webview/WebViewPrefab#DragThreshold", true)]
        public float DragToScrollThreshold { get; set; }
    }
}
