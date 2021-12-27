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
        
        public static event EventHandler ClipboardUpdate;
        public static IODelegate Output;
        public static DateTime TimeSleep;
        public string UserName { get; set; }

        public ClipManager()
        {
            InitializeComponent();
            NativeMethods.SetParent(Handle, NativeMethods.HWND_MESSAGE);
            NativeMethods.AddClipboardFormatListener(Handle); 
            Output = PrintOutput;
        }

        private WebSocketClient wsClient { get; set; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardUpdate(null);
            }
            base.WndProc(ref m);
        }

        //private static IntPtr SetHook(LowLevelKeyboardProc proc)
        //{
        //    using (Process curProcess = Process.GetCurrentProcess())
        //    using (ProcessModule curModule = curProcess.MainModule)
        //    {
        //        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        //    }
        //}

        //private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        //private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) 
        //{
        //    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        //    {
        //        int vkCode = Marshal.ReadInt32(lParam);   
        //        string theKey = ((Keys)vkCode).ToString();
        //        if (theKey.Contains("ControlKey")) 
        //        {
        //            CONTROL_DOWN = true; 
        //        }
        //    }
        //    else if (nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
        //    {
        //        int vkCode = Marshal.ReadInt32(lParam);   
        //        string theKey = ((Keys)vkCode).ToString();
        //        if (theKey.Contains("ControlKey"))
        //        {
        //            CONTROL_DOWN = false;
        //        }
        //            else if (CONTROL_DOWN && theKey == "C")
        //        {
        //            try
        //            {
        //                if (Clipboard.ContainsAudio())
        //                {
        //                    Output(DataFormats.WaveAudio, Clipboard.GetAudioStream());
        //                }
        //                if (Clipboard.ContainsFileDropList())
        //                {
        //                    Output(DataFormats.FileDrop, Clipboard.GetFileDropList());
        //                }
        //                if (Clipboard.ContainsText())
        //                {
        //                    Output(DataFormats.Text, Clipboard.GetText());
        //                }
        //                if (Clipboard.ContainsImage())
        //                {
        //                    Output(DataFormats.Bitmap, Clipboard.GetImage());
        //                }
        //                if (Clipboard.ContainsData(DataFormats.Html))
        //                {
        //                    Output(DataFormats.Html, Clipboard.GetData(DataFormats.Html));
        //                }
        //            }
        //            catch {}
        //        }
        //    }
        //    return CallNextHookEx(_hookID, nCode, wParam, lParam); 
        //}

        private static void OnClipboardUpdate(EventArgs e)
        {
            var handler = ClipboardUpdate;
            if (handler != null)
            {
                handler(null, e);
            }
            if (TimeSleep.AddSeconds(1) >= DateTime.Now) return;
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

        private async void ClipManager_Load(object sender, EventArgs e)
        {
            wsClient = new WebSocketClient(this);
            Configurator.Generate();
            notifyIcon1.Visible = true;

            var userName = Configurator.GetUserName();
            var url = Configurator.GetUrl();

            UserName = userName;
            wsClient.Url = url;
            wsClient.Usuario = userName;
            wsClient.Initialize();
            await wsClient.ConnectAsync();
        }

        public async void PrintOutput(string format, object obj)
        {
            if (format == DataFormats.Text || format == DataFormats.Html)
            {
                await wsClient.SendMessageAsync(new ClipDTO
                {
                    UserName = UserName,
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
                    UserName = UserName,
                    Format = format,
                    Data = SigBase64
                });
            }
            obj = null;
            if (format == DataFormats.FileDrop) return;
            Application.DoEvents();
        }

        public void PrintInput(string format, object obj)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    if (format == DataFormats.Bitmap)
                    {
                        var dataObject = new DataObject();
                        if (Clipboard.ContainsText())
                        {
                            dataObject.SetData(DataFormats.Text, Clipboard.GetText());
                        }
                        byte[] imageBytes = Convert.FromBase64String(obj.ToString());
                        using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
                        {
                            Image image = Image.FromStream(ms, true);
                            dataObject.SetData(format, image);
                        }
                        if (Clipboard.ContainsData(DataFormats.Html))
                        {
                            dataObject.SetData(DataFormats.Html, Clipboard.GetData(DataFormats.Html));
                        }
                        TimeSleep = DateTime.Now;
                        Clipboard.SetDataObject(dataObject, true);
                        dataObject = null;
                    }
                    if (format == DataFormats.Text)
                    {
                        var dataObject = new DataObject();

                        dataObject.SetData(DataFormats.Text, obj);

                        if (Clipboard.ContainsImage())
                        {
                            dataObject.SetData(DataFormats.Bitmap, Clipboard.GetImage());
                        }
                        if (Clipboard.ContainsData(DataFormats.Html))
                        {
                            dataObject.SetData(DataFormats.Html, Clipboard.GetData(DataFormats.Html));
                        }
                        TimeSleep = DateTime.Now;
                        Clipboard.SetDataObject(dataObject, true);
                        dataObject = null;
                    }
                    if (format == DataFormats.Html)
                    {
                        var dataObject = new DataObject();
                        if (Clipboard.ContainsText())
                        {
                            dataObject.SetData(DataFormats.Text, Clipboard.GetText());
                        }
                        if (Clipboard.ContainsImage())
                        {
                            dataObject.SetData(DataFormats.Bitmap, Clipboard.GetImage());
                        }
                        dataObject.SetData(format, obj);
                        TimeSleep = DateTime.Now;
                        Clipboard.SetDataObject(dataObject, true);
                        dataObject = null;
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public void MessageOuput(string type, string message)
        {
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(2000);
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

        private void ClipManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            Visible = false;
            e.Cancel = true;
        }


        internal static class NativeMethods
        {
            // See http://msdn.microsoft.com/en-us/library/ms649021%28v=vs.85%29.aspx
            public const int WM_CLIPBOARDUPDATE = 0x031D;
            public static IntPtr HWND_MESSAGE = new IntPtr(-3);

            // See http://msdn.microsoft.com/en-us/library/ms632599%28VS.85%29.aspx#message_only
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
            // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        }

    }
}
