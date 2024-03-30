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
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// Static utility methods used internally by 3D WebView.
    /// </summary>
    /// <remarks>
    /// This class used to be named Utils, but since Utils is a common class name,
    /// if the user's project contained a class named Utils in the global namespace,
    /// it would break the 3D WebView classes that use this class.
    /// Similarly, the XR-related methods used to be in a class named XRUtils, but Unity added
    /// an XRUtils class to the UnityEngine.Rendering namespace, which led to ambiguous references.
    /// </remarks>
    public static class VXUtils {

        public static Material CreateDefaultMaterial() {

            // Construct a new material, because Resources.Load<T>() returns a singleton.
            return new Material(Resources.Load<Material>("DefaultWebMaterial"));
        }

        public static string GetGraphicsApiErrorMessage(GraphicsDeviceType activeGraphicsApi, GraphicsDeviceType[] acceptableGraphicsApis) {

            var isValid = Array.IndexOf(acceptableGraphicsApis, activeGraphicsApi) != -1;
            if (isValid) {
                return null;
            }
            var acceptableApiStrings = acceptableGraphicsApis.ToList().Select(api => api.ToString());
            var acceptableApisList = String.Join(" or ", acceptableApiStrings.ToArray());
            return $"Unsupported graphics API: Vuplex 3D WebView requires {acceptableApisList} for this platform, but the selected graphics API is {activeGraphicsApi}. Please go to Player Settings and set \"Graphics APIs\" to {acceptableApisList}.";
        }

        public static void LogNative2DModeWarning(string methodName, string effect = "will be ignored") {

            WebViewLogger.LogWarning($"{methodName}() was called but {effect} because it is not supported in Native 2D Mode.");
        }
    }
}
