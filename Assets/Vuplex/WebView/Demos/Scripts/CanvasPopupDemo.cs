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
    /// Sets up the CanvasPopupDemo scene, which demonstrates how to use
    /// the IWithPopups interface with CanvasWebViewPrefab.
    /// </summary>
    /// <remarks>
    /// Links: <br/>
    /// - CanvasWebViewPrefab docs: https://developer.vuplex.com/webview/CanvasWebViewPrefab <br/>
    /// - IWithPopups: https://developer.vuplex.com/webview/IWithPopups <br/>
    /// - How clicking works: https://support.vuplex.com/articles/clicking <br/>
    /// - Other examples: https://developer.vuplex.com/webview/overview#examples <br/>
    /// </remarks>
    class CanvasPopupDemo : MonoBehaviour {

        async void Start() {

            var canvas = GameObject.Find("Canvas");
            // Create a webview for the main content.
            var mainWebViewPrefab = CanvasWebViewPrefab.Instantiate();
            mainWebViewPrefab.Resolution = 1.5f;
            mainWebViewPrefab.PixelDensity = 2;
            mainWebViewPrefab.Native2DModeEnabled = true;
            mainWebViewPrefab.transform.SetParent(canvas.transform, false);

            var rectTransform = mainWebViewPrefab.transform as RectTransform;
            rectTransform.anchoredPosition3D = Vector3.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            mainWebViewPrefab.transform.localScale = Vector3.one;

            // Wait for the prefab to initialize because its WebView property is null until then.
            // https://developer.vuplex.com/webview/WebViewPrefab#WaitUntilInitialized
            await mainWebViewPrefab.WaitUntilInitialized();

            // After the prefab has initialized, you can use the IWithPopups API via its WebView property.
            // https://developer.vuplex.com/webview/IWithPopups
            var webViewWithPopups = mainWebViewPrefab.WebView as IWithPopups;
            if (webViewWithPopups == null) {
                mainWebViewPrefab.WebView.LoadHtml(NOT_SUPPORTED_HTML);
                return;
            }

            Debug.Log("Loading Pinterest as an example because it uses popups for third party login. Click 'Login', then select Facebook or Google to open a popup for authentication.");
            mainWebViewPrefab.WebView.LoadUrl("https://pinterest.com");

            webViewWithPopups.SetPopupMode(PopupMode.LoadInNewWebView);
            webViewWithPopups.PopupRequested += async (webView, eventArgs) => {
                Debug.Log("Popup opened with URL: " + eventArgs.Url);
                var popupPrefab = CanvasWebViewPrefab.Instantiate(eventArgs.WebView);
                popupPrefab.Resolution = mainWebViewPrefab.Resolution;
                popupPrefab.transform.SetParent(canvas.transform, false);
                var popupRectTransform = popupPrefab.transform as RectTransform;
                popupRectTransform.anchoredPosition3D = Vector3.zero;
                popupRectTransform.offsetMin = Vector2.zero;
                popupRectTransform.offsetMax = Vector2.zero;
                popupPrefab.transform.localScale = Vector3.one;
                // Place the popup in front of the main webview.
                var localPosition = popupPrefab.transform.localPosition;
                localPosition.z = 0.1f;
                popupPrefab.transform.localPosition = localPosition;

                await popupPrefab.WaitUntilInitialized();
                popupPrefab.WebView.CloseRequested += (popupWebView, closeEventArgs) => {
                    Debug.Log("Closing the popup");
                    popupPrefab.Destroy();
                };
            };
        }

        const string NOT_SUPPORTED_HTML = @"
            <body>
                <style>
                    body {
                        font-family: sans-serif;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        line-height: 1.25;
                    }
                    div {
                        max-width: 80%;
                    }
                    li {
                        margin: 10px 0;
                    }
                </style>
                <div>
                    <p>
                        Sorry, but this 3D WebView package doesn't support yet the <a href='https://developer.vuplex.com/webview/IWithPopups'>IWithPopups</a> interface. Current packages that support popups:
                    </p>
                    <ul>
                        <li>
                            <a href='https://developer.vuplex.com/webview/StandaloneWebView'>3D WebView for Windows and macOS</a>
                        </li>
                        <li>
                            <a href='https://developer.vuplex.com/webview/AndroidWebView'>3D WebView for Android</a>
                        </li>
                        <li>
                            <a href='https://developer.vuplex.com/webview/AndroidGeckoWebView'>3D WebView for Android with Gecko Engine</a>
                        </li>
                    </ul>
                </div>
            </body>
        ";
    }
}
