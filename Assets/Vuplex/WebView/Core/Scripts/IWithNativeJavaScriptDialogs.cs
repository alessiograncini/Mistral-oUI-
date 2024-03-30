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
using System.Threading.Tasks;

namespace Vuplex.WebView {

    /// <summary>
    /// An interface implemented by a webview if it supports showing native popups
    /// triggered by JavaScript dialog APIs like window.alert() and confirm().
    /// </summary>
    /// <example>
    /// <code>
    /// await webViewPrefab.WaitUntilInitialized;
    /// var webViewWithNativeDialogs = webViewPrefab.WebView as IWithNativeJavaScriptDialogs;
    /// if (webViewWithNativeDialogs == null) {
    ///     Debug.Log("This 3D WebView plugin doesn't yet support IWithNativeJavaScriptDialogs: " + webViewPrefab.WebView.PluginType);
    ///     return;
    /// }
    /// webViewWithNativeDialogs.SetNativeJavaScriptDialogsEnabled(false);
    /// </code>
    /// </example>
    public interface IWithNativeJavaScriptDialogs {

        /// <summary>
        /// Native JavaScript dialog popups are enabled by default but can be disabled using this method.
        /// </summary>
        void SetNativeJavaScriptDialogsEnabled(bool enabled);
    }
}
