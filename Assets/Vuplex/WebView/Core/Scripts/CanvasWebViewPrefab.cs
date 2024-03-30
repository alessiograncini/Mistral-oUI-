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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    /// <summary>
    /// CanvasWebViewPrefab is a prefab that makes it easy to view and interact with an IWebView in a 2D Canvas.
    /// It takes care of creating an IWebView, displaying its texture, and handling pointer interactions
    /// from the user, like clicking, dragging, and scrolling. So, all you need to do is specify a URL or HTML to load,
    /// and then the user can view and interact with it. For use outside of a Canvas, see WebViewPrefab instead.
    /// </summary>
    /// <remarks>
    /// There are two ways to create a CanvasWebViewPrefab:
    /// <list type="number">
    ///   <item>
    ///     By dragging the CanvasWebViewPrefab.prefab file into your scene via the editor and setting its "Initial URL" property.</item>
    ///   <item>
    ///     Or by creating an instance programmatically with CanvasWebViewPrefab.Instantiate(), waiting for
    ///     it to initialize, and then calling methods on its WebView property, like LoadUrl().
    ///   </item>
    /// </list>
    /// <para>
    /// If your use case requires a high degree of customization, you can instead create an IWebView
    /// outside of the prefab with Web.CreateWebView().
    /// </para>
    /// See also:
    /// <list type="bullet">
    ///   <item>WebViewPrefab: https://developer.vuplex.com/webview/WebViewPrefab</item>
    ///   <item>How clicking and scrolling works: https://support.vuplex.com/articles/clicking</item>
    ///   <item>IWebView: https://developer.vuplex.com/webview/IWebView</item>
    ///   <item>Web (static methods): https://developer.vuplex.com/webview/Web</item>
    /// </list>
    /// </remarks>
    [HelpURL("https://developer.vuplex.com/webview/CanvasWebViewPrefab")]
    public partial class CanvasWebViewPrefab : BaseWebViewPrefab {

        public override event EventHandler<ClickedEventArgs> Clicked {
            add {
                if (_native2DModeActive) {
                    _logNative2DModeWarning("The CanvasWebViewPrefab.Clicked event is not supported in Native 2D Mode.");
                }
                base.Clicked += value;
            }
            remove {
                base.Clicked -= value;
            }
        }

        public override event EventHandler<ScrolledEventArgs> Scrolled {
            add {
                if (_native2DModeActive) {
                    _logNative2DModeWarning("The CanvasWebViewPrefab.Scrolled event is not supported in Native 2D Mode.");
                }
                base.Scrolled += value;
            }
            remove {
                base.Scrolled -= value;
            }
        }

        /// <summary>
        /// Enables or disables [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode/),
        /// which makes it so that 3D WebView positions a native 2D webview in front of the Unity game view
        /// instead of displaying web content as a texture in the Unity scene. The default is `false`. If set to `true` and the 3D WebView package
        /// in use doesn't support Native 2D Mode, then the default rendering mode is used instead. This field can only be set prior to
        /// initialization (i.e. prior to when Unity calls Start() for the CanvasWebViewPrefab). Native 2D Mode cannot be enabled or disabled
        /// after the webview has been initialized, so changing this field's value after initialization has no effect.
        /// </summary>
        /// <remarks>
        /// Important notes:
        /// <list type="bullet">
        ///   <item>
        ///     Native 2D Mode is only supported for 3D WebView for Android (non-Gecko) and 3D WebView for iOS.
        ///     For other packages, the default render mode is used instead.
        ///   </item>
        ///   <item>Native 2D Mode requires that the canvas's render mode be set to "Screen Space - Overlay".</item>
        /// </list>
        /// </remarks>
        [Label("Native 2D Mode (Android, iOS, WebGL, and UWP only)")]
        [Tooltip("Native 2D Mode positions a native 2D webview in front of the Unity game view instead of rendering web content as a texture in the Unity scene. Native 2D Mode provides better performance on iOS and UWP, because the default mode of rendering web content to a texture is slower. \n\nImportant notes:\n• Native 2D Mode is only supported for Android (non-Gecko), iOS, WebGL, and UWP. For the other 3D WebView packages, the default render mode is used instead.\n• Native 2D Mode requires that the canvas's render mode be set to \"Screen Space - Overlay\".")]
        [HideInInspector]
        [Header("Platform-specific")]
        public bool Native2DModeEnabled;

        /// <summary>
        /// Determines whether the operating system's native on-screen keyboard is
        /// automatically shown when a text input in the webview is focused. The default for
        /// CanvasWebViewPrefab is `true`.
        /// </summary>
        /// <seealso cref="IWithNativeOnScreenKeyboard"/>
        /// <remarks>
        /// The native on-screen keyboard is only supported for the following packages:
        /// <list type="bullet">
        ///   <item>3D WebView for Android (non-Gecko)</item>
        ///   <item>3D WebView for iOS</item>
        /// </list>
        /// </remarks>
        /// <remarks>
        /// 3D WebView for Android with Gecko Engine doesn't support automatically showing the native on-screen keyboard,
        /// but you can use Unity's [TouchScreenKeyboard](https://docs.unity3d.com/ScriptReference/TouchScreenKeyboard.html)
        /// API to show the keyboard and then send typed characters to the webview like described in [this article](https://support.vuplex.com/articles/how-to-use-a-third-party-keyboard).
        /// </remarks>
        /// <remarks>
        /// On iOS, disabling the keyboard for one webview disables it for all webviews.
        /// </remarks>
        /// <seealso cref="IWithNativeOnScreenKeyboard"/>
        /// <seealso cref="KeyboardEnabled"/>
        [Label("Native On-Screen Keyboard (Android and iOS only)")]
        [Tooltip("Determines whether the operating system's native on-screen keyboard is automatically shown when a text input in the webview is focused. The native on-screen keyboard is only supported for the following packages:\n• 3D WebView for Android (non-Gecko)\n• 3D WebView for iOS")]
        public bool NativeOnScreenKeyboardEnabled = true;

        /// <summary>
        /// Gets or sets the prefab's resolution in pixels per Unity unit.
        /// You can change the resolution to make web content appear larger or smaller.
        /// The default resolution for CanvasWebViewPrefab is `1`.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Setting a lower resolution decreases the pixel density, but has the effect
        /// of making web content appear larger. Setting a higher resolution increases
        /// the pixel density, but has the effect of making content appear smaller.
        /// For more information on scaling web content, see
        /// [this support article](https://support.vuplex.com/articles/how-to-scale-web-content).
        /// </para>
        /// <para>
        /// When running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode), the Resolution field
        /// isn't used because the device's native resolution is used instead. So, the Resolution field's value is inaccurate and changes to it are ignored.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Set the resolution to 2.5px per Unity unit.
        /// webViewPrefab.Resolution = 2.5f;
        /// </code>
        /// </example>
        [Label("Resolution (px / Unity unit)")]
        [Tooltip("You can change this to make web content appear larger or smaller. Note that This property is ignored when running in Native 2D Mode.")]
        [HideInInspector]
        [FormerlySerializedAs("InitialResolution")]
        public float Resolution = 1;

        /// <summary>
        /// Determines the scroll sensitivity. The default sensitivity for CanvasWebViewPrefab is `15`.
        /// </summary>
        /// <remarks>
        /// This property is ignored when running in [Native 2D Mode](https://support.vuplex.com/articles/native-2d-mode).
        /// </remarks>
        [HideInInspector]
        [Tooltip("Determines the scroll sensitivity. Note that This property is ignored when running in Native 2D Mode.")]
        public float ScrollingSensitivity = 15;

        public override bool Visible {
            get {
                var native2DWebView = _getNative2DWebViewIfActive();
                if (native2DWebView != null) {
                    return native2DWebView.Visible;
                }
                return base.Visible;
            }
            set {
                var native2DWebView = _getNative2DWebViewIfActive();
                if (native2DWebView != null) {
                    native2DWebView.SetVisible(value);
                    return;
                }
                base.Visible = value;
            }
        }

        public override Vector2 BrowserToScreenPoint(int xInPixels, int yInPixels) {

            if (WebView == null) {
                return Vector2.zero;
            }
            var rect = _getScreenSpaceRect();
            if (rect == Rect.zero) {
                return Vector2.zero;
            }
            var normalizedPoint = WebView.PointToNormalized(xInPixels, yInPixels);
            // Clamp x and y to the range [0, WebView.Size].
            var clampedNormalizedX = Math.Min(Math.Max(normalizedPoint.x, 0), 1);
            var clampedNormalizedY = Math.Min(Math.Max(normalizedPoint.y, 0), 1);
            return new Vector2(
                rect.x + rect.width * clampedNormalizedX,
                rect.y + rect.height * clampedNormalizedY
            );
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <remarks>
        /// The WebView property is available after initialization completes,
        /// which is indicated by WaitUntilInitialized().
        /// </remarks>
        /// <example>
        /// <code>
        /// // Create a CanvasWebViewPrefab
        /// var canvasWebViewPrefab = CanvasWebViewPrefab.Instantiate();
        /// // Position the prefab how we want it
        /// var canvas = GameObject.Find("Canvas");
        /// canvasWebViewPrefab.transform.parent = canvas.transform;
        /// var rectTransform = canvasWebViewPrefab.transform as RectTransform;
        /// rectTransform.anchoredPosition3D = Vector3.zero;
        /// rectTransform.offsetMin = Vector2.zero;
        /// rectTransform.offsetMax = Vector2.zero;
        /// canvasWebViewPrefab.transform.localScale = Vector3.one;
        /// // Load a URL once the prefab finishes initializing
        /// await canvasWebViewPrefab.WaitUntilInitialized();
        /// canvasWebViewPrefab.WebView.LoadUrl("https://vuplex.com");
        /// </code>
        /// </example>
        public static CanvasWebViewPrefab Instantiate() {

            return Instantiate(new WebViewOptions());
        }

        /// <summary>
        /// Like Instantiate(), except it also accepts an object
        /// of options flags that can be used to alter the generated webview's behavior.
        /// </summary>
        public static CanvasWebViewPrefab Instantiate(WebViewOptions options) {

            var prefabPrototype = (GameObject)Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject)Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab._options = options;
            return canvasWebViewPrefab;
        }

        /// <summary>
        /// Like Instantiate(float, float), except it initializes the instance with an existing, initialized
        /// IWebView instance. This causes the CanvasWebViewPrefab to use the existing
        /// IWebView instance instead of creating a new one. This can be used, for example, to create multiple
        /// WebViewPrefabs that are connected to the same IWebView, or to create a prefab for an IWebView
        /// created by IWithPopups.PopupRequested.
        /// </summary>
        /// <example>
        /// <code>
        /// await firstWebViewPrefab.WaitUntilInitialized();
        /// var secondWebViewPrefab = CanvasWebViewPrefab.Instantiate(firstWebViewPrefab.WebView);
        /// // TODO: Position secondWebViewPrefab to the location where you want to display it.
        /// </code>
        /// </example>
        public static CanvasWebViewPrefab Instantiate(IWebView webView) {

            var prefabPrototype = (GameObject)Resources.Load("CanvasWebViewPrefab");
            var gameObject = (GameObject)Instantiate(prefabPrototype);
            var canvasWebViewPrefab = gameObject.GetComponent<CanvasWebViewPrefab>();
            canvasWebViewPrefab.SetWebViewForInitialization(webView);
            return canvasWebViewPrefab;
        }

    #region Non-public members
        RectTransform _cachedRectTransform;
        Canvas _canvas {
            get {
                if (_canvasGetter == null) {
                    _canvasGetter = new CachingGetter<Canvas>(GetComponentInParent<Canvas>, 1, this);
                }
                return _canvasGetter.GetValue();
            }
        }
        CachingGetter<Canvas> _canvasGetter;
        bool _native2DModeActive {
            get {
                var webViewWith2DMode = WebView as IWithNative2DMode;
                return webViewWith2DMode != null && webViewWith2DMode.Native2DModeEnabled;
            }
        }
        bool _native2DModeEnabledAtInitialization;
        RectTransform _rectTransform {
            get {
                if (_cachedRectTransform == null) {
                    _cachedRectTransform = GetComponent<RectTransform>();
                }
                return _cachedRectTransform;
            }
        }

        // Partial method implemented by various 3D WebView packages
        // to provide platform-specific warnings.
        partial void OnInit();

        bool _canNative2DModeBeEnabled(bool logWarnings = false) {

            if (_canvas != null && _canvas.renderMode == RenderMode.WorldSpace) {
                if (logWarnings) {
                    _logNative2DModeWarning("CanvasWebViewPrefab.Native2DModeEnabled is enabled but the canvas's render mode is set to World Space, so Native 2D Mode will not be enabled. In order to use Native 2D Mode, please switch the canvas's render mode to \"Screen Space - Overlay\" or \"Screen Space - Camera\".");
                }
                return false;
            }
            // Note: this method used to return false if XRSettings.enabled is true in order to prevent accidental use on VR headsets,
            //       but that caused an issue where Native 2D Mode couldn't be used with AR Foundation.
            return true;
        }

        Rect _getRectForInitialization(bool preferNative2DMode) => preferNative2DMode ? _getScreenSpaceRect() : _rectTransform.rect;

        protected override float _getResolution() {

            if (Resolution > 0f) {
                return Resolution;
            }
            WebViewLogger.LogError("Invalid value set for CanvasWebViewPrefab.Resolution: " + Resolution);
            return 1;
        }

        IWithNative2DMode _getNative2DWebViewIfActive() {

            var webViewWith2DMode = WebView as IWithNative2DMode;
            if (webViewWith2DMode != null && webViewWith2DMode.Native2DModeEnabled) {
                return webViewWith2DMode;
            }
            return null;
        }

        protected override bool _getNativeOnScreenKeyboardEnabled() => NativeOnScreenKeyboardEnabled;

        protected override float _getScrollingSensitivity() => ScrollingSensitivity;

        Rect _getScreenSpaceRect() {

            var canvas = _canvas;
            if (canvas == null) {
                WebViewLogger.LogError("Unable to determine the screen space rect for Native 2D Mode because the CanvasWebViewPrefab is not placed in a Canvas. Please place the CanvasWebViewPrefab as the child of a Unity UI Canvas.");
                return Rect.zero;
            }
            var worldCorners = new Vector3[4];
            _rectTransform.GetWorldCorners(worldCorners);
            var topLeftCorner = worldCorners[1];
            var bottomRightCorner = worldCorners[3];

            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) {
                var camera = canvas.worldCamera;
                if (camera == null) {
                    WebViewLogger.LogError("Unable to determine the screen space rect for Native 2D Mode because the Canvas's render camera is not set. Please set the Canvas's \"Render Camera\" setting or change its render mode to \"Screen Space - Overlay\".");
                    return Rect.zero;
                } else {
                    topLeftCorner = camera.WorldToScreenPoint(topLeftCorner);
                    bottomRightCorner = camera.WorldToScreenPoint(bottomRightCorner);
                }
            }
            var x = topLeftCorner.x;
            var y = Screen.height - topLeftCorner.y;
            var width = bottomRightCorner.x - topLeftCorner.x;
            var height = topLeftCorner.y - bottomRightCorner.y;
            var scaleFactor = _getScreenSpaceScaleFactor();
            if (scaleFactor != 1f) {
                x *= scaleFactor;
                y *= scaleFactor;
                width *= scaleFactor;
                height *= scaleFactor;
            }
            return new Rect(x, y, width, height);
        }

        // Provides a scale factor to account for an issue where GetWorldCorners() is incorrect in the following scenarios:
        // - If the screen resolution is changed at runtime using Screen.SetResolution().
        // - If the "Resolution Scaling Mode" is set to "Fixed DPI" in Player Settings -> Resolution and Presentation (on Android).
        float _getScreenSpaceScaleFactor() {

            var display = Display.main;
            if (display.renderingWidth == display.systemHeight && display.renderingHeight == display.systemWidth) {
                // Some old versions of Unity (like 2019.4.33) have a bug in the Android player where after the device
                // is rotated, the Display's rendering width and height are swapped but the system width and height aren't.
                // Return 1 in that scenario to prevent computing an incorrect scale factor.
                return 1f;
            }
            // Notes:
            // - This approach doesn't work for detecting when "Resolution Scaling Mode" is set to "Fixed DPI" on iOS because
            //   display.systemWidth is equal to display.renderingWidth on iOS in that scenario. However, the native iOS plugin
            //   applies its own scale factor that works correctly for "Fixed DPI".
            // - In addition to checking `display.renderingWidth != display.systemWidth`, it's necessary to also check that
            //   `display.renderingHeight != display.systemHeight` to avoid issues where the width or height can be different in the
            //   the following scenarios due to screen notch:
            //       - If an Android device has a notch and the "Render outside safe area" option is disabled, then the renderingWidth (or height)
            //         will be equal to the Screen.safeArea.width (or height), which is less than the systemWidth (or height).
            //       - Some Huawei devices have a "Hide notch" setting that, when enabled, causes the display.systemWidth (or height) to be less
            //         than display.renderingWidth (or height).
            // - It's important to also check that `display.systemWidth != Screen.currentResolution.width` because on UWP,
            //   `display.renderingWidth != display.systemWidth` is true whenever the app's window isn't full screen, but
            //   display.systemWidth and Screen.currentResolution.width are still equal in that scenario.
            // - This method used to work by comparing the current Screen.currentResolution to the original value of
            //   Screen.currentResolution from when the app started, but that approach caused an issue on iPads because
            //   Screen.currentResolution changes when multiple apps are shown side-by-side with the iPad's Split View.
            var screenResolutionChanged = display.renderingWidth != display.systemWidth &&
                                          display.renderingHeight != display.systemHeight &&
                                          display.systemWidth != Screen.currentResolution.width;
            if (screenResolutionChanged) {
                float scaleFactor = (float)display.systemWidth / (float)display.renderingWidth;
                return scaleFactor;
            }
            return 1f;
        }

        protected override ViewportMaterialView _getVideoLayer() {

            var obj = transform.Find("VideoLayer");
            return obj == null ? null : obj.GetComponent<ViewportMaterialView>();
        }

        protected override ViewportMaterialView _getView() {

            var obj = transform.Find("CanvasWebViewPrefabView");
            return obj == null ? null : obj.GetComponent<ViewportMaterialView>();
        }

        async void _initCanvasPrefab() {
            try {
                OnInit();
                Initialized += _logNative2DRecommendationIfNeeded;
                _native2DModeEnabledAtInitialization = Native2DModeEnabled;
                var preferNative2DMode = Native2DModeEnabled && _canNative2DModeBeEnabled(true);
                var rect = _getRectForInitialization(preferNative2DMode);
                if (_sizeIsInvalid(rect.size)) {
                    // If the prefab is nested in a LayoutGroup, its width and height will be zero on the first frame,
                    // so it's necessary to pass the LayoutGroup's RectTransform LayoutRebuilder.ForceRebuildLayoutImmediate().
                    // https://forum.unity.com/threads/force-immediate-layout-update.372630
                    var layoutGroup = GetComponentInParent<LayoutGroup>();
                    if (layoutGroup != null) {
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layoutGroup.transform);
                        rect = _getRectForInitialization(preferNative2DMode);
                    }
                }
                if (_logErrorIfSizeIsInvalid(rect.size)) {
                    return;
                }
                await _initBase(rect, preferNative2DMode);
            } catch (Exception exception) {
                // Catch any exceptions that occur during initialization because
                // some applications terminate the application on uncaught exceptions.
                Debug.LogException(exception);
            }
        }

        void _logErrorIfNative2DModeEnabledChanged() {

            if (Native2DModeEnabled != _native2DModeEnabledAtInitialization) {
                Native2DModeEnabled = _native2DModeEnabledAtInitialization;
                WebViewLogger.LogError("The application tried to change the value of CanvasWebViewPrefab.Native2DModeEnabled after the prefab has initialized. Native2DModeEnabled can only be set prior to initialization. For more details, see this: https://developer.vuplex.com/webview/CanvasWebViewPrefab#Native2DModeEnabled");
            }
        }

        bool _logErrorIfSizeIsInvalid(Vector2 size) {

            if (_sizeIsInvalid(size)) {
                WebViewLogger.LogError($"CanvasWebViewPrefab dimensions are invalid! Width: {size.x.ToString("f4")}, Height: {size.y.ToString("f4")}. To correct this, please adjust the CanvasWebViewPrefab's RectTransform to make it so that its width and height are both greater than zero. https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-RectTransform.html");
                return true;
            }
            return false;
        }

        void _logNative2DModeWarning(string message) {

            WebViewLogger.LogWarning(message + " For more info, please see this article: <em>https://support.vuplex.com/articles/native-2d-mode</em>");
        }

        void _logNative2DRecommendationIfNeeded(object sender, EventArgs eventArgs) {

            var webViewWith2DMode = WebView as IWithNative2DMode;
            if (_canNative2DModeBeEnabled() && webViewWith2DMode != null && !webViewWith2DMode.Native2DModeEnabled) {
                WebViewLogger.LogTip("This platform supports Native 2D Mode, so consider enabling CanvasWebViewPrefab.Native2DModeEnabled for best results. For more info, see https://support.vuplex.com/articles/native-2d-mode .");
            }
        }

        void OnDisable() {

            // When in Native 2D Mode, hide the webview when the CanvasWebViewPrefab is deactivated.
            var webView = _getNative2DWebViewIfActive();
            if (webView != null) {
                webView.SetVisible(false);
            }
        }

        void OnEnable() {

            // When in Native 2D Mode, show the webview when the CanvasWebViewPrefab is activated.
            var webView = _getNative2DWebViewIfActive();
            if (webView != null) {
                webView.SetVisible(true);
            }
        }

        bool _resizeNative2DWebViewIfNeeded() {

            var native2DWebView = _getNative2DWebViewIfActive();
            if (native2DWebView == null) {
                return false;
            }
            var screenSpaceRect = _getScreenSpaceRect();
            if (native2DWebView.Rect != screenSpaceRect) {
                native2DWebView.SetRect(screenSpaceRect);
            }
            return true;
        }

        protected override void _setVideoLayerPosition(Rect videoRect) {

            var videoRectTransform = _videoLayer.transform as RectTransform;
            // Use Vector2.Scale() because Vector2 * Vector2 isn't supported in Unity 2017.
            videoRectTransform.anchoredPosition = Vector2.Scale(Vector2.Scale(videoRect.position, _rectTransform.rect.size), new Vector2(1, -1));
            videoRectTransform.sizeDelta = Vector2.Scale(videoRect.size, _rectTransform.rect.size);
        }

        bool _sizeIsInvalid(Vector2 size) => !(size.x > 0f && size.y > 0f);

        void Start() => _initCanvasPrefab();

        protected override void Update() {

            base.Update();
            if (WebView == null) {
                return;
            }
            _logErrorIfNative2DModeEnabledChanged();
            _sizeInUnityUnits = _rectTransform.rect.size;
            if (_logErrorIfSizeIsInvalid(_sizeInUnityUnits)) {
                return;
            }
            // Handle updating the rect for a native 2D webview.
            if (!_resizeNative2DWebViewIfNeeded()) {
                // Handle resizing a regular webview.
                _resizeWebViewIfNeeded();
            }
        }
    #endregion

    #region Obsolete APIs
        // Added in v3.2, removed in v3.12.
        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called.", true)]
        public void Init() {}

        // Added in v3.2, removed in v3.12.
        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called.", true)]
        public void Init(WebViewOptions options) {}

        // Added in v3.10, removed in v3.12.
        [Obsolete("CanvasWebViewPrefab.Init() has been removed. The CanvasWebViewPrefab script now initializes itself automatically, so Init() no longer needs to be called. Please use CanvasWebViewPrefab.SetWebViewForInitialization(IWebView) instead.", true)]
        public void Init(IWebView webView) {}

        // Deprecated in v4.0.
        [Obsolete("CanvasWebViewPrefab.InitialResolution is now deprecated. Please use CanvasWebViewPrefab.Resolution instead.")]
        public float InitialResolution {
            get { return Resolution; }
            set { Resolution = value; }
        }
    #endregion
    }
}
