using System;
using System.Diagnostics;
using System.Linq;
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
                if (_yabeFrm == null || _yabeFrm.YabeDiscoveredDevices == null || !_yabeFrm.YabeDiscoveredDevices.Any())
                {
                    MessageBox.Show(
                        _yabeFrm,
                        "Bitte zuerst BACnet-Geraete in Yabe suchen. Das Plugin wird erst mit gefundenen Geraeten geoeffnet.",
                        "ShowNetworkNumbers",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

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
