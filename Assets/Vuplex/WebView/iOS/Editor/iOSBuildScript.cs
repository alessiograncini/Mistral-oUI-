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
#if UNITY_IOS
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Vuplex.WebView.Editor {

    /// <summary>
    ///  Makes the following changes to the generated Xcode project:
    /// - Adds the WebKit framework
    /// - Adds the -ObjC linker flag for the native iOS library
    /// - Disables bitcode (required since bitcode is disabled for the native iOS library)
    /// </summary>
    public class iOSBuildScript : IPreprocessBuildWithReport {

        /// <seealso cref="IPreprocessBuildWithReport"/>
        public int callbackOrder { get => 0; }

        /// <seealso cref="IPreprocessBuildWithReport"/>
        public void OnPreprocessBuild(BuildReport report) {

            if (report.summary.platform != BuildTarget.iOS) {
                return;
            }
            var isDeviceSdk = PlayerSettings.iOS.sdkVersion == iOSSdkVersion.DeviceSDK;
            AppleEditorUtils.SetActivePlugin(
                isDeviceSdk,
                "Vuplex/WebView/iOS/Plugins/libVuplexWebViewiOS_device.a",
                "Vuplex/WebView/iOS/Plugins/libVuplexWebViewiOS_simulator.a",
                BuildTarget.iOS
            );
        }        

        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {

            if (target != BuildTarget.iOS) {
                return;
            }
            string projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projectPath));
        #if UNITY_2019_3_OR_NEWER
            string targetGuid = project.GetUnityFrameworkTargetGuid();
        #else
            string targetGuid = project.TargetGuidByName("Unity-iPhone");
        #endif
            project.AddFrameworkToProject(targetGuid, "WebKit.framework", false);
            project.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
            project.AddBuildProperty(targetGuid, "ENABLE_BITCODE", "NO");
            File.WriteAllText(projectPath, project.WriteToString());
        }
    }
}
#endif
