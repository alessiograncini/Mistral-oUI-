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
#pragma warning disable CS0067
using System;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    public abstract class BaseKeyboard : MonoBehaviour {

        /// <summary>
        /// Indicates that the user pressed a key on the keyboard.
        /// </summary>
        public event EventHandler<EventArgs<string>> KeyPressed;

        /// <summary>
        /// Indicates that the keyboard finished initializing.
        /// </summary>
        public event EventHandler Initialized;

        /// <summary>
        /// If you want to load a customized version of the Keyboard UI, you can
        /// do so by setting this field. For example, you could load a customized
        /// Keyboard UI from StreamingAssets by using a URL like "streaming-assets://keyboard/index.html".
        /// <summary>
        [Label("Custom Keyboard URL (optional)")]
        [Tooltip("If you want to load a customized version of the Keyboard UI, you can do so by setting this field. For example, you could load a customized Keyboard UI from StreamingAssets by using a URL like \"streaming-assets://keyboard/index.html\".")]
        public string CustomKeyboardUrl;

        /// <summary>
        /// Returns a task that completes when the keyboard is initialized,
        /// which means that its WebViewPrefab property is ready for use.
        /// </summary>
        /// <example>
        /// <code>
        /// await keyboard.WaitUntilInitialized();
        /// keyboard.WebViewPrefab.Clicked += (sender, eventArgs) => {
        ///     Debug.Log("Keyboard was clicked");
        /// };
        /// </code>
        /// </example>
        public Task WaitUntilInitialized() {

            var taskSource = new TaskCompletionSource<bool>();
            if (_isInitialized) {
                taskSource.SetResult(true);
            } else {
                Initialized += (sender, e) => taskSource.SetResult(true);
            }
            return taskSource.Task;
        }

        internal BaseWebViewPrefab BaseWebViewPrefab { get { return _webViewPrefab; }}

        bool _isInitialized;
        [SerializeField]
        [HideInInspector]
        protected BaseWebViewPrefab _webViewPrefab;

        protected static readonly WebViewOptions _webViewOptions = new WebViewOptions {
            clickWithoutStealingFocus = true,
            disableVideo = true,
            // If both Android plugins are installed, prefer the original Chromium
            // plugin for the keyboard, since the Gecko plugin doesn't support
            // transparent backgrounds.
            preferredPlugins = new WebPluginType[] { WebPluginType.Android }
        };

        async protected void _init() {

            _webViewPrefab.CursorIconsEnabled = false;
            _webViewPrefab.KeyboardEnabled = false;
            // Reset InitialUrl to null in case the developer modified WebViewPrefab.prefab to set a default InitialUrl.
            _webViewPrefab.InitialUrl = null;
            await _webViewPrefab.WaitUntilInitialized();
            var pluginType = _webViewPrefab.WebView.PluginType;
            if (pluginType == WebPluginType.AndroidGecko) {
                // On Android Gecko, hovering steals focus.
                _webViewPrefab.HoveringEnabled = false;
            }
            // Scrolling and dragging can also cause the keyboard
            // to steal focus on Android Gecko, so just disable them.
            _webViewPrefab.ScrollingEnabled = false;
            _webViewPrefab.DragMode = DragMode.Disabled;
            _webViewPrefab.WebView.MessageEmitted += WebView_MessageEmitted;
            // Android Gecko and Hololens don't support transparent webviews, so set the cutout
            // rect to the entire view so that the shader makes its black background
            // pixels transparent.
            if (pluginType == WebPluginType.AndroidGecko || pluginType == WebPluginType.UniversalWindowsPlatform) {
                _webViewPrefab.SetCutoutRect(new Rect(0, 0, 1, 1));
            }
            if (!String.IsNullOrWhiteSpace(CustomKeyboardUrl)) {
                _webViewPrefab.WebView.LoadUrl(CustomKeyboardUrl.Trim());
            } else {
                _webViewPrefab.WebView.LoadHtml(KeyboardUI.Html);
            }
        }

        void OnDestroy() {

            var keyboardInstance = Internal.KeyboardManager.Instance;
            if (keyboardInstance != null) {
                keyboardInstance.RemoveKeyboard(this);
            }
        }

        void WebView_MessageEmitted(object sender, EventArgs<string> e) {

            var serializedMessage = e.Value;
            var messageType = JsonUtility.FromJson<BridgeMessage>(serializedMessage).type;
            switch (messageType) {
                case "keyboard.inputReceived":
                    var input = StringBridgeMessage.ParseValue(serializedMessage);
                    KeyPressed?.Invoke(this, new EventArgs<string>(input));
                    break;
                case "keyboard.initialized":
                    _sendKeyboardLanguageMessage();
                    _isInitialized = true;
                    Internal.KeyboardManager.Instance.AddKeyboard(this);
                    Initialized?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        string _getKeyboardLanguage() {
            switch (Application.systemLanguage) {
                case SystemLanguage.Danish:
                    return "da";
                case SystemLanguage.French:
                    return "fr";
                case SystemLanguage.German:
                    return "de";
                case SystemLanguage.Italian:
                    return "it";
                case SystemLanguage.Norwegian:
                    return "no";
                case SystemLanguage.Russian:
                    return "ru";
                case SystemLanguage.Spanish:
                    return "es";
                case SystemLanguage.Swedish:
                    return "sv";
                default:
                    return "en";
            }
        }

        /// <summary>
        /// Initializes the keyboard language based on the system language.
        /// </summary>
        void _sendKeyboardLanguageMessage() {

            var message = new StringBridgeMessage {
                type = "keyboard.setLanguage",
                value = _getKeyboardLanguage()
            };
            var serializedMessage = JsonUtility.ToJson(message);
            _webViewPrefab.WebView.PostMessage(serializedMessage);
        }

        protected static void _setLayerRecursively(GameObject gameObject, int layer) {

            if (gameObject == null) {
                return;
            }
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform) {
                if (child != null) {
                    _setLayerRecursively(child.gameObject, layer);
                }
            }
        }

        // Added in v1.0, removed in v4.3.
        [Obsolete("Keyboard.InputReceived was removed in v4.3 because WebViewPrefab and CanvasWebViewPrefab now automatically handle keyboard input by default. Please remove your code that references Keyboard.InputReceived, and keyboard support will still work. For more info, including details about how you can still access keyboard input programmatically, please see this article: https://support.vuplex.com/articles/keyboard", true)]
        public event EventHandler<EventArgs<string>> InputReceived;
    }
}
