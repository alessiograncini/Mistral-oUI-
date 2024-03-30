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
using UnityEngine;
using Vuplex.WebView;

namespace Vuplex.Demos {

    /// <summary>
    /// Provides a simple example of using 3D WebView's scripting APIs
    /// with a CanvasWebViewPrefab.
    /// </summary>
    class CanvasWorldSpaceDemo : MonoBehaviour {

        CanvasWebViewPrefab canvasWebViewPrefab;

        void Awake() {

            // Use a desktop User-Agent to request the desktop versions of websites.
            // https://developer.vuplex.com/webview/Web#SetUserAgent
            // Call this from Awake() to ensure it's called before the webview initializes.
            Web.SetUserAgent(false);
        }

        async void Start() {

            // Get a reference to the CanvasWebViewPrefab.
            // https://support.vuplex.com/articles/how-to-reference-a-webview
            canvasWebViewPrefab = GameObject.Find("CanvasWebViewPrefab").GetComponent<CanvasWebViewPrefab>();

            // Wait for the prefab to initialize because its WebView property is null until then.
            // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
            await canvasWebViewPrefab.WaitUntilInitialized();

            // After the prefab has initialized, you can use the IWebView APIs via its WebView property.
            // https://developer.vuplex.com/webview/IWebView
            canvasWebViewPrefab.WebView.UrlChanged += (sender, eventArgs) => {
                Debug.Log("[CanvasWorldSpaceDemo] URL changed: " + eventArgs.Url);
            };
        }
    }
}
