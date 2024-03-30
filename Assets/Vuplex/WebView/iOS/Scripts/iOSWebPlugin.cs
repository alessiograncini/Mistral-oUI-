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
#if UNITY_IOS && !UNITY_EDITOR
using System;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    class iOSWebPlugin : IWebPlugin {

        public ICookieManager CookieManager { get; } = iOSCookieManager.Instance;

        public static iOSWebPlugin Instance {
            get {
                if (_instance == null) {
                    _instance = new iOSWebPlugin();
                }
                return _instance;
            }
        }

        public WebPluginType Type { get; } = WebPluginType.iOS;

        public void ClearAllData() => iOSWebView.ClearAllData();

        // Deprecated
        public void CreateMaterial(Action<Material> callback) {

            var material = new Material(Resources.Load<Material>("iOSWebMaterial"));
            callback(material);
        }

        public IWebView CreateWebView() => iOSWebView.Instantiate();

        public void EnableRemoteDebugging() => iOSWebView.SetRemoteDebuggingEnabled(true);

        public void SetAutoplayEnabled(bool enabled) => iOSWebView.SetAutoplayEnabled(enabled);

        public void SetCameraAndMicrophoneEnabled(bool enabled) => iOSWebView.SetCameraAndMicrophoneEnabled(enabled);

        public void SetIgnoreCertificateErrors(bool ignore) => iOSWebView.SetIgnoreCertificateErrors(ignore);

        public void SetStorageEnabled(bool enabled) => iOSWebView.SetStorageEnabled(enabled);

        public void SetUserAgent(bool mobile) => iOSWebView.GloballySetUserAgent(mobile);

        public void SetUserAgent(string userAgent) => iOSWebView.GloballySetUserAgent(userAgent);

        static iOSWebPlugin _instance;
    }
}
#endif
