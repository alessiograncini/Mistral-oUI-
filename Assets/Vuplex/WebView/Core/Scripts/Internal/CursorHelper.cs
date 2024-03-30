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
using System.Collections.Generic;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// Class used internally to change the cursor icon on Windows and macOS.
    /// </summary>
    public static class CursorHelper {

        /// <param name="cursorType">The cursor icon type, or null to reset to the default cursor.</param>
        public static void SetCursorIcon(string cursorType) {

            CursorInfo cursorInfo = null;
            if (cursorType != null) {
                _supportedCursors.TryGetValue(cursorType, out cursorInfo);
            }
            if (cursorInfo == null) {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            } else {
                Cursor.SetCursor(cursorInfo.Texture, cursorInfo.HotSpot, CursorMode.Auto);
            }
        }

        class CursorInfo {

            public CursorInfo(string textureName, Vector2 hotSpot) {
                
                _textureName = textureName;
                HotSpot = hotSpot;
            }

            public Vector2 HotSpot { get; private set; } 

            public Texture2D Texture {
                get {
                    if (_texture == null) {
                        _texture = Resources.Load<Texture2D>(_textureName);
                    }
                    return _texture;
                }
            }

            private Texture2D _texture;
            private string _textureName;
        }

        static Dictionary<string, CursorInfo> _supportedCursors = new Dictionary<string, CursorInfo> {
            ["pointer"] = new CursorInfo("pointer", new Vector2(5, 0)),
            ["text"] = new CursorInfo("text", new Vector2(16, 16))
        };
    }
}
