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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    public class WebPluginFactory {

        public virtual List<IWebPlugin> GetAllPlugins() {

            _assertNotTooEarly();
            return _allPlugins.ToList();
        }

        public virtual IWebPlugin GetDefaultPlugin(WebPluginType[] preferredPlugins = null) {

            _assertNotTooEarly();
            var isServerBuild = false;
            #if UNITY_SERVER
                isServerBuild = true;
            #endif
            if (isServerBuild) {
                _logMockWarningOnce("3D WebView doesn't support the \"Server Build\" option because it uses a null graphics device (GraphicsDeviceType.Null)");
                return MockWebPlugin.Instance;
            }

            #if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                return _choosePlugin(_standalonePlugin, "Windows or macOS", "3D WebView for Windows and macOS", "windows-mac");
            #elif UNITY_ANDROID
                var preferChromiumAndroidPlugin = preferredPlugins != null && preferredPlugins.Contains(WebPluginType.Android);
                IWebPlugin selectedAndroidPlugin = null;
                if (_androidPlugin != null && (_androidGeckoPlugin == null || preferChromiumAndroidPlugin)) {
                    selectedAndroidPlugin = _androidPlugin;
                } else if (_androidGeckoPlugin != null) {
                    selectedAndroidPlugin = _androidGeckoPlugin;
                }
                return _choosePlugin(selectedAndroidPlugin, "Android", "3D WebView for Android", "android");
            #elif UNITY_IOS
                return _choosePlugin(_iosPlugin, "iOS", "3D WebView for iOS", "ios");
            #elif UNITY_WSA
                return _choosePlugin(_uwpPlugin, "UWP", "3D WebView for UWP", "uwp");
            #elif UNITY_VISIONOS
                return _choosePlugin(_visionOSPlugin, "visionOS", "3D WebView for visionOS", "visionos");
            #elif UNITY_WEBGL
                return _choosePlugin(_webGLPlugin, "WebGL", "2D WebView for WebGL", "webgl");
            #else
                throw new WebViewUnavailableException("3D WebView is not supported on the current build platform. For more info, please visit https://developer.vuplex.com .");
            #endif
        }

        public static void RegisterAndroidPlugin(IWebPlugin plugin) {

            _addPlugin(_androidPlugin = plugin);
        }

        public static void RegisterAndroidGeckoPlugin(IWebPlugin plugin) {

            _addPlugin(_androidGeckoPlugin = plugin);
        }

        public static void RegisterIOSPlugin(IWebPlugin plugin) {

            _addPlugin(_iosPlugin = plugin);
        }

        public static void RegisterStandalonePlugin(IWebPlugin plugin) {

            _addPlugin(_standalonePlugin = plugin);
        }

        public static void RegisterUwpPlugin(IWebPlugin plugin) {

            _addPlugin(_uwpPlugin = plugin);
        }

        public static void RegisterVisionOSPlugin(IWebPlugin plugin) {

            _addPlugin(_visionOSPlugin = plugin);
        }

        public static void RegisterWebGLPlugin(IWebPlugin plugin) {

            _addPlugin(_webGLPlugin = plugin);
        }

        protected static HashSet<IWebPlugin> _allPlugins = new HashSet<IWebPlugin>();
        protected static IWebPlugin _androidPlugin;
        protected static IWebPlugin _androidGeckoPlugin;
        static bool _beforeSceneLoadCalled;
        protected static IWebPlugin _iosPlugin;
        bool _mockWarningLogged;
        protected static IWebPlugin _standalonePlugin;
        protected static IWebPlugin _uwpPlugin;
        protected static IWebPlugin _visionOSPlugin;
        protected static IWebPlugin _webGLPlugin;

        static void _addPlugin(IWebPlugin plugin) {

            if (plugin != null) {
                _allPlugins.Add(plugin);
            }
        }

        void _assertNotTooEarly() {

            if (!_beforeSceneLoadCalled) {
                // The plugin registrant classes (like StandaloneWebPluginRegistrant) call their corresponding methods (like RegisterStandalonePlugin)
                // in BeforeSceneLoad. So, if the application calls a Web method prior to Awake() (e.g. in BeforeSceneLoad), not all the plugins may be registered yet.
                // For example, the MockWebPlugin may be registered (e.g. via RegisterAndroidPlugin), but the StandaloneWebPlugin may not be registered yet.
                throw new InvalidOperationException("A Web class method was called too early, prior to Awake(). This can happen, for example, if the application calls a Web method in a function decorated with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`. Please wait until Awake() or later to call Web class methods.");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void _beforeSceneLoad() => _beforeSceneLoadCalled = true;

        IWebPlugin _choosePlugin(IWebPlugin plugin, string buildPlatform, string packageName, string storeUrlPath) {

            #if UNITY_EDITOR
                if (IgnoreMissingPluginInEditor && plugin == null) {
                    plugin = MockWebPlugin.Instance;
                }
            #endif
            if (plugin == null) {
                throw new WebViewUnavailableException($"The build platform is set to {buildPlatform}, but {packageName} isn't installed in the project. {packageName} is required in order for 3D WebView to work on {buildPlatform}." + _getMoreInfoText(storeUrlPath));
            }
            if ((Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor) && _standalonePlugin != null) {
                return _standalonePlugin;
            }
            if (plugin is MockWebPlugin) {
                if (Application.platform == RuntimePlatform.LinuxEditor) {
                    _logMockWarningOnce("3D WebView doesn't support the Linux Unity Editor");
                } else {
                    _logMockWarningOnce("3D WebView for Windows and macOS is not currently installed");
                }
            }
            return plugin;
        }

        /// <summary>
        /// If the corresponding 3D WebView package for the current build platform isn't installed and the application attempts to instantiate a webview,
        /// then by default, 3D WebView throws an exception warning about the missing package. The application can set this field to true to cause 3D WebView
        /// to ignore the missing package in the Editor and instead use 3D WebView for Windows and macOS if it's installed or the mock webview implementation if it's not.
        /// This option only impacts the Editor and doesn't affect the Player, so an exception is still thrown in the Player at runtime in this scenario.
        /// </summary>
        /// <example>
        /// <code>
        /// void Awake() {
        ///     Vuplex.WebView.Internal.WebPluginFactory.IgnoreMissingPluginInEditor = true;
        /// }
        /// </code>
        /// </example>
        public static bool IgnoreMissingPluginInEditor;

        string _getMoreInfoText(string storeUrlPath) => $" For more info, please visit https://store.vuplex.com/webview/{storeUrlPath} .";

        /// <summary>
        /// Logs the warning once so that it doesn't spam the console.
        /// </summary>
        void _logMockWarningOnce(string reason) {

            if (!_mockWarningLogged) {
                _mockWarningLogged = true;
                WebViewLogger.LogWarning($"{reason}, so the mock webview will be used{(Application.isEditor ? " while running in the editor" : "")}. For more info, please see <em>https://support.vuplex.com/articles/mock-webview</em>.");
            }
        }
    }
}
