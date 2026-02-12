namespace CAD2DModel.Interaction;

/// <summary>
/// Mouse button enumeration
/// </summary>
public enum MouseButton
{
    Left,
    Middle,
    Right,
    XButton1,
    XButton2
}

/// <summary>
/// Keyboard modifier keys
/// </summary>
[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// Keyboard keys enumeration (simplified)
/// </summary>
public enum Key
{
    None,
    Escape,
    Enter,
    Space,
    Delete,
    Backspace,
    Tab,
    // Letter keys
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    // Number keys
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    // Arrow keys
    Left, Right, Up, Down,
    // Other
    Home, End, PageUp, PageDown,
    Insert
}

/// <summary>
/// Cursor types
/// </summary>
public enum Cursor
{
    Arrow,
    Cross,
    Hand,
    IBeam,
    SizeAll,
    SizeNESW,
    SizeNS,
    SizeNWSE,
    SizeWE,
    Wait,
    PickBox  // Selection cursor with pick box indicator
}

/// <summary>
/// Selection filter flags for controlling what entities can be selected
/// </summary>
[Flags]
public enum SelectionFilter
{
    None = 0,
    Polylines = 1 << 0,
    Boundaries = 1 << 1,
    Vertices = 1 << 2,
    Segments = 1 << 3,
    All = Polylines | Boundaries | Vertices
}
