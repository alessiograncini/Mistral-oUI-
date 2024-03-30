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
    /// Sets up the PopupDemo scene, which demonstrates how to use
    /// the IWithPopups interface with WebViewPrefab.
    /// </summary>
    /// <remarks>
    /// Links: <br/>
    /// - WebViewPrefab docs: https://developer.vuplex.com/webview/WebViewPrefab <br/>
    /// - IWithPopups: https://developer.vuplex.com/webview/IWithPopups <br/>
    /// - How clicking works: https://support.vuplex.com/articles/clicking <br/>
    /// - Other examples: https://developer.vuplex.com/webview/overview#examples <br/>
    /// </remarks>
    class PopupDemo : MonoBehaviour {

        async void Start() {

            // Use a desktop User-Agent to request the desktop versions of websites.
            // https://developer.vuplex.com/webview/Web#SetUserAgent
            Web.SetUserAgent(false);

            // Create a 0.6 x 0.3 webview for the main web content.
            var mainWebViewPrefab = WebViewPrefab.Instantiate(0.6f, 0.3f);
            mainWebViewPrefab.PixelDensity = 2;
            mainWebViewPrefab.transform.parent = transform;
            mainWebViewPrefab.transform.localPosition = new Vector3(0, 0, 0.4f);
            mainWebViewPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);

            // Add an on-screen keyboard under the webview.
            // https://developer.vuplex.com/webview/Keyboard
            var keyboard = Keyboard.Instantiate();
            keyboard.transform.SetParent(mainWebViewPrefab.transform, false);
            keyboard.transform.localPosition = new Vector3(0, -0.31f, 0);
            keyboard.transform.localEulerAngles = Vector3.zero;

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
                var popupPrefab = WebViewPrefab.Instantiate(eventArgs.WebView);
                popupPrefab.Resolution = mainWebViewPrefab.Resolution;
                popupPrefab.transform.parent = transform;
                // Place the popup in front of the main webview.
                popupPrefab.transform.localPosition = new Vector3(0, 0, 0.39f);
                popupPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
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
