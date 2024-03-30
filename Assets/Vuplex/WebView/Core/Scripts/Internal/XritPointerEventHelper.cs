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
#if VUPLEX_XR_INTERACTION_TOOLKIT
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vuplex.WebView.Internal {

    class XritPointerEventHelper : MonoBehaviour {

        public PointerEventData LastPointerEventData { get; private set; }

        public static XritPointerEventHelper Instance {
            get {
                if (_instance == null) {
                    _instance = new GameObject("WebView XR Pointer Event Helper").AddComponent<XritPointerEventHelper>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        static XritPointerEventHelper _instance;

        void Start() {

            var uiInputModule = EventSystem.current.currentInputModule as UnityEngine.XR.Interaction.Toolkit.UI.UIInputModule;
            if (uiInputModule == null) {
                WebViewLogger.LogWarning("The scene's input module is not an XR Interaction Toolkit UIInputModule, so hovering will not be enabled.");
                return;
            }
            uiInputModule.finalizeRaycastResults += (eventData, _) => LastPointerEventData = eventData;
        }
    }
}
#endif
