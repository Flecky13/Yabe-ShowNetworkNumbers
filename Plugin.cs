using System;
using System.Diagnostics;
using System.Windows.Forms;
using Yabe;

namespace ShowNetworkNumbers
{
    public class Plugin : IYabePlugin
    {
        private YabeMainDialog _yabeFrm;

        public void Init(YabeMainDialog yabeFrm)
        {
            _yabeFrm = yabeFrm;

            ToolStripMenuItem menuItem = new ToolStripMenuItem
            {
                Text = "ShowNetworkNumbers"
            };
            menuItem.Click += MenuItem_Click;

            yabeFrm.pluginsToolStripMenuItem.DropDownItems.Add(menuItem);
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Trace.WriteLine("Loading ShowNetworkNumbers window...");
                ShowNetworkNumbersForm form = new ShowNetworkNumbersForm(_yabeFrm);
                form.Show();
            }
            catch
            {
                Cursor.Current = Cursors.Default;
                Trace.Fail("Failed to load the ShowNetworkNumbers window.");
            }
        }
    }
}
