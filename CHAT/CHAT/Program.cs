using System;
using System.Windows.Forms;


namespace CHAT
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // .NET 6+ style
            Application.Run(new Form1());
        }
    }
}