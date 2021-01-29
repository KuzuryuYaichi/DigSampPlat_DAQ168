using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace DAQ168
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Process[] ps = Process.GetProcessesByName("DAQ168");
            if (ps != null && ps.Length > 1)
            {
                DialogResult dr = MessageBox.Show("已经打开了一个实例，是否要继续打开","请确认",MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    return;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main_Frm());
        }
    }
}
