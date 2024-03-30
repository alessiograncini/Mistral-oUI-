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
    /// Event args for IWebView.LoadFailed.
    /// </summary>
    public class LoadFailedEventArgs : System.EventArgs {

        /// <summary>
        /// A platform-specific error code indicating why the load failed.
        /// </summary>
        public readonly string NativeErrorCode;

        /// <summary>
        /// The URL for which the load failed.
        /// </summary>
        public readonly string Url;

        public LoadFailedEventArgs(string nativeErrorCode, string url) {

            NativeErrorCode = nativeErrorCode;
            Url = url;
        }

        public override string ToString() => $"NativeErrorCode = {NativeErrorCode}, Url = {Url}";
    }
}
