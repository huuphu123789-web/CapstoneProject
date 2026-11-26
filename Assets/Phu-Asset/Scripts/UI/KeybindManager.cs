using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public enum GameAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    Sprint,
    Jump,
    Interact,
    Flashlight
}

/// <summary>
/// Quản lý gán phím (Keybindings) toàn cục, lưu tự động vào PlayerPrefs.
/// Hỗ trợ đồng thời cả New Input System và Legacy Input Manager.
/// </summary>
public static class KeybindManager
{
    private const string PREF_PREFIX = "Keybind_";

    private static readonly Dictionary<GameAction, KeyCode> defaultBindings = new Dictionary<GameAction, KeyCode>
    {
        { GameAction.MoveForward,  KeyCode.W },
        { GameAction.MoveBackward, KeyCode.S },
        { GameAction.MoveLeft,     KeyCode.A },
        { GameAction.MoveRight,    KeyCode.D },
        { GameAction.Sprint,       KeyCode.LeftShift },
        { GameAction.Jump,         KeyCode.Space },
        { GameAction.Interact,     KeyCode.E },
        { GameAction.Flashlight,   KeyCode.F }
    };

    private static readonly Dictionary<GameAction, KeyCode> currentBindings = new Dictionary<GameAction, KeyCode>();
    private static bool isInitialized = false;

    /// <summary>
    /// True khi người chơi đang trong trạng thái bấm chọn phím mới trong tab Controls
    /// (dùng để PauseMenu không đóng bảng khi bấm ESC hủy, và TabGroup không nhảy tab khi bấm Q/E).
    /// </summary>
    public static bool IsRebinding = false;

    public static event Action OnBindingsChanged;

