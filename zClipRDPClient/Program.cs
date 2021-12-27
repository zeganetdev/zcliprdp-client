using System;
using System.Threading;
using System.Windows.Forms;

namespace zRDPClip
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            new Mutex(true, "zClipRDPClient", out createdNew);

            if (!createdNew) return;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.Run(new ClipManager());
        }
        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.ToString());
        }
    }
}
