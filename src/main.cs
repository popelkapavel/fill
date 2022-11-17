using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace fill {
    static class main {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] arg){
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            fMain fm=new fMain(arg);
            Application.AddMessageFilter(fm);
            Application.Run(fm);
        }
    }
}
