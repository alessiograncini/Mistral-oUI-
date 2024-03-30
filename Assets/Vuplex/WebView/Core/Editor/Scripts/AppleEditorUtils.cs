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
#if UNITY_IOS || UNITY_VISIONOS
#pragma warning disable CS0618
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vuplex.WebView.Editor {

    public static class AppleEditorUtils {

        public static void SetActivePlugin(bool enablePlugin1, string plugin1ExpectedPath, string plugin2ExpectedPath, BuildTarget platform) {

            _enableOrDisableIfNeeded(plugin1ExpectedPath, enablePlugin1, platform);
            _enableOrDisableIfNeeded(plugin2ExpectedPath, !enablePlugin1, platform);
        }

        static void _enableOrDisableIfNeeded(string pluginExpectedPath, bool enable, BuildTarget platform) {

            string absolutePath;
            try {
                absolutePath = EditorUtils.FindFile(Path.Combine(Application.dataPath, pluginExpectedPath));
            } catch (Exception ex) {
                #if VUPLEX_INTERNAL
                    return;
                #else
                    throw ex;
                #endif
            }
            var relativePath = absolutePath.Replace(Application.dataPath, "Assets");        
            var pluginImporter = (PluginImporter)PluginImporter.GetAtPath(relativePath);
            if (pluginImporter.GetCompatibleWithPlatform(platform) != enable) {
                pluginImporter.SetCompatibleWithPlatform(platform, enable);
                pluginImporter.SetPlatformData(platform, "CPU", "AnyCPU");
                pluginImporter.SaveAndReimport();
            } 
        }
    }
}
#endif