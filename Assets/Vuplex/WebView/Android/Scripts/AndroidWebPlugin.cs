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
#if UNITY_ANDROID && !UNITY_EDITOR
#pragma warning disable CS0618
using System;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    class AndroidWebPlugin : MonoBehaviour, IWebPlugin {

        public ICookieManager CookieManager { get; } = AndroidCookieManager.Instance;

        public static AndroidWebPlugin Instance {
            get {
                if (_instance == null) {
                    _instance = new GameObject("AndroidWebPlugin").AddComponent<AndroidWebPlugin>();
                    DontDestroyOnLoad(_instance.gameObject);
                    if (AndroidUtils.DeviceIsMetaQuest()) {
                        AndroidWebView.SetAlternativePointerInputSystemEnabled(true);
                    }
                    if (SystemInfo.deviceModel.Contains("Magic Leap")) {
                        AndroidWebView.SetForceDrawEnabled(true);
                    }
                }
                return _instance;
            }
        }

        public WebPluginType Type { get; } = WebPluginType.Android;

        public void ClearAllData() => AndroidWebView.ClearAllData();

        // Deprecated
        public void CreateMaterial(Action<Material> callback) => callback(AndroidUtils.CreateAndroidMaterial());

        public IWebView CreateWebView() => AndroidWebView.Instantiate();

        public void EnableRemoteDebugging() => AndroidWebView.SetRemoteDebuggingEnabled(true);

        public void SetAutoplayEnabled(bool enabled) => AndroidWebView.SetAutoplayEnabled(enabled);

        public void SetCameraAndMicrophoneEnabled(bool enabled) => AndroidWebView.SetCameraAndMicrophoneEnabled(enabled);

        public void SetIgnoreCertificateErrors(bool ignore) => AndroidWebView.SetIgnoreCertificateErrors(ignore);

        public void SetStorageEnabled(bool enabled) => AndroidWebView.SetStorageEnabled(enabled);

        public void SetUserAgent(bool mobile) => AndroidWebView.GloballySetUserAgent(mobile);

        public void SetUserAgent(string userAgent) => AndroidWebView.GloballySetUserAgent(userAgent);

        static AndroidWebPlugin _instance;

        /// <summary>
        /// Automatically pause web processing and media playback
        /// when the app is paused and resume it when the app is resumed.
        /// </summary>
        void OnApplicationPause(bool isPaused) {

            if (isPaused) {
                AndroidWebView.FlushCookies();
            }
            // See the documentation for AndroidWebView.PauseAll() for more info.
            #if !VUPLEX_ANDROID_DISABLE_AUTOMATIC_PAUSING
                if (isPaused) {
                    WebViewLogger.LogWarning("3D WebView for Android is automatically pausing all of the webviews in an application because Unity has paused. If this interferes with WebViews created by other plugins in your project (such as ad SDKs), please see this article: https://support.vuplex.com/articles/android-conflicts-with-other-plugins");
                    AndroidWebView.PauseAll();
                } else {
                    AndroidWebView.ResumeAll();
                }
            #endif
        }
    }
}
#endif
