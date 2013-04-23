using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace WiiCoverDownloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            int cont = 0;

            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName == "WiiCoverDownloader")
                    cont++;
            }
            if (cont > 1)
            {
                MessageBox.Show("WiiCoverDownloader has been already opened/started.", "WiiCoverDownloader", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Environment.Exit(-1);
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WiiCoverDownloader());
        }
    }
}
