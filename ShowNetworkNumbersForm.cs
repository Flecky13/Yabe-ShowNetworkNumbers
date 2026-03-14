using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.BACnet;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Yabe;

namespace ShowNetworkNumbers
{
    public class ShowNetworkNumbersForm : Form
    {
        private readonly YabeMainDialog _yabeFrm;
        private readonly Button _refreshButton;
        private readonly Panel _diagramHostPanel;
        private readonly FlowLayoutPanel _routerRowPanel;

        public ShowNetworkNumbersForm(YabeMainDialog yabeFrm)
        {
            _yabeFrm = yabeFrm;

            Text = "ShowNetworkNumbers v" + Assembly.GetExecutingAssembly().GetName().Version;
            Width = 980;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;

            _refreshButton = new Button
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Refresh"
            };
            _refreshButton.Click += (sender, args) => LoadNetworks();

            _diagramHostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.WhiteSmoke,
                AutoScroll = true,
                Padding = new Padding(20)
            };

            _routerRowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 430,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(8)
            };

            _diagramHostPanel.Controls.Add(_routerRowPanel);

            Controls.Add(_diagramHostPanel);
            Controls.Add(_refreshButton);

            Load += (sender, args) => LoadNetworks();
        }

        private void LoadNetworks()
        {
            _routerRowPanel.Controls.Clear();

            List<BACnetDevice> devices = new List<BACnetDevice>(_yabeFrm.YabeDiscoveredDevices)
                .OrderBy(d => d.deviceId)
                .ToList();

            foreach (BACnetDevice device in devices)
            {
                _routerRowPanel.Controls.Add(CreateRouterCard(device));
            }

            if (_routerRowPanel.Controls.Count == 0)
            {
                Label empty = new Label
                {
                    AutoSize = true,
                    Text = "Keine Netzwerke gefunden.",
                    ForeColor = Color.DimGray,
                    Margin = new Padding(16)
                };
                _routerRowPanel.Controls.Add(empty);
            }
        }

        private Control CreateRouterCard(BACnetDevice device)
        {
            string macAddress = GetMacAddress(device);

            Panel card = new Panel
            {
                Width = 190,
                Height = 278,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(12)
            };

            Label ipLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = ExtractIpAddress(macAddress)
            };
            if (string.IsNullOrWhiteSpace(ipLabel.Text))
                ipLabel.Text = "IP: n/a";

            Label macLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Text = "MAC: " + macAddress
            };

            Panel deviceSymbol = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(68, 114, 196)
            };

            Label deviceText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Text = device.deviceId.ToString()
            };
            deviceSymbol.Controls.Add(deviceText);

            Label networkNumbersLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 78,
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(4, 6, 4, 4),
                Text = "Netzwerknummern\r\n" + string.Join(", ", ExtractNetworkNumbers(macAddress))
            };

            Label rawTextLabel = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.TopCenter,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                Padding = new Padding(6, 0, 6, 6),
                Text = "Address: " + macAddress
            };

            card.Controls.Add(rawTextLabel);
            card.Controls.Add(networkNumbersLabel);
            card.Controls.Add(deviceSymbol);
            card.Controls.Add(macLabel);
            card.Controls.Add(ipLabel);

            return card;
        }

        private static string GetMacAddress(BACnetDevice device)
        {
            if (device == null || device.BacAdr == null)
                return "n/a";

            string value = device.BacAdr.ToString();
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value;
        }

        private static string ExtractIpAddress(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            Match ipMatch = Regex.Match(input, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
            return ipMatch.Success ? ipMatch.Value : string.Empty;
        }

        private static IEnumerable<string> ExtractNetworkNumbers(string macAddress)
        {
            HashSet<string> values = new HashSet<string>();
            CollectNumbers(macAddress, values);

            if (values.Count == 0)
                values.Add("n/a");

            return values;
        }

        private static void CollectNumbers(string source, HashSet<string> values)
        {
            source = source ?? string.Empty;

            MatchCollection explicitNetMatches = Regex.Matches(source, @"(?i)\b(?:net|network)\s*[:=#-]?\s*(\d+)\b");
            foreach (Match match in explicitNetMatches)
            {
                if (match.Groups.Count > 1)
                    values.Add(match.Groups[1].Value);
            }

            MatchCollection bracketNetMatches = Regex.Matches(source, @"\((\d+)(?:-\d+)?\)");
            foreach (Match match in bracketNetMatches)
            {
                if (match.Groups.Count > 1)
                    values.Add(match.Groups[1].Value);
            }
        }

    }
}
