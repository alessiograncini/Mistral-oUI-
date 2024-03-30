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
// Only define BaseWebView.cs on supported platforms to avoid IL2CPP linking
// errors on unsupported platforms.
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || (UNITY_IOS && !VUPLEX_OMIT_IOS) || UNITY_VISIONOS || (UNITY_WEBGL && !VUPLEX_OMIT_WEBGL) || UNITY_WSA
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// The base IWebView implementation, which is extended for each platform.
    /// </summary>
    public abstract class BaseWebView : MonoBehaviour {

        public event EventHandler CloseRequested;

        public event EventHandler<ConsoleMessageEventArgs> ConsoleMessageLogged {
            add {
                _consoleMessageLogged += value;
                if (_consoleMessageLogged != null && _consoleMessageLogged.GetInvocationList().Length == 1) {
                    _setConsoleMessageEventsEnabled(true);
                }
            }
            remove {
                _consoleMessageLogged -= value;
                if (_consoleMessageLogged == null) {
                    _setConsoleMessageEventsEnabled(false);
                }
            }
        }

        public event EventHandler<EventArgs<bool>> FocusChanged;

        public event EventHandler<FocusedInputFieldChangedEventArgs> FocusedInputFieldChanged {
            add {
                _focusedInputFieldChanged += value;
                if (_focusedInputFieldChanged != null && _focusedInputFieldChanged.GetInvocationList().Length == 1) {
                    _setFocusedInputFieldEventsEnabled(true);
                }
            }
            remove {
                _focusedInputFieldChanged -= value;
                if (_focusedInputFieldChanged == null) {
                    _setFocusedInputFieldEventsEnabled(false);
                }
            }
        }

        public event EventHandler<LoadFailedEventArgs> LoadFailed;

        public event EventHandler<ProgressChangedEventArgs> LoadProgressChanged;

        public event EventHandler<EventArgs<string>> MessageEmitted;

        public event EventHandler<TerminatedEventArgs> Terminated;

        public event EventHandler<EventArgs<string>> TitleChanged;

        public event EventHandler<UrlChangedEventArgs> UrlChanged;

        public bool IsDisposed { get; protected set; }

        public bool IsInitialized { get { return _initState == InitState.Initialized; }}

        public List<string> PageLoadScripts { get; } = new List<string>();

        public Vector2Int Size { get; private set; }

        public Texture2D Texture { get; protected set; }

        public string Title { get; private set; } = "";

        public string Url { get; private set; } = "";

        public virtual Task<bool> CanGoBack() {

            _assertValidState();
            var taskSource = new TaskCompletionSource<bool>();
            _pendingCanGoBackCallbacks.Add(taskSource.SetResult);
            WebView_canGoBack(_nativeWebViewPtr);
            return taskSource.Task;
        }

        public virtual Task<bool> CanGoForward() {

            _assertValidState();
            var taskSource = new TaskCompletionSource<bool>();
            _pendingCanGoForwardCallbacks.Add(taskSource.SetResult);
            WebView_canGoForward(_nativeWebViewPtr);
            return taskSource.Task;
        }

        public virtual Task<byte[]> CaptureScreenshot() {

            var texture = _getReadableTexture();
            var bytes = ImageConversion.EncodeToPNG(texture);
            Destroy(texture);
            return Task.FromResult(bytes);
        }

        public virtual void Click(int xInPixels, int yInPixels, bool preventStealingFocus = false) {

            _assertValidState();
            _assertPointIsWithinBounds(xInPixels, yInPixels);
            // On most platforms, the regular Click() method doesn't steal focus,
            // So, the default is to ignore preventStealingFocus.
            WebView_click(_nativeWebViewPtr, xInPixels, yInPixels);
        }

        public void Click(Vector2 normalizedPoint, bool preventStealingFocus = false) {

            _assertValidState();
            var pixelsPoint = _normalizedToPointAssertValid(normalizedPoint);
            Click(pixelsPoint.x, pixelsPoint.y, preventStealingFocus);
        }

        public virtual async void Copy() {

            _assertValidState();
            GUIUtility.systemCopyBuffer = await _getSelectedText();
        }

        public virtual Material CreateMaterial() {

            if (_native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("CreateMaterial", "will return null");
                return null;
            }
            var material = _createMaterial();
            material.mainTexture = Texture;
            return material;
        }

        public virtual async void Cut() {

            _assertValidState();
            GUIUtility.systemCopyBuffer = await _getSelectedText();
            SendKey("Backspace");
        }

        public virtual void Dispose() {

            _assertValidState();
            IsDisposed = true;
            WebView_destroy(_nativeWebViewPtr);
            _nativeWebViewPtr = IntPtr.Zero;
            // To avoid a MissingReferenceException, verify that this script
            // hasn't already been destroyed prior to accessing gameObject.
            if (this != null) {
                Destroy(gameObject);
            }
        }

        public Task<string> ExecuteJavaScript(string javaScript) {

            var taskSource = new TaskCompletionSource<string>();
            ExecuteJavaScript(javaScript, taskSource.SetResult);
            return taskSource.Task;
        }

        public virtual void ExecuteJavaScript(string javaScript, Action<string> callback) {

            _assertValidState();
            string resultCallbackId = null;
            if (callback != null) {
                resultCallbackId = Guid.NewGuid().ToString();
                _pendingJavaScriptResultCallbacks[resultCallbackId] = callback;
            }
            WebView_executeJavaScript(_nativeWebViewPtr, javaScript, resultCallbackId);
        }

        public virtual Task<byte[]> GetRawTextureData() {

            var texture = _getReadableTexture();
            var bytes = texture.GetRawTextureData();
            Destroy(texture);
            return Task.FromResult(bytes);
        }

        public virtual void GoBack() {

            _assertValidState();
            WebView_goBack(_nativeWebViewPtr);
        }

        public virtual void GoForward() {

            _assertValidState();
            WebView_goForward(_nativeWebViewPtr);
        }

        public virtual void LoadHtml(string html) {

            _assertValidState();
            WebView_loadHtml(_nativeWebViewPtr, html);
        }

        public virtual void LoadUrl(string url) {

            _assertValidState();
            WebView_loadUrl(_nativeWebViewPtr, _transformUrlIfNeeded(url));
        }

        public virtual void LoadUrl(string url, Dictionary<string, string> additionalHttpHeaders) {

            _assertValidState();
            if (additionalHttpHeaders == null) {
                LoadUrl(url);
            } else {
                var headerStrings = additionalHttpHeaders.Keys.Select(key => $"{key}: {additionalHttpHeaders[key]}").ToArray();
                var newlineDelimitedHttpHeaders = String.Join("\n", headerStrings);
                WebView_loadUrlWithHeaders(_nativeWebViewPtr, _transformUrlIfNeeded(url), newlineDelimitedHttpHeaders);
            }
        }

        public Vector2Int NormalizedToPoint(Vector2 normalizedPoint) {

            return new Vector2Int(
                (int)Math.Round(normalizedPoint.x * (float)Size.x),
                (int)Math.Round(normalizedPoint.y * (float)Size.y)
            );
        }

        public virtual void Paste() {

            _assertValidState();
            var text = GUIUtility.systemCopyBuffer;
            foreach (var character in text) {
                SendKey(char.ToString(character));
            }
        }

        public Vector2 PointToNormalized(int xInPixels, int yInPixels) {

            return new Vector2((float)xInPixels / (float)Size.x, (float)yInPixels / (float)Size.y);
        }

        public virtual void PostMessage(string message) {

            var escapedString = message.Replace("\\", "\\\\")
                                       .Replace("'", "\\'")
                                       .Replace("\n", "\\n")
                                       .Replace("\r", "\\r");
            ExecuteJavaScript($"vuplex._emit('message', {{ data: '{escapedString}' }})", null);
        }

        public virtual void Reload() {

            _assertValidState();
            WebView_reload(_nativeWebViewPtr);
        }

        public virtual void Resize(int width, int height) {

            if (width == Size.x && height == Size.y) {
                return;
            }
            _assertValidState();
            _assertValidSize(width, height);
            _warnIfAbnormallyLarge(width, height);
            Size = new Vector2Int(width, height);
            _resize();
        }

        public virtual void Scroll(int scrollDeltaXInPixels, int scrollDeltaYInPixels) {

            _assertValidState();
            WebView_scroll(_nativeWebViewPtr, scrollDeltaXInPixels, scrollDeltaYInPixels);
        }

        public void Scroll(Vector2 normalizedScrollDelta) {

            _assertValidState();
            var scrollDeltaInPixels = NormalizedToPoint(normalizedScrollDelta);
            Scroll(scrollDeltaInPixels.x, scrollDeltaInPixels.y);
        }

        public virtual void Scroll(Vector2 normalizedScrollDelta, Vector2 normalizedPoint) {

            _assertValidState();
            var scrollDeltaInPixels = NormalizedToPoint(normalizedScrollDelta);
            var pointInPixels = _normalizedToPointAssertValid(normalizedPoint);
            WebView_scrollAtPoint(_nativeWebViewPtr, scrollDeltaInPixels.x, scrollDeltaInPixels.y, pointInPixels.x, pointInPixels.y);
        }

        public virtual void SelectAll() {

            _assertValidState();
            // If the focused element is an input with a select() method, then use that.
            // Otherwise, travel up the DOM until we get to the body or a contenteditable
            // element, and then select its contents.
            ExecuteJavaScript(
                @"(function() {
                    var element = document.activeElement || document.body;
                    while (!(element === document.body || element.getAttribute('contenteditable') === 'true')) {
                        if (typeof element.select === 'function') {
                            element.select();
                            return;
                        }
                        element = element.parentElement;
                    }
                    var range = document.createRange();
                    range.selectNodeContents(element);
                    var selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(range);
                })();",
                null
            );
        }

        public virtual void SendKey(string key) {

            _assertValidState();
            WebView_sendKey(_nativeWebViewPtr, key);
        }

        public static void SetCameraAndMicrophoneEnabled(bool enabled) => WebView_setCameraAndMicrophoneEnabled(enabled);

        public virtual void SetDefaultBackgroundEnabled(bool enabled) {

            _assertValidState();
            WebView_setDefaultBackgroundEnabled(_nativeWebViewPtr, enabled);
        }

        public virtual void SetFocused(bool focused) {

            _assertValidState();
            WebView_setFocused(_nativeWebViewPtr, focused);
            FocusChanged?.Invoke(this, new EventArgs<bool>(focused));
        }

        public virtual void SetRenderingEnabled(bool enabled) {

            _assertValidState();
            if (_native2DModeEnabled) {
                VXUtils.LogNative2DModeWarning("SetRenderingEnabled");
                return;
            }
            WebView_setRenderingEnabled(_nativeWebViewPtr, enabled);
            _renderingEnabled = enabled;
        }

        public virtual void StopLoad() {

            _assertValidState();
            WebView_stopLoad(_nativeWebViewPtr);
        }

        public Task WaitForNextPageLoadToFinish() {

            if (_pageLoadFinishedTaskSource == null) {
                _pageLoadFinishedTaskSource = new TaskCompletionSource<bool>();
            }
            return _pageLoadFinishedTaskSource.Task;
        }

        public virtual void ZoomIn() {

            _assertValidState();
            WebView_zoomIn(_nativeWebViewPtr);
        }

        public virtual void ZoomOut() {

            _assertValidState();
            WebView_zoomOut(_nativeWebViewPtr);
        }

    #region Non-public members
        protected enum InitState {
            Uninitialized,
            InProgress,
            Initialized
        }

        // Anything over 19.4 megapixels (6k) is almost certainly a mistake.
        protected virtual int _abnormallyLargeThreshold { get { return 19400000; }}
        EventHandler<ConsoleMessageEventArgs> _consoleMessageLogged;
        protected IntPtr _currentNativeTexture;

    #if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN
        protected const string _dllName = "VuplexWebViewWindows";
    #elif (UNITY_STANDALONE_OSX && !UNITY_EDITOR) || UNITY_EDITOR_OSX
        protected const string _dllName = "VuplexWebViewMac";
    #elif UNITY_WSA
        protected const string _dllName = "VuplexWebViewUwp";
    #elif UNITY_ANDROID
        protected const string _dllName = "VuplexWebViewAndroid";
    #else
        protected const string _dllName = "__Internal";
    #endif

        EventHandler<FocusedInputFieldChangedEventArgs> _focusedInputFieldChanged;
        protected InitState _initState = InitState.Uninitialized;
        TaskCompletionSource<bool> _initTaskSource;
        Material _materialForBlitting;
        protected bool _native2DModeEnabled;  // Used for Native 2D Mode.
        protected Vector2Int _native2DPosition; // Used for Native 2D Mode.
        protected IntPtr _nativeWebViewPtr;
        TaskCompletionSource<bool> _pageLoadFinishedTaskSource;
        List<Action<bool>> _pendingCanGoBackCallbacks = new List<Action<bool>>();
        List<Action<bool>> _pendingCanGoForwardCallbacks = new List<Action<bool>>();
        protected Dictionary<string, Action<string>> _pendingJavaScriptResultCallbacks = new Dictionary<string, Action<string>>();
        protected bool _renderingEnabled = true;
        // Used for Native 2D Mode. Use Size as the single source of truth for the size
        // to ensure that both Size and Rect stay in sync when Resize() or SetRect() is called.
        protected Rect _rect {
            get { return new Rect(_native2DPosition, Size); }
            set {
                Size = new Vector2Int((int)value.width, (int)value.height);
                _native2DPosition = new Vector2Int((int)value.x, (int)value.y);
            }
        }
        static string[] STANDARD_URI_SCHEMES = new string[] { "http:", "https:", "file:", "about:" };
        static readonly Regex _streamingAssetsUrlRegex = new Regex(@"^streaming-assets:(//)?(.*)$", RegexOptions.IgnoreCase);

        protected void _assertNative2DModeEnabled() {

            if (!_native2DModeEnabled) {
                throw new InvalidOperationException("IWithNative2DMode methods can only be called on a webview with Native 2D Mode enabled.");
            }
        }

        protected void _assertPointIsWithinBounds(int xInPixels, int yInPixels) {

            var isValid = xInPixels >= 0 && xInPixels <= Size.x && yInPixels >= 0 && yInPixels <= Size.y;
            if (!isValid) {
                throw new ArgumentException($"The point provided ({xInPixels}px, {yInPixels}px) is not within the bounds of the webview (width: {Size.x}px, height: {Size.y}px).");
            }
        }

        protected void _assertSingletonEventHandlerUnset(object handler, string eventName) {

            if (handler != null) {
                throw new InvalidOperationException(eventName + " supports only one event handler. Please remove the existing handler before adding a new one.");
            }
        }

        void _assertSupportedGraphicsApi() {

            var supportedApis = _getSupportedGraphicsApis();
            if (supportedApis == null) {
                // Graphics API validation is disabled for this platform.
                return;
            }
            var isValid = supportedApis.ToList().Contains(SystemInfo.graphicsDeviceType);
            if (isValid) {
                return;
            }
            var listOfSupportedApis = supportedApis.Length == 1 ? supportedApis[0].ToString() : $"one of the following: {String.Join(", ", supportedApis)}";
            var message = $"Unsupported Graphics API: The Graphics API is set to {SystemInfo.graphicsDeviceType}, which 3D WebView doesn't support on this platform. Please go to Player Settings -> Other Settings and set the Graphics API to {listOfSupportedApis}";
            WebViewLogger.LogError(message);
            throw new InvalidOperationException(message);
        }

        void _assertValidSize(int width, int height) {

            if (!(width > 0 && height > 0)) {
                throw new ArgumentException($"Invalid size: ({width}, {height}). The width and height must both be greater than 0.");
            }
        }

        protected void _assertValidState() {

            if (!IsInitialized) {
                throw new InvalidOperationException("Methods cannot be called on an uninitialized webview. Prior to calling the webview's methods, please initialize it first by calling IWebView.Init() and awaiting the Task it returns.");
            }
            if (IsDisposed) {
                throw new InvalidOperationException("Methods cannot be called on a disposed webview.");
            }
        }

        protected Vector2Int _normalizedToPointAssertValid(Vector2 normalizedPoint) {

            var isValid = normalizedPoint.x >= 0f && normalizedPoint.x <= 1f && normalizedPoint.y >= 0f && normalizedPoint.y <= 1f;
            if (isValid) {
                return NormalizedToPoint(normalizedPoint);
            }
            throw new ArgumentException($"The normalized point provided is invalid. The x and y values of normalized points must be in the range of [0, 1], but the value provided was {normalizedPoint.ToString("n4")}. For more info, please see https://support.vuplex.com/articles/normalized-points");
        }

        protected virtual Material _createMaterial() => VXUtils.CreateDefaultMaterial();

        protected virtual Task<Texture2D> _createTexture(int width, int height) {

            _warnIfAbnormallyLarge(width, height);
            var textureFormat = _getTextureFormat();
            var texture = new Texture2D(
                width,
                height,
                textureFormat,
                false,
                false
            );
            #if UNITY_2020_2_OR_NEWER
                var originalTexture = texture;
                // In Unity 2020.2, Unity's internal TexturesD3D11.cpp class on Windows logs an error if
                // UpdateExternalTexture() is called on a Texture2D created from the constructor
                // rather than from Texture2D.CreateExternalTexture(). So, rather than returning
                // the original Texture2D created via the constructor, we return a copy created
                // via CreateExternalTexture(). This approach is only used for 2020.2 and newer because
                // it doesn't work in 2018.4 and instead causes a crash.
                texture = Texture2D.CreateExternalTexture(
                    width,
                    height,
                    textureFormat,
                    false,
                    false,
                    originalTexture.GetNativeTexturePtr()
                );
                // Destroy the original texture so that its memory is released.
                Destroy(originalTexture);
            #endif
            return Task.FromResult(texture);
        }

        protected virtual void _destroyNativeTexture(IntPtr nativeTexture) {

            WebView_destroyTexture(nativeTexture, SystemInfo.graphicsDeviceType.ToString());
        }

        Texture2D _getReadableTexture() {

            // https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
            RenderTexture tempRenderTexture = RenderTexture.GetTemporary(
                Size.x,
                Size.y,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear
            );
            RenderTexture previousRenderTexture = RenderTexture.active;
            RenderTexture.active = tempRenderTexture;
            // Explicitly clear the temporary render texture, otherwise it can contain
            // existing content that won't be overwritten by transparent pixels.
            GL.Clear(true, true, Color.clear);
            // Use the version of Graphics.Blit() that accepts a material
            // so that any transformations needed are performed with the shader.
            if (_materialForBlitting == null) {
                _materialForBlitting = _createMaterial();
            }
            Graphics.Blit(Texture, tempRenderTexture, _materialForBlitting);
            Texture2D readableTexture = new Texture2D(Size.x, Size.y, TextureFormat.RGBA32, false);
            readableTexture.ReadPixels(new Rect(0, 0, tempRenderTexture.width, tempRenderTexture.height), 0, 0);
            readableTexture.Apply();
            RenderTexture.active = previousRenderTexture;
            RenderTexture.ReleaseTemporary(tempRenderTexture);
            return readableTexture;
        }

        Task<string> _getSelectedText() {

            // window.getSelection() doesn't work on the content of <textarea> and <input> elements in
            // Gecko and legacy Edge.
            // https://developer.mozilla.org/en-US/docs/Web/API/Window/getSelection#Related_objects
            return ExecuteJavaScript(
                @"var element = document.activeElement;
                if (element instanceof HTMLInputElement || element instanceof HTMLTextAreaElement) {
                    element.value.substring(element.selectionStart, element.selectionEnd);
                } else {
                    window.getSelection().toString();
                }"
            );
        }

        protected virtual GraphicsDeviceType[] _getSupportedGraphicsApis() => null;

        protected virtual TextureFormat _getTextureFormat() => TextureFormat.RGBA32;

        // Invoked by the native plugin.
        void HandleCanGoBackResult(string message) {

            var result = Boolean.Parse(message);
            var callbacks = new List<Action<bool>>(_pendingCanGoBackCallbacks);
            _pendingCanGoBackCallbacks.Clear();
            foreach (var callback in callbacks) {
                try {
                    callback(result);
                } catch (Exception e) {
                    WebViewLogger.LogError("An exception occurred while calling the callback for CanGoBack: " + e);
                }
            }
        }

        // Invoked by the native plugin.
        void HandleCanGoForwardResult(string message) {

            var result = Boolean.Parse(message);
            var callBacks = new List<Action<bool>>(_pendingCanGoForwardCallbacks);
            _pendingCanGoForwardCallbacks.Clear();
            foreach (var callBack in callBacks) {
                try {
                    callBack(result);
                } catch (Exception e) {
                    WebViewLogger.LogError("An exception occurred while calling the callForward for CanGoForward: " + e);
                }
            }
        }

        // Invoked by the native plugin.
        void HandleCloseRequested(string message) => CloseRequested?.Invoke(this, EventArgs.Empty);

        // Invoked by the native plugin.
        void HandleFocusedInputFieldChanged(string typeString) {

            var type = FocusedInputFieldChangedEventArgs.ParseType(typeString);
            _focusedInputFieldChanged?.Invoke(this, new FocusedInputFieldChangedEventArgs(type));
        }

        // Invoked by the native plugin.
        protected void HandleInitFinished(string unusedParam) {

            _initState = InitState.Initialized;
            var taskSource = _initTaskSource;
            _initTaskSource = null;
            // Call TaskCompletionSource.SetResult() last because any code awaiting the task will execute immediately when it's called.
            taskSource.SetResult(true);
        }

        // Invoked by the native plugin.
        void HandleJavaScriptResult(string message) {

            var components = message.Split(new char[] { ',' }, 2);
            var resultCallbackId = components[0];
            var result = components[1];
            _handleJavaScriptResult(resultCallbackId, result);
        }

        void _handleJavaScriptResult(string resultCallbackId, string result) {

            var callback = _pendingJavaScriptResultCallbacks[resultCallbackId];
            _pendingJavaScriptResultCallbacks.Remove(resultCallbackId);
            callback(result);
        }

        // Invoked by the native plugin.
        void HandleLoadFailed(string param) {

            var parameters = param.Split(new []{','}, 2);
            var nativeErrorCode = parameters[0];
            var url = parameters[1];
            var eventArgs = new LoadFailedEventArgs(nativeErrorCode, url);
            LoadFailed?.Invoke(this, eventArgs);
            OnLoadProgressChanged(new ProgressChangedEventArgs(ProgressChangeType.Failed, 1.0f));
            var taskSource = _pageLoadFinishedTaskSource;
            _pageLoadFinishedTaskSource = null;
            if (LoadFailed == null && LoadProgressChanged == null) {
                // No handlers are attached to LoadFailed or LoadProgressChanged,
                // so log a warning about the page load failure.
                WebViewLogger.LogWarning("A web page failed to load. This can happen if the URL loaded is invalid or if the device has no network connection. To detect and handle page load failures like this, applications can use the IWebView.LoadFailed event or the IWebView.LoadProgressChanged event. You can disable this warning message by attaching an event handler to one of those events. Load failure details: " + eventArgs);
                if (Application.internetReachability == NetworkReachability.NotReachable) {
                    WebViewLogger.LogError("The device is not connected to the internet (Application.internetReachability == NetworkReachability.NotReachable).");
                }
            }
            // Call TaskCompletionSource.SetException() last because any code awaiting the task will execute immediately when it's called.
            taskSource?.SetException(new PageLoadFailedException("The current web page failed to load."));
        }

        // Invoked by the native plugin.
        void HandleLoadFinished(string unusedParam) {

            OnLoadProgressChanged(new ProgressChangedEventArgs(ProgressChangeType.Finished, 1.0f));
            var taskSource = _pageLoadFinishedTaskSource;
            _pageLoadFinishedTaskSource = null;
            foreach (var script in PageLoadScripts) {
                ExecuteJavaScript(script, null);
            }
            // Call TaskCompletionSource.SetResult() last because any code awaiting the task will execute immediately when it's called.
            taskSource?.SetResult(true);
        }

        // Invoked by the native plugin.
        void HandleLoadStarted(string unusedParam) {

            OnLoadProgressChanged(new ProgressChangedEventArgs(ProgressChangeType.Started, 0.0f));
        }

        // Invoked by the native plugin.
        void HandleLoadProgressUpdate(string progressString) {

            var progress = float.Parse(progressString, CultureInfo.InvariantCulture);
            OnLoadProgressChanged(new ProgressChangedEventArgs(ProgressChangeType.Updated, progress));
        }

        // Invoked by the native plugin.
        protected virtual void HandleMessageEmitted(string serializedMessage) {

            // For performance, only try to deserialize the message if it's one we're listening for.
            var messageType = serializedMessage.Contains("vuplex.webview") ? BridgeMessage.ParseType(serializedMessage) : null;
            switch (messageType) {
                case "vuplex.webview.consoleMessageLogged": {
                    var consoleMessage = JsonUtility.FromJson<ConsoleBridgeMessage>(serializedMessage);
                    _consoleMessageLogged?.Invoke(this, consoleMessage.ToEventArgs());
                    break;
                }
                case "vuplex.webview.focusedInputFieldChanged": {
                    var type = StringBridgeMessage.ParseValue(serializedMessage);
                    HandleFocusedInputFieldChanged(type);
                    break;
                }
                case "vuplex.webview.javaScriptResult": {
                    var message = JsonUtility.FromJson<StringWithIdBridgeMessage>(serializedMessage);
                    _handleJavaScriptResult(message.id, message.value);
                    break;
                }
                case "vuplex.webview.titleChanged": {
                    Title = StringBridgeMessage.ParseValue(serializedMessage);
                    TitleChanged?.Invoke(this, new EventArgs<string>(Title));
                    break;
                }
                case "vuplex.webview.transparencyBlockedWarning": {
                    var reason = StringBridgeMessage.ParseValue(serializedMessage);
                    WebViewLogger.LogWarning($"Transparency has been enabled for the webview, but the web page's CSS explicitly sets a background, so the page probably won't be transparent as expected. Diagnosis: {reason} For more info, please see this page: https://support.vuplex.com/articles/how-to-make-a-webview-transparent#troubleshooting");
                    break;
                }
                case "vuplex.webview.urlChanged": {
                    _handleUrlChanged(serializedMessage);
                    break;
                }
                default: {
                    MessageEmitted?.Invoke(this, new EventArgs<string>(serializedMessage));
                    break;
                }
            }
        }

        // Invoked by the native plugin.
        void HandleTerminated(string typeString) {

            TerminationType type = TerminationType.Unknown;
            switch (typeString) {
                case "CRASHED":
                    type = TerminationType.Crashed;
                    break;
                case "KILLED":
                    type = TerminationType.Killed;
                    break;
                case "UNKNOWN":
                    type = TerminationType.Unknown;
                    break;
                default:
                    WebViewLogger.LogError("Unrecognized termination type: " + typeString);
                    break;
            }
            Terminated?.Invoke(this, new TerminatedEventArgs(type));
            if (Terminated == null) {
                WebViewLogger.LogError($"The browser engine indicated that the browser's web content process terminated. Reason: {type}. You can detect and handle this condition using the IWebView.Terminated event. This message was logged because the application hasn't attached a handler to the Terminated event. For more details, please see this page: https://developer.vuplex.com/webview/IWebView#Terminated");
            }
        }

        // Invoked by the native plugin.
        virtual protected void HandleTextureChanged(string paramString) {

            var parameters = paramString.Split(new char[] { ',' }, 3);
            var textureString = parameters[0];
            // Use UInt64.Parse() because Int64.Parse() can result in an OverflowException.
            var nativeTexture = new IntPtr((Int64)UInt64.Parse(textureString));
            if (nativeTexture == _currentNativeTexture) {
                return;
            }
            var previousNativeTexture = _currentNativeTexture;
            _currentNativeTexture = nativeTexture;
            if (parameters.Length >= 3) {
                // On Windows, the plugin also emits the texture width and height so that Texture.Reinitialize() can be called here to avoid the
                // following warnings that are logged in the Unity 2022.3 Editor:
                // > Registering a native texture with width={...} while the actual texture has width={...}
                // > Registering a native texture with height={...} while the actual texture has height={...}
                var width = int.Parse(parameters[1]);
                var height = int.Parse(parameters[2]);
                #if UNITY_2021_2_OR_NEWER
                    Texture.Reinitialize(width, height);
                #endif
            }
            Texture.UpdateExternalTexture(nativeTexture);
            if (previousNativeTexture != IntPtr.Zero) {
                _destroyNativeTexture(previousNativeTexture);
            }
        }

        void _handleUrlChanged(string serializedMessage) {

            var action = JsonUtility.FromJson<UrlChangedMessage>(serializedMessage).urlAction;
            if (Url == action.Url) {
                return;
            }
            // Custom URI schemes are only be emitted via IWebView.UrlChanged but not set as IWebView.Url.
            var isCustomUriScheme = !STANDARD_URI_SCHEMES.Any(scheme => action.Url.StartsWith(scheme));
            if (!isCustomUriScheme) {
                Url = action.Url;
            }

            if (action.Url.StartsWith("https://accounts.google.com/v3/signin/rejected")) {
                WebViewLogger.LogError(
                    @"Google tries to block WebViews from signing into Google accounts, but a workaround is to use Web.SetUserAgent() to change the browser's User-Agent in Awake(), like this:
                    void Awake() {
                        #if UNITY_STANDALONE || UNITY_EDITOR
                            // On Windows and macOS, change the User-Agent to mobile:
                            Web.SetUserAgent(true);
                        #elif UNITY_IOS
                            // On iOS, change the User-Agent to desktop:
                            Web.SetUserAgent(false);
                        #else
                            // Otherwise, change the User-Agent to a recent version of FireFox (Google blocks older versions).
                            var firefox100ReleaseDate = DateTime.Parse(""2022-05-03"");
                            var currentVersion = 100 + ((DateTime.Now.Year - firefox100ReleaseDate.Year) * 12) + DateTime.Now.Month - firefox100ReleaseDate.Month;
                            Web.SetUserAgent($""Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:{currentVersion}.0) Gecko/20100101 Firefox/{currentVersion}.0"");
                        #endif
                    }"
                );
            }

            UrlChanged?.Invoke(this, new UrlChangedEventArgs(action.Url, action.Type));
        }

        protected async Task<Task> _initBase(int width, int height, bool createTexture = true, bool asyncInit = false) {

            if (_initState != InitState.Uninitialized) {
                var message = _initState == InitState.Initialized ? "Init() cannot be called on a webview that has already been initialized."
                                                                  : "Init() cannot be called on a webview that is already in the process of initialization.";
                throw new InvalidOperationException(message);
            }
            _assertValidSize(width, height);
            // Assign the game object a unique name so that the native view can send it messages.
            gameObject.name = "WebView-" + Guid.NewGuid().ToString();
            Size = new Vector2Int(width, height);
            _warnIfAbnormallyLarge(width, height);
            // Prevent the script from automatically being destroyed when a new scene is loaded.
            DontDestroyOnLoad(gameObject);
            if (createTexture) {
                // The Graphics API only needs to be validated in 3D rendering mode.
                _assertSupportedGraphicsApi();
                Texture = await _createTexture(width, height);
            }
            if (asyncInit) {
                _initState = InitState.InProgress;
                _initTaskSource = new TaskCompletionSource<bool>();
                return _initTaskSource.Task;
            } else {
                _initState = InitState.Initialized;
                return null;
            }
        }

        static void _logSystemInfoIfNeeded() {

            #if !UNITY_EDITOR
                var info = new System.Collections.Specialized.OrderedDictionary() {
                    ["Unity version"] = Application.unityVersion,
                    ["Development build"] = Debug.isDebugBuild,
                    ["OS version"] = SystemInfo.operatingSystem,
                    ["Device model"] = SystemInfo.deviceModel,
                    ["Graphics API"] = SystemInfo.graphicsDeviceType,
                #if UNITY_2019_3_OR_NEWER
                    ["Rendering threading mode"] = SystemInfo.renderingThreadingMode,
                #endif
                    ["Render pipeline"] = GraphicsSettings.renderPipelineAsset?.ToString() ?? "default",
                #if UNITY_2018_2_OR_NEWER
                    ["SRP Batcher"] = GraphicsSettings.useScriptableRenderPipelineBatching,
                #endif
                };
                var infoString = String.Join("\n", info.Keys.Cast<string>().Select(key => $"{key}: {info[key]}"));
                WebViewLogger.Log("System info (used by Vuplex support):\n" + infoString);
            #endif
        }

        protected virtual void OnLoadProgressChanged(ProgressChangedEventArgs eventArgs) => LoadProgressChanged?.Invoke(this, eventArgs);

        protected ConsoleMessageLevel _parseConsoleMessageLevel(string levelString) {

            switch (levelString) {
                case "DEBUG":
                    return ConsoleMessageLevel.Debug;
                case "ERROR":
                    return ConsoleMessageLevel.Error;
                case "LOG":
                    return ConsoleMessageLevel.Log;
                case "WARNING":
                    return ConsoleMessageLevel.Warning;
                default:
                    WebViewLogger.LogWarning("Unrecognized console message level: " + levelString);
                    return ConsoleMessageLevel.Log;
            }
        }

        protected virtual void _resize() => WebView_resize(_nativeWebViewPtr, Size.x, Size.y);

        protected virtual void _setConsoleMessageEventsEnabled(bool enabled) {

            _assertValidState();
            WebView_setConsoleMessageEventsEnabled(_nativeWebViewPtr, enabled);
        }

        protected virtual void _setFocusedInputFieldEventsEnabled(bool enabled) {

            _assertValidState();
            WebView_setFocusedInputFieldEventsEnabled(_nativeWebViewPtr, enabled);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void _staticInit() {

            #if !(NET_4_6 || NET_STANDARD_2_0)
                WebViewLogger.LogError("Support for the legacy .NET 3.5 runtime was removed in 3D WebView v4.0. Please switch to the .NET 4.x runtime.");
            #endif
            Application.lowMemory += () => WebViewLogger.LogWarning("Low memory warning: Application.lowMemory indicated that the app has been notified of low memory. For tips on reducing memory usage, please see this article: https://support.vuplex.com/articles/how-to-reduce-memory");
            _logSystemInfoIfNeeded();
        }

        protected string _transformUrlIfNeeded(string originalUrl) {

            if (originalUrl == null) {
                throw new ArgumentException("URL cannot be null.");
            }
            // Default to https:// if no protocol is specified.
            if (!originalUrl.Contains(":")) {
                if (!originalUrl.Contains(".")) {
                    // The URL doesn't include a valid domain, so throw instead of defaulting to https://.
                    throw new ArgumentException("Invalid URL: " + originalUrl);
                }
                var updatedUrl = "https://" + originalUrl;
                WebViewLogger.LogWarning($"The provided URL is missing a protocol (e.g. http://, https://), so it will default to https://. Original URL: {originalUrl}, Updated URL: {updatedUrl}");
                return updatedUrl;
            }
            // If a streaming-assets:// URL was specified, so transform it to a URL that the browser can load.
            var streamingAssetsRegexMatch = _streamingAssetsUrlRegex.Match(originalUrl);
            if (streamingAssetsRegexMatch.Success) {
                var urlPath = streamingAssetsRegexMatch.Groups[2].Captures[0].Value;
                // If Application.streamingAssetsPath doesn't already contain a URL protocol, then add
                // the file:// protocol. It already has a protocol in the case of WebGL (http(s)://)
                // and Android (jar:file://).
                var urlProtocolToAdd = Application.streamingAssetsPath.Contains("://") ? "" : "file://";
                // Spaces in URLs must be escaped
                var streamingAssetsUrl = urlProtocolToAdd + Path.Combine(Application.streamingAssetsPath, urlPath).Replace(" ", "%20");
                return streamingAssetsUrl;
            }
            return originalUrl;
        }

        void _warnIfAbnormallyLarge(int width, int height) {

            // Cast to floats to avoid integer overflow.
            if ((float)width * (float)height > _abnormallyLargeThreshold) {
                var message = $"The application specified an abnormally large webview size for this platform ({width}px x {height}px). Webviews of this size are normally only created by mistake and may lead to issues, such as the webview not rendering or the app crashing. To avoid this issue, please reduce the Resolution of your WebViewPrefab or CanvasWebViewPrefab: https://developer.vuplex.com/webview/WebViewPrefab#Resolution .";
                WebViewLogger.LogWarning(message);
                // In the Editor, throw an exception to prevent a graphics error from crashing the Editor.
                #if UNITY_EDITOR && !VUPLEX_ALLOW_LARGE_WEBVIEWS
                    throw new ArgumentException(message + " This exception is thrown while running in the Editor in order to prevent the Editor from crashing due to a graphics error. If this large webview size is intentional, you can disable this exception by adding the scripting symbol VUPLEX_ALLOW_LARGE_WEBVIEWS to player settings. However, please note that if the webview size is larger than the graphics system can handle, the Editor may crash.");
                #endif
            }
        }

        [DllImport(_dllName)]
        static extern void WebView_canGoBack(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_canGoForward(IntPtr webViewPtr);

        [DllImport(_dllName)]
        protected static extern void WebView_click(IntPtr webViewPtr, int x, int y);

        [DllImport(_dllName)]
        protected static extern void WebView_destroyTexture(IntPtr texture, string graphicsApi);

        [DllImport(_dllName)]
        static extern void WebView_destroy(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_executeJavaScript(IntPtr webViewPtr, string javaScript, string resultCallbackId);

        [DllImport(_dllName)]
        static extern void WebView_goBack(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_goForward(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_sendKey(IntPtr webViewPtr, string input);

        [DllImport(_dllName)]
        static extern void WebView_loadHtml(IntPtr webViewPtr, string html);

        [DllImport(_dllName)]
        static extern void WebView_loadUrl(IntPtr webViewPtr, string url);

        [DllImport(_dllName)]
        static extern void WebView_loadUrlWithHeaders(IntPtr webViewPtr, string url, string newlineDelimitedHttpHeaders);

        [DllImport(_dllName)]
        static extern void WebView_reload(IntPtr webViewPtr);

        [DllImport(_dllName)]
        protected static extern void WebView_resize(IntPtr webViewPtr, int width, int height);

        [DllImport(_dllName)]
        static extern void WebView_scroll(IntPtr webViewPtr, int deltaX, int deltaY);

        [DllImport(_dllName)]
        static extern void WebView_scrollAtPoint(IntPtr webViewPtr, int deltaX, int deltaY, int pointerX, int pointerY);

        [DllImport(_dllName)]
        static extern void WebView_setCameraAndMicrophoneEnabled(bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setConsoleMessageEventsEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setDefaultBackgroundEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setFocused(IntPtr webViewPtr, bool focused);

        [DllImport(_dllName)]
        static extern void WebView_setFocusedInputFieldEventsEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_setRenderingEnabled(IntPtr webViewPtr, bool enabled);

        [DllImport(_dllName)]
        static extern void WebView_stopLoad(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_zoomIn(IntPtr webViewPtr);

        [DllImport(_dllName)]
        static extern void WebView_zoomOut(IntPtr webViewPtr);
    #endregion

    #region Obsolete APIs
        [Obsolete(ObsoletionMessages.Blur, true)]
        public void Blur() {}

        [Obsolete(ObsoletionMessages.CanGoBack, true)]
        public void CanGoBack(Action<bool> callback) {}

        [Obsolete(ObsoletionMessages.CanGoForward, true)]
        public void CanGoForward(Action<bool> callback) {}

        [Obsolete(ObsoletionMessages.CaptureScreenshot, true)]
        public void CaptureScreenshot(Action<byte[]> callback) {}

        [Obsolete(ObsoletionMessages.DisableViewUpdates, true)]
        public void DisableViewUpdates() {}

        [Obsolete(ObsoletionMessages.EnableViewUpdates, true)]
        public void EnableViewUpdates() {}

        [Obsolete(ObsoletionMessages.Focus, true)]
        public void Focus() {}

        [Obsolete(ObsoletionMessages.GetRawTextureData, true)]
        public void GetRawTextureData(Action<byte[]> callback) {}

        [Obsolete(ObsoletionMessages.HandleKeyboardInput)]
        public void HandleKeyboardInput(string key) => SendKey(key);

        [Obsolete(ObsoletionMessages.Init, true)]
        public void Init(Texture2D texture, float width, float height) {}

        [Obsolete(ObsoletionMessages.Init2, true)]
        public void Init(Texture2D texture, float width, float height, Texture2D videoTexture) {}

        Dictionary<EventHandler, EventHandler<LoadFailedEventArgs>> _legacyPageLoadFailedHandlerMap;
        [Obsolete(ObsoletionMessages.PageLoadFailed)]
        public event EventHandler PageLoadFailed {
            add {
                EventHandler<LoadFailedEventArgs> newHandler = (sender, eventArgs) => value(sender, EventArgs.Empty);
                LoadFailed += newHandler;
                _legacyPageLoadFailedHandlerMap[value] = newHandler;
            }
            remove {
                EventHandler<LoadFailedEventArgs> newHandler;
                _legacyPageLoadFailedHandlerMap.TryGetValue(value, out newHandler);
                if (newHandler == null) {
                    return;
                }
                LoadFailed -= newHandler;
                _legacyPageLoadFailedHandlerMap.Remove(value);
            }
        }

        [Obsolete(ObsoletionMessages.Resolution, true)]
        public float Resolution { get; }

        [Obsolete(ObsoletionMessages.SetResolution, true)]
        public void SetResolution(float pixelsPerUnityUnit) {}

        [Obsolete(ObsoletionMessages.SizeInPixels)]
        public Vector2 SizeInPixels { get { return (Vector2)Size; }}

        #pragma warning disable CS0067
        [Obsolete(ObsoletionMessages.VideoRectChanged, true)]
        public event EventHandler<EventArgs<Rect>> VideoRectChanged;

        [Obsolete(ObsoletionMessages.VideoTexture, true)]
        public Texture2D VideoTexture { get; }
    #endregion
    }
}
#endif
