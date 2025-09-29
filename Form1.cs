using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BusInformer
{
    public partial class MainForm : Form
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string[] messages;
        private static int index = 0;
        private static MainForm instance;

        public MainForm()
        {
            InitializeComponent();
            instance = this;

            messages = new string[]
            {
                "do [Информатор ] Автобус следует по маршруту 35",
                "do [Информатор ] Следующая остановка: Автошкола",
                "do [Информатор ] Осторожно, двери закрываются!",
                "do [Информатор ] Конечная остановка: Вокзал"
            };
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(messages[index]);
            lblStatus.Text = $"В буфере: {messages[index]}";

            _hookID = SetHook(_proc);
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            lblStatus.Text = "Остановлено.";
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    proc,
                    GetModuleHandle(curModule.ModuleName),
                    0
                );
            }
        }

        // --- CALLBACK ДЛЯ ХУКА ---
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                if ((Control.ModifierKeys & Keys.Control) == Keys.Control && vkCode == (int)Keys.V)
                {
                    index = (index + 1) % messages.Length;
                    Clipboard.SetText(messages[index]);

                    instance.Invoke(new Action(() =>
                    {
                        instance.lblStatus.Text = $"В буфере: {messages[index]}";
                    }));
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // --- WinAPI ---
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
