using System.Runtime.InteropServices;
using System.Text;

namespace OverlayApp;

public partial class Form1 : Form
{
    // --- Windows API constants for keyboard hooking ---
    private const int WH_KEYBOARD_LL = 13;              // Low-level keyboard hook identifier
    private const int WM_KEYDOWN = 0x0100;              // Windows message: key down
    private const int WM_KEYUP = 0x0101;                // Windows message: key up

    private UserSettings settings;

    // Form display
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    // --- Global variables for keyboard hooks ---
    private static IntPtr _hookID = IntPtr.Zero;        // Handle for the main keyboard hook
    private static LowLevelKeyboardProc? _proc;         // Delegate for main hook callback

    // --- For one-time hook after selecting a diacritical character ---
    private static IntPtr _diacriticalHookID = IntPtr.Zero; // Handle for diacritical keyboard hook
    private static LowLevelKeyboardProc? _diacriticalProc;  // Delegate for diacritical hook
    private static bool _diacriticalHandled = false;        // Prevent multiple handling of same key

    // --- Overlay window and key state tracking ---
    private OverlayForm? overlay;                    // Reference to the overlay UI
    private bool isHotkeyDown = false;               // Tracks if the hotkey is currently held

    // --- Define hotkey combination (Ctrl + Alt + A) ---
    private const Keys HotkeyKey = Keys.A;
    private const Keys HotkeyModifiers = Keys.Control | Keys.Shift;

