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

namespace Vuplex.WebView {

    /// <summary>
    /// An interface implemented by a webview if it supports opening popups.
    /// For detailed examples, please see 3D WebView's PopupDemo and CanvasPopupDemo
    /// scenes.
    /// </summary>
    /// <remarks>
    /// There are several approaches that a web page can use to open a URL in a popup.
    /// For example, it can load a new URL using the window.open() JavaScript API or
    /// use the target="_blank" attribute on a link. In a normal web browser, these
    /// approaches cause the browser to open the new URL in a new tab or window. In
    /// 3D WebView, the default behavior is to load the new URL in the existing webview
    /// without creating a new webview. However, an application can change this by
    /// using the IWithPopups interface to make popup URLs open in a new popup webview.
    /// </remarks>
    /// <example>
    /// <code>
    /// await webViewPrefab.WaitUntilInitialized;
    /// var webViewWithPopups = webViewPrefab.WebView as IWithPopups;
    /// if (webViewWithPopups != null) {
    ///     webViewWithPopups.SetPopupMode(PopupMode.LoadInNewWebView);
    ///
    ///     webViewWithPopups.PopupRequested += async (sender, eventArgs) => {
    ///         Debug.Log("Popup opened with URL: " + eventArgs.Url);
    ///         // Create and display a new WebViewPrefab for the popup.
    ///         var popupPrefab = WebViewPrefab.Instantiate(eventArgs.WebView);
    ///         popupPrefab.transform.parent = transform;
    ///         popupPrefab.transform.localPosition = Vector3.zero;
    ///         popupPrefab.transform.localEulerAngles = new Vector3(0, 180, 0);
    ///         await popupPrefab.WaitUntilInitialized();
    ///         popupPrefab.WebView.CloseRequested += (popupWebView, closeEventArgs) => {
    ///             Debug.Log("Closing the popup");
    ///             popupPrefab.Destroy();
    ///         };
    ///     };
    /// }
    /// </code>
    /// </example>
    public interface IWithPopups {

        /// <summary>
        /// Sets how the webview handles popups.
        /// </summary>
        void SetPopupMode(PopupMode popupMode);

        /// <summary>
        /// Indicates that the webview requested a popup.
        /// </summary>
        event EventHandler<PopupRequestedEventArgs> PopupRequested;
    }
}