    public static void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (var kvp in defaultBindings)
        {
            string saved = PlayerPrefs.GetString(PREF_PREFIX + kvp.Key, kvp.Value.ToString());
            if (Enum.TryParse(saved, out KeyCode parsed) && parsed != KeyCode.None)
            {
                currentBindings[kvp.Key] = parsed;
            }
            else
            {
                currentBindings[kvp.Key] = kvp.Value;
            }
        }
    }

    public static KeyCode GetKey(GameAction action)
    {
        EnsureInitialized();
        return currentBindings.TryGetValue(action, out KeyCode k) ? k : defaultBindings[action];
    }

    public static void SetKey(GameAction action, KeyCode newKey)
    {
        EnsureInitialized();
        if (newKey == KeyCode.None || newKey == KeyCode.Escape) return;

        KeyCode oldKey = GetKey(action);

        // Nếu phím mới đang được gán cho hành động khác -> hoán đổi để tránh trùng phím
        GameAction? conflictAction = null;
        foreach (var kvp in currentBindings)
        {
            if (kvp.Key != action && kvp.Value == newKey)
            {
                conflictAction = kvp.Key;
                break;
            }
        }

        currentBindings[action] = newKey;
        PlayerPrefs.SetString(PREF_PREFIX + action, newKey.ToString());

        if (conflictAction.HasValue)
        {
            currentBindings[conflictAction.Value] = oldKey;
            PlayerPrefs.SetString(PREF_PREFIX + conflictAction.Value, oldKey.ToString());
        }

        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static void ResetToDefaults()
    {
        EnsureInitialized();
        foreach (var kvp in defaultBindings)
        {
            currentBindings[kvp.Key] = kvp.Value;
            PlayerPrefs.SetString(PREF_PREFIX + kvp.Key, kvp.Value.ToString());
        }
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static string GetActionLabel(GameAction action)
    {
        switch (action)
        {
            case GameAction.MoveForward:  return "Move Forward";
            case GameAction.MoveBackward: return "Move Backward";
            case GameAction.MoveLeft:     return "Move Left";
            case GameAction.MoveRight:    return "Move Right";
            case GameAction.Sprint:       return "Sprint";
            case GameAction.Jump:         return "Jump";
            case GameAction.Interact:     return "Interact";
            case GameAction.Flashlight:   return "Flashlight";
            default:                      return action.ToString();
        }
    }

    public static string GetKeyDisplayName(GameAction action)
    {
        return FormatKeyCode(GetKey(action));
    }

    public static string FormatKeyCode(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.LeftShift:    return "L-SHIFT";
            case KeyCode.RightShift:   return "R-SHIFT";
            case KeyCode.LeftControl:  return "L-CTRL";
            case KeyCode.RightControl: return "R-CTRL";
            case KeyCode.LeftAlt:      return "L-ALT";
            case KeyCode.RightAlt:     return "R-ALT";
            case KeyCode.Space:        return "SPACE";
            case KeyCode.UpArrow:      return "UP";
            case KeyCode.DownArrow:    return "DOWN";
            case KeyCode.LeftArrow:    return "LEFT";
            case KeyCode.RightArrow:   return "RIGHT";
            case KeyCode.Return:       return "ENTER";
            default:
                string s = key.ToString();
                if (s.StartsWith("Alpha") && s.Length == 6) return s.Substring(5);
                return s.ToUpperInvariant();
        }
    }

    public static bool GetKeyHeld(GameAction action)
    {
        KeyCode kc = GetKey(action);
#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemKeyControl(kc, out KeyControl ctrl) && ctrl.isPressed)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(kc)) return true;
#endif
        return false;
    }

    public static bool GetKeyDown(GameAction action)
    {
        KeyCode kc = GetKey(action);
#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemKeyControl(kc, out KeyControl ctrl) && ctrl.wasPressedThisFrame)
        {
            return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(kc)) return true;
#endif
        return false;
    }

    public static Vector2 GetMoveVector()
    {
        float x = 0f;
        float z = 0f;
        if (GetKeyHeld(GameAction.MoveRight)) x += 1f;
        if (GetKeyHeld(GameAction.MoveLeft))  x -= 1f;
        if (GetKeyHeld(GameAction.MoveForward))  z += 1f;
        if (GetKeyHeld(GameAction.MoveBackward)) z -= 1f;

        Vector2 v = new Vector2(x, z);
        if (v.sqrMagnitude > 1f) v.Normalize();
        return v;
    }

    /// <summary>
    /// Bắt phím bàn phím bất kỳ vừa được nhấn trong frame hiện tại (dùng cho UI Rebind).
    /// </summary>
    public static bool TryDetectPressedKey(out KeyCode detectedKey)
    {
        detectedKey = KeyCode.None;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            foreach (KeyControl k in kb.allKeys)
            {
                if (k != null && k.wasPressedThisFrame)
                {
                    KeyCode mapped = MapInputSystemKeyToKeyCode(k.keyCode);
                    if (mapped != KeyCode.None)
                    {
                        detectedKey = mapped;
                        return true;
                    }
                }
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.anyKeyDown)
        {
            foreach (KeyCode kc in RebindableKeyPool)
            {
                if (Input.GetKeyDown(kc))
                {
                    detectedKey = kc;
                    return true;
                }
            }
        }
#endif
        return false;
    }

    private static readonly KeyCode[] RebindableKeyPool = new KeyCode[]
    {
        KeyCode.Escape,
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.I,
        KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R,
        KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        KeyCode.Space, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.Tab, KeyCode.Return,
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6
    };

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetInputSystemKeyControl(KeyCode kc, out KeyControl control)
    {
        control = null;
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;

        Key key = MapKeyCodeToInputSystemKey(kc);
        if (key == Key.None) return false;

        control = kb[key];
        return control != null;
    }

    private static Key MapKeyCodeToInputSystemKey(KeyCode kc)
    {
        if (kc >= KeyCode.A && kc <= KeyCode.Z)
            return Key.A + (kc - KeyCode.A);
        if (kc >= KeyCode.Alpha1 && kc <= KeyCode.Alpha9)
            return Key.Digit1 + (kc - KeyCode.Alpha1);
        if (kc == KeyCode.Alpha0) return Key.Digit0;

        switch (kc)
        {
            case KeyCode.Space:        return Key.Space;
            case KeyCode.LeftShift:    return Key.LeftShift;
            case KeyCode.RightShift:   return Key.RightShift;
            case KeyCode.LeftControl:  return Key.LeftCtrl;
            case KeyCode.RightControl: return Key.RightCtrl;
            case KeyCode.LeftAlt:      return Key.LeftAlt;
            case KeyCode.RightAlt:     return Key.RightAlt;
            case KeyCode.Tab:          return Key.Tab;
            case KeyCode.Return:       return Key.Enter;
            case KeyCode.Escape:       return Key.Escape;
            case KeyCode.UpArrow:      return Key.UpArrow;
            case KeyCode.DownArrow:    return Key.DownArrow;
            case KeyCode.LeftArrow:    return Key.LeftArrow;
            case KeyCode.RightArrow:   return Key.RightArrow;
            case KeyCode.F1:           return Key.F1;
            case KeyCode.F2:           return Key.F2;
            case KeyCode.F3:           return Key.F3;
            case KeyCode.F4:           return Key.F4;
            case KeyCode.F5:           return Key.F5;
            case KeyCode.F6:           return Key.F6;
            default:                   return Key.None;
        }
    }

    private static KeyCode MapInputSystemKeyToKeyCode(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
            return KeyCode.A + (key - Key.A);
        if (key >= Key.Digit1 && key <= Key.Digit9)
            return KeyCode.Alpha1 + (key - Key.Digit1);
        if (key == Key.Digit0) return KeyCode.Alpha0;

        switch (key)
        {
            case Key.Space:      return KeyCode.Space;
            case Key.LeftShift:  return KeyCode.LeftShift;
            case Key.RightShift: return KeyCode.RightShift;
            case Key.LeftCtrl:   return KeyCode.LeftControl;
            case Key.RightCtrl:  return KeyCode.RightControl;
            case Key.LeftAlt:    return KeyCode.LeftAlt;
            case Key.RightAlt:   return KeyCode.RightAlt;
            case Key.Tab:        return KeyCode.Tab;
            case Key.Enter:      return KeyCode.Return;
            case Key.Escape:     return KeyCode.Escape;
            case Key.UpArrow:    return KeyCode.UpArrow;
            case Key.DownArrow:  return KeyCode.DownArrow;
            case Key.LeftArrow:  return KeyCode.LeftArrow;
            case Key.RightArrow: return KeyCode.RightArrow;
            case Key.F1:         return KeyCode.F1;
            case Key.F2:         return KeyCode.F2;
            case Key.F3:         return KeyCode.F3;
            case Key.F4:         return KeyCode.F4;
            case Key.F5:         return KeyCode.F5;
            case Key.F6:         return KeyCode.F6;
            default:             return KeyCode.None;
        }
    }
#endif
}
