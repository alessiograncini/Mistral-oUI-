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
#if UNITY_ANDROID

namespace Vuplex.WebView.Internal {

    static class AndroidWarnings {

        public static string LogStreamingAssetsErrorAndGetWarningPageHtml() {

            var message = "Sorry, but 3D WebView for Android is unable to load web pages from StreamingAssets when \"Split Application Binary\" is enabled. For more details, please see this page: ";
            var supportArticleUrl = "https://support.vuplex.com/articles/android-split-binary-streaming-assets";
            WebViewLogger.LogError(message + supportArticleUrl);
            return $@"{message}
                      <br><br>
                      <a href='{supportArticleUrl}'>{supportArticleUrl}</a>
                      <style>
                        body {{
                            font-family: sans-serif;
                        }}
                      </style>";
        }
    }
}
#endif
