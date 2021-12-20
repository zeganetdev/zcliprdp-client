using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static zRDPClip.Delegate;

namespace zRDPClip
{
    public partial class ClipManager : Form
    {

        ///////////////////////////////////////////////////////////
        //A bunch of DLL Imports to set a low level keyboard hook
        ///////////////////////////////////////////////////////////
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        ////////////////////////////////////////////////////////////////
        //Some constants to make handling our hook code easier to read
        ////////////////////////////////////////////////////////////////
        private const int WH_KEYBOARD_LL = 13;                    //Type of Hook - Low Level Keyboard
        private const int WM_KEYDOWN = 0x0100;                    //Value passed on KeyDown
        private const int WM_KEYUP = 0x0101;                      //Value passed on KeyUp
        private static LowLevelKeyboardProc _proc = HookCallback; //The function called when a key is pressed
        private static IntPtr _hookID = IntPtr.Zero;
        private static bool CONTROL_DOWN = false;                 //Bool to use as a flag for control key


        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        public static IODelegate Output;
        private WebSocketClient wsClient { get; set; }

        public ClipManager()
        {
            InitializeComponent();
            _hookID = SetHook(_proc);
            Output = PrintOutput;
        }
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string theKey = ((Keys)vkCode).ToString();
                if (theKey.Contains("ControlKey"))
                {
                    CONTROL_DOWN = true;
                }
            }
            else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                string theKey = ((Keys)vkCode).ToString();
                if (theKey.Contains("ControlKey"))
                {
                    CONTROL_DOWN = false;
                }
                else if (CONTROL_DOWN && theKey == "C")
                {
                    try
                    {
                        if (Clipboard.ContainsAudio())
                        {
                            Output(DataFormats.WaveAudio, Clipboard.GetAudioStream());
                        }
                        if (Clipboard.ContainsFileDropList())
                        {
                            Output(DataFormats.FileDrop, Clipboard.GetFileDropList());
                        }
                        if (Clipboard.ContainsText())
                        {
                            Output(DataFormats.Text, Clipboard.GetText());
                        }
                        if (Clipboard.ContainsImage())
                        {
                            Output(DataFormats.Bitmap, Clipboard.GetImage());
                        }
                        if (Clipboard.ContainsData(DataFormats.Html))
                        {
                            Output(DataFormats.Html, Clipboard.GetData(DataFormats.Html));
                        }
                    }
                    catch { }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private async void ClipManager_Load(object sender, EventArgs e)
        {
            wsClient = new WebSocketClient();
            Configurator.Generate();
            notifyIcon1.Visible = true;

            var userName = Configurator.GetUserName();
            var url = Configurator.GetUrl();
            if (userName == null || userName.Trim().Length == 0)
            {
                Visible = true;
                btnConnect(true);
            }
            else {
                txtUsuario.Text = userName;
                wsClient.Url = url;
                wsClient.Usuario = userName;
                wsClient.Initialize();
                await wsClient.ConnectAsync();
                btnConnect(false);
                Visible = false;
            }
        }

        private void btnConnect(bool value)
        {
            btnConectar.Visible = value;
            btnDesconectar.Visible = !value;
        }

        public async void PrintOutput(string format, object obj)
        {
            if (format == DataFormats.Text || format == DataFormats.Html)
            {
                await wsClient.SendMessageAsync(new ClipDTO
                {
                    UserName = txtUsuario.Text,
                    Format = format,
                    Data = obj
                });
            }
            if (format == DataFormats.Bitmap) {
                string SigBase64;
                using (var ms = new MemoryStream())
                {
                    using (var bitmap = (Bitmap) obj)
                    {
                        bitmap.Save(ms, ImageFormat.Jpeg);
                        SigBase64 = Convert.ToBase64String(ms.GetBuffer()); 
                    }
                }
                await wsClient.SendMessageAsync(new ClipDTO
                {
                    UserName = txtUsuario.Text,
                    Format = format,
                    Data = SigBase64
                });
            }
            if (format == DataFormats.FileDrop) return;
            Application.DoEvents();
        }

        public static void PrintInput(string format, object obj)
        {
            Thread thread = new Thread(() => {
                try
                {
                    if (format == DataFormats.Bitmap)
                    {
                        byte[] imageBytes = Convert.FromBase64String(obj.ToString());
                        using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                        {
                            Image image = Image.FromStream(ms, true);
                            Clipboard.SetData(format, image);
                        }
                    }
                    if (format == DataFormats.Text)
                    {
                        Clipboard.SetData(format, obj);
                    }
                    if (format == DataFormats.Html)
                    {
                        var dataObject = new DataObject();
                        dataObject.SetText(Clipboard.GetText());
                        Application.DoEvents();
                        dataObject.SetData(format, obj);
                        Clipboard.SetDataObject(dataObject, true);
                    }
                }
                catch {}
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private async void btnConectar_Click(object sender, EventArgs e)
        {
            if (txtUsuario.Text.Length == 0)
            {
                MessageBox.Show("No debe ser vacio", "zClipManager");
                return;
            }
            wsClient.Url = Configurator.GetUrl();
            wsClient.Usuario = txtUsuario.Text;
            wsClient.Initialize();
            Configurator.SetUserName(txtUsuario.Text);
            await wsClient.ConnectAsync();
            btnConnect(false);
            Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                contextMenuStrip1.Show();
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Visible = true;
        }

        private async void btnDesconectar_Click(object sender, EventArgs e)
        {
            btnConnect(true);
            await wsClient.DisconnectAsync();
        }

        private void ClipManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            Visible = false;
            e.Cancel = true;
        }


    }
}
