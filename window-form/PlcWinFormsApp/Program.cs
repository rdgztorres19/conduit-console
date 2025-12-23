using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sitas.Edge.Core;
using Sitas.Edge.EdgePlcDriver;
using Sitas.Edge.EdgePlcDriver.Messages;
using Microsoft.Extensions.Logging;

namespace PlcWinFormsApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

