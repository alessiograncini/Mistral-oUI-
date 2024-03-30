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
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEditor;
using Vuplex.WebView.Internal;

namespace Vuplex.WebView.Editor {

    public static class EditorUtils {

        public static void CopyAndReplaceDirectory(string srcPath, string dstPath, bool ignoreMetaFiles = true) {

            if (Directory.Exists(dstPath)) {
                Directory.Delete(dstPath, true);
            }
            if (File.Exists(dstPath)) {
                File.Delete(dstPath);
            }
            Directory.CreateDirectory(dstPath);

            foreach (var file in Directory.GetFiles(srcPath)) {
                if (!ignoreMetaFiles || Path.GetExtension(file) != ".meta") {
                    File.Copy(file, Path.Combine(dstPath, Path.GetFileName(file)));
                }
            }
            foreach (var dir in Directory.GetDirectories(srcPath)) {
                CopyAndReplaceDirectory(dir, Path.Combine(dstPath, Path.GetFileName(dir)), ignoreMetaFiles);
            }
        }

        public static void DrawLink(string linkText, string url, int underlineLength) {

            var linkStyle = new GUIStyle {
                richText = true,
                padding = new RectOffset {
                    top = 2,
                    bottom = 2
                }
            };
            var linkClicked = GUILayout.Button(
                EditorUtils.TextWithColor(linkText, EditorUtils.GetLinkColor()),
                linkStyle
            );
            var linkRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);

            // Unity's editor GUI doesn't support underlines, so fake it.
            var underscores = new string[underlineLength];
            for (var i = 0; i < underlineLength; i++) {
                underscores[i] = "_";
            }
            var underline = String.Join("", underscores);

            GUI.Label(
                linkRect,
                EditorUtils.TextWithColor(underline, EditorUtils.GetLinkColor()),
                new GUIStyle {
                    richText = true,
                    padding = new RectOffset {
                        top = 4,
                        bottom = 2
                }
            });
            if (linkClicked) {
                Application.OpenURL(url);
            }
        }

        /// <summary>
        /// Returns the path to a given directory, searching for it if needed.
        /// If `directoryToSearch` isn't provided, `Application.dataPath` is used.
        /// </summary>
        public static string FindDirectory(string expectedPath, string directoryToSearch = null, string[] ignorePaths = null) {

            if (Directory.Exists(expectedPath)) {
                return expectedPath;
            }
            // The directory isn't in the expected location, so fall back to finding it.
            var directoryName = Path.GetFileName(expectedPath);
            if (directoryToSearch == null) {
                directoryToSearch = Application.dataPath;
            }
            var directories = Directory.GetDirectories(directoryToSearch, directoryName, SearchOption.AllDirectories);
            if (ignorePaths != null) {
                directories = directories.ToList().Where(d => !ignorePaths.Contains(d)).ToArray();
            }
            return _returnOnePathOrThrow(directories, expectedPath, directoryToSearch, true);
        }

        /// <summary>
        /// Returns the path to a given file, searching for it if needed.
        /// If `directoryToSearch` isn't provided, `Application.dataPath` is used.
        /// </summary>
        public static string FindFile(string expectedPath, string directoryToSearch = null) {

            if (File.Exists(expectedPath)) {
                return expectedPath;
            }
            // The file isn't in the expected location, so fall back to finding it.
            var fileName = Path.GetFileName(expectedPath);
            if (directoryToSearch == null) {
                directoryToSearch = Application.dataPath;
            }
            var files = Directory.GetFiles(directoryToSearch, fileName, SearchOption.AllDirectories);
            return _returnOnePathOrThrow(files, expectedPath, directoryToSearch);
        }

        public static string GetLinkColor() => EditorGUIUtility.isProSkin ? "#7faef0ff" : "#11468aff";

        public static string TextWithColor(string text, string color) => $"<color={color}>{text}</color>";

        public static bool XRSdkIsEnabled(string sdkNameFragment) {

            // This approach is taken because the legacy Oculus XR plugin identifies itself as "Oculus", but
            // the new XR plugin shows up as two devices named "oculus input" and "oculus display". Similarly,
            // the MockHMD plugin used to identify itself as "MockHMD" but now it shows up as "MockHMD Head Tracking"
            // and "MockHMD Display".
            foreach (var sdkName in XRSettings.supportedDevices) {
                if (sdkName.ToLowerInvariant().Contains(sdkNameFragment.ToLowerInvariant())) {
                    return true;
                }
            }
            return false;
        }

        static string _returnOnePathOrThrow(string[] paths, string expectedPath, string directorySearched, bool isDirectory = false) {

            var itemName = isDirectory ? "directory" : "file";
            if (paths.Length == 1) {
                return paths[0];
            }
            var targetFileOrDirectoryName = Path.GetFileName(expectedPath);
            if (paths.Length > 1) {
                var joinedPaths = String.Join(", ", paths);
                throw new Exception($"Unable to determine which version of the {itemName} {targetFileOrDirectoryName} to use because multiple instances ({paths.Length}) were unexpectedly found in the directory {directorySearched}. Please review the list of instances found and remove duplicates so that there is only one: {joinedPaths}");
            }
            throw new Exception($"Unable to locate the {itemName} {targetFileOrDirectoryName}. It's not in the expected location ({expectedPath}), and no instances were found in the directory {directorySearched}. To resolve this issue, please try deleting your existing Assets/Vuplex directory and reinstalling 3D WebView.");
        }
    }
}
