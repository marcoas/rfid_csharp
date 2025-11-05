using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ZK_RFID102demomain
{
    static class Program
    {
        // <summary>
        // El punto de entrada principal de la aplicación.。
        // </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}