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
    /// An interface implemented by a webview if it supports creating a PDF from a web page.
    /// Created PDFs are saved to Application.temporaryCachePath, but you can move them to a different
    /// location after they are created. If you wish to format the PDF differently on Windows and macOS,
    /// you can achieve that by using StandaloneWebView.CreatePdf() instead.
    /// </remarks>
    /// <remarks>
    /// On iOS, PDF creation is only supported on iOS 14 and newer.
    /// </remarks>
    /// <example>
    /// <code>
    /// await webViewPrefab.WaitUntilInitialized();
    /// var webViewWithPdfCreation = webViewPrefab.WebView as IWithPdfCreation;
    /// if (webViewWithPdfCreation == null) {
    ///     Debug.Log("This 3D WebView plugin doesn't yet support IWithPdfCreation: " + webViewPrefab.WebView.PluginType);
    ///     return;
    /// }
    /// var pdfFilePath = await webViewWithPdfCreation.CreatePdf();
    /// // Now that the PDF has been created, do something with it.
    /// // For example, you can move it to a different location.
    /// File.Move(pdfFilePath, someOtherLocation);
    /// </code>
    /// </example>
    public interface IWithPdfCreation {

        /// <summary>
        /// Creates a PDF from the current web page and returns the full file path of the created PDF.
        /// PDFs are saved to Application.temporaryCachePath, but you can move them to a different
        /// location after they are created.
        /// </summary>
        Task<string> CreatePdf();
    }
}
