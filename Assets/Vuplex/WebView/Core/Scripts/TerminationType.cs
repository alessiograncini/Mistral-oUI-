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
namespace Vuplex.WebView {

    /// <summary>
    /// Termination types for the IWebView.Terminated event.
    /// </summary>
    public enum TerminationType {

        /// <summary>
        /// Indicates that the web content process terminated
        /// because it crashed. This can happen, for example,
        /// due to a bug in the browser engine or due to the
        /// application running out of memory.
        /// </summary>
        Crashed,

        /// <summary>
        /// Indicates that the web content process terminated because
        /// it was killed by the operating system. This can happen
        /// on Android because it terminates application processes
        /// when the device is low on memory or CPU resources.
        /// </summary>
        Killed,

        /// <summary>
        /// Indicates that the reason for the termination is unknown.
        /// This value is used on iOS because iOS's webViewWebContentProcessDidTerminate
        /// callback doesn't indicate the reason for the termination.
        /// The actual reason is either that the web content process crashed or that the operating
        /// system killed it due to resource constraints, but it's not known which of these is the case.
        /// </summary>
        Unknown
    }
}
