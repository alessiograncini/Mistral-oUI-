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
namespace Vuplex.WebView.Demos {

    // Note: This file is still named HardwareKeyboardListener.cs (rather than something like Legacy.cs)
    // so that it replaces the existing HardwareKeyboardListener.cs file when upgrading from an old version.
    // Added in v3.0, removed in v4.3.
    [System.Obsolete("HardwareKeyboardListener was removed in v4.3 because WebViewPrefab and CanvasWebViewPrefab now automatically handle keyboard input by default. Please remove your code that references HardwareKeyboardListener, and keyboard support will still work. For more info, including details about how you can still access keyboard input programmatically, please see this article: https://support.vuplex.com/articles/keyboard", true)]
    public class HardwareKeyboardListener {}
}
