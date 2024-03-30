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
using System.Threading.Tasks;

namespace Vuplex.WebView {

    /// <summary>
    /// Implemented by AndroidWebView to provide an alternative to IWebView.GetRawTextureData() that
    /// works without the graphics extensions that 3D WebView normally requires on Android
    /// (the OpenGL GL_OES_EGL_image_external extension or Vulkan VK_ANDROID_external_memory_android_hardware_buffer extension).
    /// </summary>
    /// <example>
    /// <code>
    /// await webViewPrefab.WaitUntilInitialized();
    /// var webViewWithFallbackTextureData = webViewPrefab.WebView as IWithFallbackTextureData;
    /// if (webViewWithFallbackTextureData == null) {
    ///     Debug.Log("This 3D WebView plugin doesn't yet support IWithFallbackTextureData: " + webViewPrefab.WebView.PluginType);
    ///     return;
    /// }
    /// var textureData = await webViewWithFallbackTextureData.GetFallbackTextureData();
    /// var texture = new Texture2D(
    ///     webViewPrefab.WebView.Size.x,
    ///     webViewPrefab.WebView.Size.y,
    ///     TextureFormat.RGBA32,
    ///     false,
    ///     false
    /// );
    /// texture.LoadRawTextureData(textureData);
    /// texture.Apply();
    /// </code>
    /// </example>
    public interface IWithFallbackTextureData {

        /// <summary>
        /// Like IWebView.GetRawTextureData(), except it has the following differences: <br/>
        /// - It works without the graphics extensions that 3D WebView normally requires on Android
        /// (the OpenGL GL_OES_EGL_image_external extension or Vulkan VK_ANDROID_external_memory_android_hardware_buffer extension). <br/>
        /// - It doesn't capture hardware accelerated content like video or WebGL.
        /// </summary>
        Task<byte[]> GetFallbackTextureData();
    }
}
