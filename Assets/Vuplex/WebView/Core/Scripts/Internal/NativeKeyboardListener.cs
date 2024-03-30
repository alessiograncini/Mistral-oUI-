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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Vuplex.WebView.Internal {

    public class KeyboardEventArgs : EventArgs {

        public KeyboardEventArgs(string key, KeyModifier modifiers) {
            Key = key;
            Modifiers = modifiers;
        }
        public readonly string Key;
        public readonly KeyModifier Modifiers;
        public override string ToString() => $"Key: {Key}, Modifiers: {Modifiers}";
    }

    /// <summary>
    /// Internal class that detects keys pressed on the native hardware keyboard.
    /// </summary>
    public class NativeKeyboardListener : MonoBehaviour {

        public event EventHandler ImeCompositionCancelled;

        public event EventHandler<EventArgs<string>> ImeCompositionChanged;

        public event EventHandler<EventArgs<string>> ImeCompositionFinished;

        public event EventHandler<KeyboardEventArgs> KeyDownReceived;

        public event EventHandler<KeyboardEventArgs> KeyUpReceived;

        public static NativeKeyboardListener Instantiate() {

            return new GameObject("NativeKeyboardListener").AddComponent<NativeKeyboardListener>();
        }

        class KeyRepeatState {
            public string Key;
            public bool HasRepeated;
        }

        Regex _alphanumericRegex = new Regex("[a-zA-Z0-9]");
        static readonly Func<string, bool> _hasValidUnityKeyName = _memoize<string, bool>(
            javaScriptKeyName => {
                try {
                    foreach (var keyName in _getPotentialUnityKeyNames(javaScriptKeyName)) {
                        Input.GetKey(keyName);
                    }
                    return true;
                } catch {
                    return false;
                }
            }
        );
        List<string> _keysDown = new List<string>();
        // Keys that don't show up correctly in Input.inputString.
        // Must be defined before _keyValues and cannot contain any values present in _keyValues (or else the key will be dispatched twice).
        static readonly string[] _keyValuesUndetectableThroughInputString = new string[] {
            // Notes:
            // - "Backspace" is included only because it doesn't show up in Input.inputString for the Hololens system TouchScreenKeyboard. In other scenarios, it can be detected as \b through Input.inputString.
            // - "Enter" is only included because it doesn't show up in Input.inputString when IME is enabled on macOS. In other scenarios, it can be detected as \n or \r through Input.inputString.
            "ArrowDown", "ArrowRight", "ArrowLeft", "ArrowUp", "Backspace", "End", "Enter", "Escape", "Delete", "Help", "Home", "Insert", "PageDown", "PageUp", "Tab"
        };
        KeyRepeatState _keyRepeatState;
        static readonly string[] _keyValues = new string[] {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "`", "-", "=", "[", "]", "\\", ";", "'", ",", ".", "/", " "
        }.Concat(_keyValuesUndetectableThroughInputString).ToArray();
        bool _legacyInputManagerDisabled;
        KeyModifier _modifiersDown;

        bool _areKeysUndetectableThroughInputStringPressed() {

            foreach (var key in _keyValuesUndetectableThroughInputString) {
                foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                    // Use GetKey instead of GetKeyDown because on macOS, Input.inputString
                    // contains garbage when the arrow keys are held down.
                    if (Input.GetKey(keyName)) {
                        return true;
                    }
                }
            }
            return false;
        }

        void Awake() {

            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                _legacyInputManagerDisabled = true;
                WebViewLogger.LogWarning("3D WebView's support for automatically detecting input from the native keyboard currently requires Unity's Legacy Input Manager, which is currently disabled for the project. So, automatic detection of input from the native keyboard will be disabled. For details, please see this page: https://support.vuplex.com/articles/keyboard");
            #else
                _enableImeIfNeeded();
            #endif
        }

        void _enableImeIfNeeded() {

            var result = _checkIfImeShouldBeEnabled();
            if (result.Item1) {
                Input.imeCompositionMode = IMECompositionMode.On;
            } else if (result.Item2 != null) {
                WebViewLogger.LogError(result.Item2);
            }
        }

        Tuple<bool, string> _checkIfImeShouldBeEnabled() {

            var isMacOS = Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor;
            if (!isMacOS) {
                return new Tuple<bool, string>(true, null);
            }
            var correctlySupportsIme = false;
            #if UNITY_2020_3
                // Note: Unity doesn't support the *_OR_NEWER scripting symbols for minor release versions, like UNITY_2020_3_38_OR_NEWER)
                var minorVersion = Application.unityVersion.Split(new char[] { '.' })[2];
                var minorVersionNumber = int.Parse(minorVersion.Split(new char[] { 'f', 'a', 'b' })[0]);
                correctlySupportsIme = minorVersionNumber >= 38;
            #elif UNITY_2021_2_OR_NEWER
                correctlySupportsIme = true;
            #endif
            if (correctlySupportsIme) {
                return new Tuple<bool, string>(true, null);
            }
            string errorMessage = null;
            switch (Application.systemLanguage) {
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                case SystemLanguage.Japanese:
                case SystemLanguage.Korean:
                    errorMessage = $"The system language is set to a language that uses IME ({Application.systemLanguage}), but the version of Unity in use ({Application.unityVersion}) has a bug where IME doesn't work correctly on macOS. To use IME with 3D WebView on macOS, please upgrade to Unity 2021.2 or newer. For more details, please see this page: https://issuetracker.unity3d.com/issues/macos-linux-input-dot-inputstring-doesnt-convert-input-to-the-suggestions-from-ime";
                    break;
            }
            return new Tuple<bool, string>(false, errorMessage);
        }

        KeyModifier _getModifiers() {

            var modifiers = KeyModifier.None;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                modifiers |= KeyModifier.Shift;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) {
                modifiers |= KeyModifier.Control;
            }
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) {
                modifiers |= KeyModifier.Alt;
            }
            if (Input.GetKey(KeyCode.LeftWindows) ||
                Input.GetKey(KeyCode.RightWindows)) {
                modifiers |= KeyModifier.Meta;
            }
            // Don't pay attention to the command keys on Windows because Unity has a bug
            // where it falsly reports the command keys are pressed after switching languages
            // with the windows+space shortcut.
            #if !(UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                if (Input.GetKey(KeyCode.LeftCommand) ||
                    Input.GetKey(KeyCode.RightCommand)) {
                    modifiers |= KeyModifier.Meta;
                }
            #endif

            return modifiers;
        }

        // https://docs.unity3d.com/Manual/class-InputManager.html#:~:text=the%20Input%20Manager.-,Key%20names,-follow%20these%20naming
        // For keys like Alt that have both left and right versions, it's important to include both for Android because
        // GetKeyUp("Alt") doesn't detect the right alt key keyup on Android. The same is true for Control, Meta, and Shift.
        // Memoize the results to prevent array allocation from creating a lot of garbage that will need collected.
        static readonly Func<string, string[]> _getPotentialUnityKeyNames = _memoize<string, string[]>(
            javaScriptKeyValue => {
                switch (javaScriptKeyValue) {
                    case " ":
                        return new [] {"space"};
                    case "Alt":
                        return new [] {"left alt", "right alt"};
                    case "ArrowUp":
                        return new [] {"up"};
                    case "ArrowDown":
                        return new [] {"down"};
                    case "ArrowRight":
                        return new [] {"right"};
                    case "ArrowLeft":
                        return new [] {"left"};
                    case "Control":
                        return new [] {"left ctrl", "right ctrl"};
                    case "Enter":
                        return new [] {"return"};
                    case "Meta":
                        return new [] {"left cmd", "right cmd"};
                    case "PageUp":
                        return new [] {"page up"};
                    case "PageDown":
                        return new [] {"page down"};
                    case "Shift":
                        return new [] {"left shift", "right shift"};
                }

                // The numpad keys show up in Input.inputString as normal characters, but to detect them through GetKeyUp(),
                // they must be contained in brackets, like "[1]".
                // https://docs.unity3d.com/Manual/class-InputManager.html
                var numpadKeys = new [] {"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "+", "-", "*", "/", "."};
                foreach (var numpadKey in numpadKeys) {
                    if (javaScriptKeyValue == numpadKey) {
                        return new [] {numpadKey, $"[{numpadKey}]"};
                    }
                }

                return new [] {javaScriptKeyValue.ToLowerInvariant()};
            }
        );

        /// <summary>
        /// Returns a memoized version of the given function.
        /// </summary>
        static Func<TArg, TReturn> _memoize<TArg, TReturn>(Func<TArg, TReturn> function) {

            var cache = new Dictionary<TArg, TReturn>();
            return arg => {
                TReturn result;
                if (cache.TryGetValue(arg, out result)) {
                    return result;
                }
                result = function(arg);
                cache.Add(arg, result);
                return result;
            };
        }

        // When an IME composition is in progress, Input.GetKeyDown() and GetKey() are unable to detect the arrow keys.
        // So, in that scenario, we use Event.current (which must be used within OnGUI()) to detect the arrow keys so
        // they can be used to move the cursor within the IME composition.
        void OnGUI() {

            var imeCompositionInProgress = Input.compositionString.Length > 0;
            if (!imeCompositionInProgress) {
                return;
            }
            var ev = Event.current;
            if (ev == null || !ev.isKey) {
                return;
            }
            string keyToDispatch = null;
            if (ev.keyCode == KeyCode.LeftArrow) {
                keyToDispatch = "ArrowLeft";
            } else if (ev.keyCode == KeyCode.RightArrow) {
                keyToDispatch = "ArrowRight";
            }
            if (keyToDispatch == null) {
                return;
            }
            var eventArgs = new KeyboardEventArgs(keyToDispatch, KeyModifier.None);
            KeyDownReceived?.Invoke(this, eventArgs);
            KeyUpReceived?.Invoke(this, eventArgs);
        }

        bool _processInputString() {

            var inputString = Input.inputString;
            // Some versions of Unity 2020.3 for macOS have a bug where Input.inputString includes characters
            // twice (for example: "aa" instead of "a"). This occurs specifically in the Player (not in the Editor)
            // and has been reproduced with 2020.3.39 and 2020.3.41.
            // https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-16427
            #if UNITY_STANDALONE_OSX && UNITY_2020_3
                if (inputString.Length == 2 && inputString[0] == inputString[1]) {
                    inputString = inputString[0].ToString();
                }
            #endif
            foreach (var character in inputString) {
                string characterString;
                switch (character) {
                    case '\b':
                        characterString = "Backspace";
                        break;
                    case '\n':
                    case '\r':
                        characterString = "Enter";
                        break;
                    case (char)0xF728:
                        // 0xF728 = NSDeleteFunctionKey on macOS
                        characterString = "Delete";
                        break;
                    default:
                        characterString = character.ToString();
                        break;
                }
                // For some keyboard layouts like AZERTY (e.g. French), Input.inputString will contain
                // the correct character for a ctr+alt+{} key combination (e.g. ctrl+alt+0 makes Input.inputString equal "@"), but
                // Input.GetKeyUp() won't return true for that key when the key combination is released
                // (e.g. Input.GetKeyUp("@") always returns false). So, as a workaround, we emit
                // the KeyUpReceived event immediately in that scenario instead of adding it to _keysDown.
                var skipGetKeyUpBecauseUnityBug = _modifiersDown != KeyModifier.None && characterString.Length == 1 && !_alphanumericRegex.IsMatch(characterString);
                var altGrPressed = _modifiersDown == (KeyModifier.Alt | KeyModifier.Control);
                if (skipGetKeyUpBecauseUnityBug && altGrPressed) {
                    // When AltGr is used, the Alt and Control modifiers will be detected, but these shouldn't
                    // emitted because they shouldn't be passed to the browser with the key.
                    // Note: this should only be reset to KeyModifier.None when AltGr is detected, because the JavaScript KeyboardEvent.key field
                    // isn't set correctly on Windows and macOS for characters like @ and ! unless the Shift modifier is included.
                    _modifiersDown = KeyModifier.None;
                }
                KeyDownReceived?.Invoke(this, new KeyboardEventArgs(characterString, _modifiersDown));
                // We also need to skip calling Input.GetKeyUp() if the character isn't compatible with GetKeyUp(). For example, on
                // Azerty keyboards, the 2 key (without modifiers) triggers "é", which can't be passed to GetKeyUp().
                var skipGetKeyUpBecauseIncompatibleCharacter = !_hasValidUnityKeyName(characterString);
                if (skipGetKeyUpBecauseUnityBug || skipGetKeyUpBecauseIncompatibleCharacter) {
                    KeyUpReceived?.Invoke(this, new KeyboardEventArgs(characterString, _modifiersDown));
                } else {
                    // It's a character that works with Input.GetKeyUp(), so add it to _keysDown.
                    _keysDown.Add(characterString);
                }
            }
            return Input.inputString.Length > 0;
        }

        bool _processIme() {

            var previousCompositionString = _previousImeCompositionString;
            _previousImeCompositionString = Input.compositionString;
            var compositionString = Input.compositionString;
            // The Microsoft Pinyin keyboard automatically adds apostrophes between latin letters. For example, if you type
            // "abc", the Input.compositionString contains "a'b'c". However, when moving
            // between characters with the arrow keys, the Windows IME skips over the apostrophes like they don't exist.
            // This is problematic because when arrow keys are sent to Chromium (to move the text caret within its IME composition node),
            // it doesn't automatically skip over apostrophes like the Windows IME does. This causes the text caret position in the Chromium
            // IME visualization to be incorrect compared to the actual text caret position in the (invisible) Windows IME composition.
            // It would be ideal to detect the text caret position of the Windows IME composition (in order to synchronize Chromium's IME text caret position),
            // but I haven't found a way to accomplish that. Unity doesn't have an API for it, and when I tried to use the Win32 ImmGetCompositionString() API
            // like demonstrated in CefClient's GetCompositionSelectionRange() function, I found that it always incorrectly indicates that the text caret is
            // positioned at the end of the composition. So, as a workaround to keep the text caret position in sync between the underlying (invisible) Windows IME
            // and the visualized Chromium IME composition node, we remove the apostrophes from the IME composition and don't send them to Chromium.
            if (compositionString.Contains("'")) {
                compositionString = compositionString.Replace("'", "");
            }
            var imeStateChanged = false;
            if (previousCompositionString != "") {
                // If Input.inputString contains a value while Input.compositionString contains a value (even if they're the same value),
                // it indicates that Input.inputString contains the text of a committed (finished) IME composition and that Input.compositionString
                // contain the text of a new composition that has just started.                
                if (Input.inputString != "") {
                    ImeCompositionFinished?.Invoke(this, new EventArgs<string>(Input.inputString));
                    imeStateChanged = true;
                } else if (compositionString == "") {
                    ImeCompositionCancelled?.Invoke(this, EventArgs.Empty);
                    imeStateChanged = true;
                }
            }
            if (compositionString != previousCompositionString && compositionString != "") {
                ImeCompositionChanged?.Invoke(this, new EventArgs<string>(compositionString));
                imeStateChanged = true;
            }
            return imeStateChanged;
        }

        void _processKeysPressed() {

            if (!(Input.anyKeyDown || Input.inputString.Length > 0)) {
                return;
            }
            var nonInputStringKeysDetected = _processKeysUndetectableThroughInputString();
            if (nonInputStringKeysDetected) {
                return;
            }
            // Using Input.inputString when possible is preferable since it
            // handles different languages and characters that would be hard
            // to support using Input.GetKeyDown().
            var inputStringKeysDetected = _processInputString();
            if (inputStringKeysDetected) {
                return;
            }
            // If we've made it to this point, then only modifier keys by themselves have been pressed.
            _processModifierKeysOnly();
        }

        void _processKeysReleased() {

            if (_keysDown.Count == 0) {
                return;
            }
            var keysDownCopy = new List<string>(_keysDown);
            foreach (var key in keysDownCopy) {
                bool keyUp = false;
                try {
                    foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                        if (Input.GetKeyUp(keyName)) {
                            keyUp = true;
                            break;
                        }
                    }
                } catch (ArgumentException ex) {
                    // This would only happen if an invalid key is added to _keyValuesUndetectableThroughInputString
                    // because other keys are verified via _hasValidUnityKeyName.
                    WebViewLogger.LogError("Invalid key value passed to Input.GetKeyUp: " + ex);
                    _keysDown.Remove(key);
                    return;
                }
                if (keyUp) {
                    _keysDown.Remove(key);
                    var emitKeyUp = true;
                    // See the comments above _repeatKey().
                    if (_keyRepeatState?.Key == key) {
                        CancelInvoke(REPEAT_KEY_METHOD_NAME);
                        if (_keyRepeatState.HasRepeated) {
                            // KeyUpReceived has already been emitted for the key.
                            emitKeyUp = false;
                        }
                        _keyRepeatState = null;
                    }
                    if (emitKeyUp) {
                        KeyUpReceived?.Invoke(this, new KeyboardEventArgs(key, _modifiersDown));
                    }
                }
            }
        }

        bool _processKeysUndetectableThroughInputString() {

            var modifierKeysPressed = !(_modifiersDown == KeyModifier.None || _modifiersDown == KeyModifier.Shift);
            var keysUndetectableThroughInputStringArePressed = _areKeysUndetectableThroughInputStringPressed();
            var oneOrMoreKeysProcessed = false;
            // On Windows, when modifier keys are held down, Input.inputString is blank
            // even if other keys are pressed. So, use Input.GetKeyDown() in that scenario.
            if (keysUndetectableThroughInputStringArePressed || (Input.inputString.Length == 0 && modifierKeysPressed)) {
                foreach (var key in _keyValues) {
                    foreach (var keyName in _getPotentialUnityKeyNames(key)) {
                        if (Input.GetKeyDown(keyName)) {
                            KeyDownReceived?.Invoke(this, new KeyboardEventArgs(key, _modifiersDown));
                            _keysDown.Add(key);
                            oneOrMoreKeysProcessed = true;
                            // See the comments above _repeatKey().
                            if (_keyRepeatState != null) {
                                CancelInvoke(REPEAT_KEY_METHOD_NAME);
                            }
                            _keyRepeatState = new KeyRepeatState { Key = key };
                            InvokeRepeating(REPEAT_KEY_METHOD_NAME, 0.5f, 0.1f);
                            break;
                        }
                    }
                }
            }
            return oneOrMoreKeysProcessed;
        }

        void _processModifierKeysOnly() {

            foreach (var value in Enum.GetValues(typeof(KeyModifier))) {
                var modifierValue = (KeyModifier)value;
                if (modifierValue == KeyModifier.None) {
                    continue;
                }
                if ((_modifiersDown & modifierValue) != 0) {
                    var key = modifierValue.ToString();
                    KeyDownReceived?.Invoke(this, new KeyboardEventArgs(key, KeyModifier.None));
                    _keysDown.Add(key);
                }
            }
        }

        // Whereas Input.inputString automatically handles repeating keys when they are pressed down,
        // Input.GetKeyDown() doesn't implement that repeating functionality. So, this class uses
        // InvokeRepeating() to implement repeating for keys in _keyValuesUndetectableThroughInputString.
        const string REPEAT_KEY_METHOD_NAME = "_repeatKey";
        void _repeatKey() {

            var key = _keyRepeatState?.Key;
            if (key == null) {
                // This shouldn't happen.
                CancelInvoke(REPEAT_KEY_METHOD_NAME);
                return;
            }
            var eventArgs = new KeyboardEventArgs(key, _modifiersDown);
            if (!_keyRepeatState.HasRepeated) {
                // This is the first time _repeatKey() has been called for the key,
                // so it's still down from the initial press.
                KeyUpReceived?.Invoke(this, eventArgs);
                _keyRepeatState.HasRepeated = true;
            }
            KeyDownReceived?.Invoke(this, eventArgs);
            KeyUpReceived?.Invoke(this, eventArgs);
        }

        string _previousImeCompositionString = "";

        void Update() {

            if (_legacyInputManagerDisabled) {
                return;
            }
            if (_processIme()) {
                return;
            }
            _modifiersDown = _getModifiers();
            _processKeysPressed();
            _processKeysReleased();
        }
    }
}
