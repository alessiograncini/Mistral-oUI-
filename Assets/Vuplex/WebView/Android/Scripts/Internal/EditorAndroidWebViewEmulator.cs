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
#if UNITY_ANDROID && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView {

    // Hooks into the partial methods of MockWebView and StandaloneWebView
    // in order to log Android-related warnings while running in the editor.
    //
    // This file can't be moved to the /Editor folder because it must be
    // compiled into the same assembly as StandaloneWebView.cs and MockWebView.cs

// It's necessary to explicitly exclude the Linux editor because ConditionalCompilationUtility
// doesn't execute when running in headless mode (i.e. for CI/CD).
#if VUPLEX_STANDALONE && !UNITY_EDITOR_LINUX
    partial class StandaloneWebView {
#else
    partial class MockWebView {
#endif

        partial void OnLoadUrl(string url) {

            if (url == null) {
                return;
            }
            if (!PlayerSettings.Android.splitApplicationBinary) {
                return;
            }
            if (url.StartsWith("streaming-assets://") || url.Contains(Application.streamingAssetsPath)) {
                var warningPageHtml = AndroidWarnings.LogStreamingAssetsErrorAndGetWarningPageHtml();
                LoadHtml(warningPageHtml);
            }
        }
    }
}
#endif
