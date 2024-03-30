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
#pragma warning disable CS0618
using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine.XR;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView.Editor {

    /// <summary>
    /// Build script that applies required settings for Android.
    /// </summary>
    public class AndroidBuildScript :
                                    #if UNITY_2018_2_OR_NEWER
                                      IPostGenerateGradleAndroidProject,
                                    #endif
                                      IPreprocessBuild {

        // Set the callbackOrder to MaxValue so that this script runs last, after OVRGradleGeneration.PatchAndroidManifest().
        public int callbackOrder { get { return int.MaxValue; }}

        /// <seealso cref="IPreprocessBuild"/>
        public void OnPreprocessBuild(BuildTarget buildTarget, string buildPath) {

            if (buildTarget != BuildTarget.Android) {
                return;
            }
            AndroidEditorUtils.PreprocessBuild("3D WebView for Android", "Assets/Vuplex/WebView/Android/Plugins/proguard-webview-android.txt", "libVuplexWebViewAndroid.so", true);
            #if !UNITY_2018_2_OR_NEWER
                // IPostGenerateGradleAndroidProject is only supported in Unity 2018.2 and newer.
                WebViewLogger.LogWarning("The version of Unity in use is older than 2018.2, so AndroidBuildScript.cs will be unable to automatically modify the app's AndroidManifest.xml to apply recommended settings.");
            #endif
        }

        /// <seealso cref="IPostGenerateGradleAndroidProject"/>
        public void OnPostGenerateGradleAndroidProject(string gradleProjectPath) {

            _modifyAndroidManifestIfNeeded(gradleProjectPath);
        }

        const string ANDROID_XML_NAMESPACE = "http://schemas.android.com/apk/res/android";

        // On Meta Quest devices, the app's AndroidManifest.xml must have the following application
        // tag in order for 3D WebView for Android to render correctly:
        // <meta-data android:name="com.oculus.always_draw_view_root" android:value="true"/>
        static void _addOculusMetaDataElementIfNeeded(XmlDocument xmlDocument, XmlElement applicationElement) {

            // Add the Oculus meta-data element when any XR device is supported (not just ones that contain the word "Oculus")
            // because the OpenXR devices may be used instead of the Oculus devices.
            if (XRSettings.supportedDevices.Length == 0) {
                return;
            }
            var oculusMetaDataAttributeName = "com.oculus.always_draw_view_root";
            if (xmlDocument.InnerXml.Contains(oculusMetaDataAttributeName)) {
                // The AndroidManifest.xml file already contains the needed meta-data element.
                // This happens on subsequent builds with the incremental build pipeline added in 2021.2.
                return;
            }
            var metaDataElement = xmlDocument.CreateElement("meta-data");
            metaDataElement.SetAttribute("name", ANDROID_XML_NAMESPACE, oculusMetaDataAttributeName);
            metaDataElement.SetAttribute("value", ANDROID_XML_NAMESPACE, "true");
            applicationElement.AppendChild(metaDataElement);
        }

        void _addVuplexActivityIfNeeded(XmlDocument xmlDocument, XmlElement applicationElement) {

            var helperActivityName = "com.vuplex.webview.HelperActivity";
            if (xmlDocument.InnerXml.Contains(helperActivityName)) {
                // The AndroidManifest.xml file already contains the helper activity.
                // This happens on subsequent builds with the incremental build pipeline added in 2021.2.
                return;
            }
            var activityElement = xmlDocument.CreateElement("activity");
            activityElement.SetAttribute("name", ANDROID_XML_NAMESPACE, helperActivityName);
            activityElement.SetAttribute("theme", ANDROID_XML_NAMESPACE, "@android:style/Theme.Translucent.NoTitleBar");
            applicationElement.AppendChild(activityElement);
        }

        /// <summary>
        /// Enables support for loading http:// (non-https) URLs.
        /// https://support.vuplex.com/articles/how-to-enable-cleartext-traffic-on-android
        /// </summary>
        void _enableCleartextTrafficIfNeeded(XmlElement applicationElement) {

            var leaveDisabled = false;
            #if VUPLEX_ANDROID_DISABLE_CLEARTEXT_TRAFFIC
                leaveDisabled = true;
            #endif
            if (leaveDisabled) {
                return;
            }
            applicationElement.SetAttribute("usesCleartextTraffic", ANDROID_XML_NAMESPACE, "true");
            var networkSecurityConfigExists = !String.IsNullOrEmpty(applicationElement.GetAttribute("networkSecurityConfig", ANDROID_XML_NAMESPACE));
            if (!networkSecurityConfigExists) {
                return;
            }
            #if VUPLEX_OCULUS_PROJECT_CONFIG
                var projectConfig = OVRProjectConfig.GetProjectConfig();
                if (projectConfig != null) {
                    if (projectConfig.enableNSCConfig) {
                        WebViewLogger.Log("Just a heads-up: 3D WebView is automatically disabling the \"Enable NSC Configuration\" setting in Oculus project settings in order to allow webviews to load plain http:// (non-https) URLs. If you want to disable this behavior, you can do so by adding the scripting symbol VUPLEX_ANDROID_DISABLE_CLEARTEXT_TRAFFIC in Player Settings. For more info, see this page: https://support.vuplex.com/articles/how-to-enable-cleartext-traffic-on-android");
                        projectConfig.enableNSCConfig = false;
                        OVRProjectConfig.CommitProjectConfig(projectConfig);
                        applicationElement.RemoveAttribute("networkSecurityConfig", ANDROID_XML_NAMESPACE);
                        return;
                    }
                }
            #endif
            WebViewLogger.LogWarning("The application's AndroidManifest.xml file sets a value for android:networkSecurityConfig, which may override 3D WebView's attempt to enable http:// (non-https) URLs. If you find that the application is unable to load http:// URLs, then please either remove your network security config file or verify that it includes cleartextTrafficPermitted=\"false\". For more info, see this page: https://support.vuplex.com/articles/how-to-enable-cleartext-traffic-on-android");
        }

        /// <summary>
        /// Sets android:hardwareAccelerated="true" on both the application and the activity elements.
        /// This is needed in order to display hardware-accelerated content (videos and WebGL) in
        /// native 2D mode. Unity explicitly sets android:hardwareAccelerated="false" on the activity,
        /// so this is the only way to override it.
        /// https://forum.unity.com/threads/532786
        /// </summary>
        void _enableHardwareAccelerationIfNeeded(XmlElement applicationElement, XmlElement activityElement) {

            #if !VUPLEX_ANDROID_DISABLE_HARDWARE_ACCELERATION
                applicationElement.SetAttribute("hardwareAccelerated", ANDROID_XML_NAMESPACE, "true");
                activityElement.SetAttribute("hardwareAccelerated", ANDROID_XML_NAMESPACE, "true");
            #endif
        }

        void _modifyAndroidManifestIfNeeded(string gradleProjectPath) {

            var androidManifestPath = Path.Combine(gradleProjectPath, "src", "main", "AndroidManifest.xml");
            var xmlDocument = new XmlDocument();
            xmlDocument.PreserveWhitespace = true;
            xmlDocument.Load(androidManifestPath);
            var applicationElement = (XmlElement)xmlDocument.DocumentElement.SelectSingleNode("//application");
            var namespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
            namespaceManager.AddNamespace("android", ANDROID_XML_NAMESPACE);
            // First, try to get the activity that matches the default
            // "com.unity3d.player.UnityPlayerActivity" activity name.
            var activityElement = (XmlElement)applicationElement.SelectSingleNode("//activity[@android:name='com.unity3d.player.UnityPlayerActivity']", namespaceManager);
            if (activityElement == null) {
                // No activity matches the default name, so just use the first activity.
                var activityElements = applicationElement.SelectNodes("//activity");
                if (activityElements.Count == 0) {
                    throw new BuildFailedException("3D WebView is unable to make required modifications to the AndroidManifest.xml file because it contains no <activity> elements.");
                }
                if (activityElements.Count > 1) {
                    WebViewLogger.LogWarning("3D WebView must modify the main <activity> in the the AndroidManifest.xml in order for some functionality to work correctly, but the AndroidManifest.xml doesn't contain an <activity> with the default name 'com.unity3d.player.UnityPlayerActivity' and the AndroidManifest.xml file contains multiple <activity> elements. So, 3D WebView will assume the first <activity> in the AndroidManifest.xml is the application's main activity.");
                }
                activityElement = (XmlElement)activityElements[0];
            }
            _enableHardwareAccelerationIfNeeded(applicationElement, activityElement);
            _enableCleartextTrafficIfNeeded(applicationElement);
            _addOculusMetaDataElementIfNeeded(xmlDocument, applicationElement);
            _addVuplexActivityIfNeeded(xmlDocument, applicationElement);
            xmlDocument.Save(androidManifestPath);
        }
    }
}
#endif
