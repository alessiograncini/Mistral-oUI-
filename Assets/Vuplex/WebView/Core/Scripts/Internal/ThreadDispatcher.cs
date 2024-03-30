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
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    /// <summary>
    /// Internal class for running code on the main Unity thread.
    /// </summary>
    public class ThreadDispatcher : MonoBehaviour {

        public static bool CurrentlyOnMainThread {
            get {
                if (_mainThreadId == 0) {
                    // This happens if CurrentlyOnMainThread is accessed from a method annotated with `RuntimeInitializeOnLoadMethod()`
                    // (for example: method that AndroidWebPlugin.cs calls on startup on Meta Quest).
                    return true;
                }
                return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
            }
        }

        public static void RunOnMainThread(Action action) {

            if (CurrentlyOnMainThread) {
                action();
                return;
            }
            lock(_backlog) {
                _backlog.Add(action);
                _queued = true;
            }
        }

        // It's necessary to create the instance at startup instead of dynamically through
        // an Instance property (like 3D WebView's other singleton classes do) because it's used
        // from other threads, and Unity APIs (like `new GameObject()`) can't be used from other threads.
        // Note: BeforeSceneLoad is used because earlier callbacks (e.g. SubsystemRegistration, BeforeSplashScreen)
        // prevent it from working correctly.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void _initialize() {
            if (_instance == null) {
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
                _instance = new GameObject("WebView Thread Dispatcher").AddComponent<ThreadDispatcher>();
                DontDestroyOnLoad(_instance.gameObject);
            }
        }

        void Update() {

            if (_queued) {
                lock(_backlog) {
                    var tmp = _actions;
                    _actions = _backlog;
                    _backlog = tmp;
                    _queued = false;
                }

                foreach (var action in _actions) {
                    try {
                        action();
                    } catch (Exception e) {
                        WebViewLogger.LogError("An exception occurred while dispatching an action on the main thread: " + e);
                    }
                }
                _actions.Clear();
            }
        }

        static List<Action> _actions = new List<Action>(8);
        static List<Action> _backlog = new List<Action>(8);
        static ThreadDispatcher _instance;
        static int _mainThreadId;
        static volatile bool _queued = false;
    }
}
