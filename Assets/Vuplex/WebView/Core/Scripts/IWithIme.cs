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
using UnityEngine;

namespace Vuplex.WebView {

    /// <summary>
    /// An interface implemented by a webview if it supports Input Method Editor (IME) for
    /// inputting Chinese, Japanese, and Korean text. On Windows and macOS, if keyboard support
    /// is enabled via WebViewPrefab.KeyboardEnabled, then IME is automatically enabled
    /// (implemented with this interface). For an example of using this interface, please see
    /// 3D WebView's KeyboardManager.cs script.
    /// </summary>
    public interface IWithIme {

        /// <summary>
        /// Indicates that the coordinates of the IME text composition within the browser changed.
        /// </summary>
        event EventHandler<EventArgs<Vector2Int>> ImeInputFieldPositionChanged;

        /// <summary>
        /// Cancels the current IME composition.
        /// </summary>
        void CancelImeComposition();

        /// <summary>
        /// Finishes the current IME composition with the given text.
        /// </summary>
        void FinishImeComposition(string text);

        /// <summary>
        /// Updates the current IME composition with the given text, or starts a new composition
        /// if one isn't already in progress.
        /// </summary>
        void SetImeComposition(string text);
    }
}
