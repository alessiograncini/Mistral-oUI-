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
namespace Vuplex.WebView {

    /// <summary>
    /// An interface implemented by a webview if it supports [deep linking](https://en.wikipedia.org/wiki/Mobile_deep_linking).
    /// </summary>
    /// <remarks>
    /// On iOS, in order to open a link with a custom URI scheme, that scheme must also be listed in
    /// the app's Info.plist using the key [LSApplicationQueriesSchemes](https://developer.apple.com/library/archive/documentation/General/Reference/InfoPlistKeyReference/Articles/LaunchServicesKeys.html),
    /// otherwise iOS will block the custom URI scheme from being loaded.
    /// Example Info.plist entry:
    /// <code>
    /// &lt;key&gt;LSApplicationQueriesSchemes&lt;/key&gt;
    /// &lt;array&gt;
    ///     &lt;string&gt;vnd.youtube&lt;/string&gt;
    /// &lt;/array&gt;
    /// </code>
    /// </remarks>
    /// On Android, deep linking is only triggered by a URL with a custom scheme (e.g. my-app://path).
    /// A URL with an http:// or https:// scheme will always open the web page in the browser rather than deep link to an external app.
    /// <remarks>
    /// </remarks>
    /// <example>
    /// <code>
    /// await webViewPrefab.WaitUntilInitialized();
    /// var webViewWithDeepLinking = webViewPrefab.WebView as IWithDeepLinking;
    /// if (webViewWithDeepLinking == null) {
    ///     Debug.Log("This 3D WebView plugin doesn't support IWithDeepLinking: " + webViewPrefab.WebView.PluginType);
    ///     return;
    /// }
    /// webViewWithDeepLinking.SetDeepLinkingEnabled(true);
    /// // Load a page with a link that opens the YouTube app.
    /// webViewPrefab.WebView.LoadHtml("&lt;a href='vnd.youtube://grP0iDrSjso'&gt;Click to launch YouTube&lt;/a&gt;");
    /// </code>
    /// </example>
    public interface IWithDeepLinking {

        /// <summary>
        /// Sets whether deep links are enabled. The default is `false`.
        /// </summary>
        void SetDeepLinkingEnabled(bool enabled);
    }
}
