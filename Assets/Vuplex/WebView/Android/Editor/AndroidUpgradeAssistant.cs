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
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Vuplex.WebViewUpgrade {

    /// <summary>
    /// Deletes old files from previous versions of 3D WebView that must
    /// be removed to prevent compiler errors.
    /// </summary>
    [InitializeOnLoad]
    class AndroidUpgradeAssistant {

        static string[] _filesToDelete = new string[] {
            "AndroidTextureCreator.cs" // Removed in v4.7
        };

        static AndroidUpgradeAssistant() {

            foreach (var fileName in _filesToDelete) {
                var filePaths = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);
                if (filePaths.Length > 0) {
                    Vuplex.WebView.Internal.WebViewLogger.Log($"Just a heads-up: 3D WebView is automatically deleting an old {fileName} file that is no longer part of 3D WebView.");
                    foreach (var filePath in filePaths) {
                        File.Delete(filePath);
                    }
                }
            }
        }
    }
}
#endif