    // --- Form constructor ---
    public Form1()
    {
        InitializeComponent();

        // Create a context menu for the tray icon
        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Exit", null, (s, e) => Application.Exit());

        // Create tray icon
        trayIcon = new NotifyIcon();
        trayIcon.Text = "Charli";
        trayIcon.Icon = new Icon("favicon.ico");
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (s, e) => ShowThisForm();

        // Assign and install the main keyboard hook
        _proc = HookCallback;
        _hookID = SetHook(_proc);

        this.ShowInTaskbar = false;
        this.WindowState = FormWindowState.Minimized;
        this.Opacity = 0;
        this.Hide();
        this.Text = "Charli";
        this.Icon = new Icon("favicon.ico");

        //Window Settings
        this.Width = 300;
        this.Height = 180;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;
        this.MinimizeBox = true;


        //Form Contents
        Label title = new Label();
        title.AutoSize = true;
        title.Text = "Settings";
        title.Location = new Point(10, 10);
        title.Font = new Font(title.Font.FontFamily, 16);
        this.Controls.Add(title);

        Label hotkeySettings = new Label();
        hotkeySettings.Text = "Hotkey";
        hotkeySettings.Location = new Point(10, 50);
        this.Controls.Add(hotkeySettings);

        FlowLayoutPanel panel = new FlowLayoutPanel();
        panel.FlowDirection = FlowDirection.LeftToRight;
        panel.AutoSize = true;
        panel.WrapContents = false;
        panel.Location = new Point(10, 70);
        this.Controls.Add(panel);

        CheckBox checkBoxCtrl = new CheckBox() { Text = "Ctrl", AutoSize = true };
        CheckBox checkBoxAlt = new CheckBox() { Text = "Alt", AutoSize = true };
        CheckBox checkBoxShift = new CheckBox() { Text = "Shift", AutoSize = true };
        ComboBox comboBoxMainKey = new ComboBox() { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

        //User Settings
        settings = UserSettings.Load();
        checkBoxCtrl.Checked = settings.UseCtrl;
        checkBoxAlt.Checked = settings.UseAlt;
        checkBoxShift.Checked = settings.UseShift;
        comboBoxMainKey.SelectedItem = settings.MainKey;

        panel.Controls.Add(checkBoxCtrl);
        panel.Controls.Add(checkBoxAlt);
        panel.Controls.Add(checkBoxShift);
        panel.Controls.Add(comboBoxMainKey);
        panel.Size = new Size(300, 30);

        var keys = Enum.GetValues(typeof(Keys))
            .Cast<Keys>()
            .Where(k => (k >= Keys.A && k <= Keys.Z))
            .ToList();

        comboBoxMainKey.DataSource = keys;

        Button saveButton = new Button() { Text = "Save", AutoSize = true };
        this.Controls.Add(saveButton);

        saveButton.Location = new Point(10, 100);
        saveButton.Click += (s, e) =>
        {
            settings.UseCtrl = checkBoxCtrl.Checked;
            settings.UseAlt = checkBoxAlt.Checked;
            settings.UseShift = checkBoxShift.Checked;
            settings.MainKey = (Keys)comboBoxMainKey.SelectedItem!;
            settings.Save();
            MessageBox.Show("Hotkey saved!", "Settings");
        };
    }

    private void ShowThisForm()
    {
        this.Opacity = 1;
        this.WindowState = FormWindowState.Normal;
        this.Show();
        this.BringToFront();
        this.ShowInTaskbar = true;
    }

    // --- Windows API imports for setting keyboard hooks ---
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // --- Delegate for keyboard hook callback ---
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    // --- Installs a keyboard hook using the given callback ---
    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule!)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    // --- Main keyboard hook callback ---
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            // Get virtual key code from lParam
            int vkCode = System.Runtime.InteropServices.Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            // Check current modifier state
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;
            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                // Hotkey just pressed
                if (!isHotkeyDown && key == settings.MainKey && ctrl == settings.UseCtrl && alt == settings.UseAlt && shift == settings.UseShift)
                {
                    isHotkeyDown = true;
                    this.BeginInvoke((Action)(() => ShowOverlay()));
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP)
            {
                // Hotkey released
                if (isHotkeyDown && key == HotkeyKey)
                {
                    isHotkeyDown = false;
                    this.BeginInvoke((Action)(() => HideOverlay()));
                }
            }
        }

        // Pass the event to next hook in the chain
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll")]
    private static extern int ToUnicode(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff,
        int cchBuff,
        uint wFlags
    );

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    // --- Show the overlay at the current mouse position ---
    private void ShowOverlay()
    {
        if (overlay == null || overlay.IsDisposed)
        {
            overlay = new OverlayForm();
        }

        overlay.UpdatePositionToMouse();
        overlay.Show();
        overlay.BringToFront();
    }

    // --- Hide the overlay and prepare diacritical hook if needed ---
    private void HideOverlay()
    {
        if (overlay != null && !overlay.IsDisposed)
        {
            // overlay.Hide();
        }

        // If user selected a diacritical, install a one-time hook to handle next key
        if (OverlayForm.SelectedDiacriticalIndex != null)
        {
            _diacriticalHandled = false;
            _diacriticalProc = DiacriticalHookCallback;
            _diacriticalHookID = SetHook(_diacriticalProc);
        }
    }

    // --- Callback to handle the next key press after diacritical selection ---
    private IntPtr DiacriticalHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (_diacriticalHandled)
            return CallNextHookEx(_diacriticalHookID, nCode, wParam, lParam);

        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = System.Runtime.InteropServices.Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            // Accept only letter or number keys
            if ((key >= Keys.A && key <= Keys.Z))
            {
                _diacriticalHandled = true;
                Console.WriteLine(key);

                // Remove the hook after use
                if (_diacriticalHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_diacriticalHookID);
                    _diacriticalHookID = IntPtr.Zero;
                }

                // Retrieve selected diacritical index
                int? idx = OverlayForm.SelectedDiacriticalIndex;

                if (idx != null)
                {
                    string output = "";
                    var outputs = typeof(OverlayApp.OverlayForm)
                        .GetField("sliceOutputs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.GetValue(overlay) as string[];

                    if (outputs == null) outputs = new string[] { };

                    if (idx.Value == 2) // Custom logic for stroke (e.g. đ, Đ)
                    {
                        if (key == Keys.D)
                            output = ((Control.ModifierKeys & Keys.Shift) == Keys.Shift) ? "Đ" : "đ";
                        else
                            output = ((char)vkCode).ToString(); // fallback
                    }
                    else if (idx.Value < outputs.Length)
                    {
                        // Compose base character with diacritical
                        string diacritical = outputs[idx.Value];

                        byte[] keyboardState = new byte[256];
                        GetKeyboardState(keyboardState);

                        uint scanCode = MapVirtualKey((uint)vkCode, 0);
                        StringBuilder sb = new StringBuilder(2);

                        ToUnicode((uint)vkCode, scanCode, keyboardState, sb, sb.Capacity, 0);
                        string keyChar = sb.ToString();
                        // string keyChar = ((char)vkCode).ToString();
                        output = keyChar + diacritical;
                    }

                    SendKeys.SendWait(output);
                }

                OverlayForm.SelectedDiacriticalIndex = null;
                return (IntPtr)1; // Prevent original key from being processed
            }
        }

        return CallNextHookEx(_diacriticalHookID, nCode, wParam, lParam);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
            this.ShowInTaskbar = false;
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    // --- Clean up the keyboard hook on form close ---
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnhookWindowsHookEx(_hookID);

        if (_diacriticalHookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_diacriticalHookID);
        }

        if (trayIcon != null)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();
        }

        base.OnFormClosed(e);
    }
}
