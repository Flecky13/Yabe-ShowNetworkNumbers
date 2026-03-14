using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        private readonly Button _toggleAllButton;
        private readonly Button _refreshButton;
        private readonly Panel _topPanel;
        private readonly Panel _diagramHostPanel;
        private readonly FlowLayoutPanel _routerRowPanel;

        public ShowNetworkNumbersForm(YabeMainDialog yabeFrm)
        {
            _yabeFrm = yabeFrm;

            Text = "ShowNetworkNumbers v" + Assembly.GetExecutingAssembly().GetName().Version;
            Width = 980;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;

            _toggleAllButton = new Button
            {
                Height = 30,
                Width = 130,
                Text = "Alle ausklappen",
                FlatStyle = FlatStyle.System
            };
            _toggleAllButton.Click += (sender, args) => ToggleAllNetworkColumns();

            _refreshButton = new Button
            {
                Height = 30,
                Width = 110,
                Text = "Refresh",
                FlatStyle = FlatStyle.System
            };
            _refreshButton.Click += (sender, args) => LoadNetworks();

            _topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(8, 5, 8, 5)
            };

            FlowLayoutPanel topButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight
            };
            topButtons.Controls.Add(_refreshButton);
            topButtons.Controls.Add(_toggleAllButton);
            _topPanel.Controls.Add(topButtons);

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
                Padding = new Padding(4)
            };
            _routerRowPanel.Paint += RouterRowPanel_Paint;
            _routerRowPanel.Resize += (sender, args) => _routerRowPanel.Invalidate();

            _diagramHostPanel.Controls.Add(_routerRowPanel);

            Controls.Add(_diagramHostPanel);
            Controls.Add(_topPanel);

            Load += (sender, args) => LoadNetworks();
        }

        private void LoadNetworks()
        {
            _routerRowPanel.Controls.Clear();

            List<BACnetDevice> devices = new List<BACnetDevice>(_yabeFrm.YabeDiscoveredDevices)
                .OrderBy(d => d.deviceId)
                .ToList();

            List<DeviceLayoutData> layoutDevices = devices
                .Select(CreateDeviceLayoutData)
                .OrderBy(d => d.RouterKey)
                .ThenBy(d => d.DeviceId)
                .ToList();

            IEnumerable<IGrouping<string, DeviceLayoutData>> routerGroups = layoutDevices
                .GroupBy(d => d.RouterKey)
                .OrderBy(g => g.Key);

            foreach (IGrouping<string, DeviceLayoutData> routerGroup in routerGroups)
                _routerRowPanel.Controls.Add(CreateRouterGroupControl(routerGroup));

            _routerRowPanel.PerformLayout();
            _routerRowPanel.Invalidate();

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

        private DeviceLayoutData CreateDeviceLayoutData(BACnetDevice device)
        {
            string macAddress = GetMacAddress(device);
            SnetResolution snetResolution = ResolveSnet(device, macAddress);
            int? snetNumber = snetResolution.Value;

            return new DeviceLayoutData
            {
                Device = device,
                DeviceId = device != null ? device.deviceId : uint.MaxValue,
                RouterKey = GetRouterKeyAddress(device),
                DisplayMac = macAddress,
                Snet = snetNumber,
                SnetText = snetNumber.HasValue ? snetNumber.Value.ToString() : "n/a"
            };
        }

        private Control CreateRouterGroupControl(IGrouping<string, DeviceLayoutData> routerGroup)
        {
            List<DeviceLayoutData> groupDevices = routerGroup
                .OrderBy(d => d.DeviceId)
                .ToList();

            DeviceLayoutData routerDevice = groupDevices
                .FirstOrDefault(d => !d.Snet.HasValue)
                ?? groupDevices.FirstOrDefault();

            FlowLayoutPanel groupPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(4),
                Padding = new Padding(8)
            };
            groupPanel.Paint += RouterGroupPanel_Paint;
            groupPanel.Resize += (sender, args) => groupPanel.Invalidate();

            FlowLayoutPanel contentPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            if (routerDevice != null)
            {
                Control routerCard = CreateRouterCard(routerDevice.Device, routerDevice.DisplayMac, routerDevice.SnetText);
                routerCard.Tag = "router-card";
                contentPanel.Controls.Add(routerCard);
            }

            FlowLayoutPanel columnsRow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };

            List<DeviceLayoutData> childDevices = groupDevices
                .Where(d => routerDevice == null || d.DeviceId != routerDevice.DeviceId)
                .ToList();

            List<DeviceLayoutData> withoutSnet = childDevices.Where(d => !d.Snet.HasValue).ToList();
            if (withoutSnet.Count > 0)
                columnsRow.Controls.Add(CreateSnetColumn("n/a", withoutSnet));

            IEnumerable<IGrouping<int, DeviceLayoutData>> snetGroups = childDevices
                .Where(d => d.Snet.HasValue)
                .GroupBy(d => d.Snet.Value)
                .OrderBy(g => g.Key);

            foreach (IGrouping<int, DeviceLayoutData> snetGroup in snetGroups)
                columnsRow.Controls.Add(CreateSnetColumn(snetGroup.Key.ToString(), snetGroup.OrderBy(d => d.DeviceId).ToList()));

            if (columnsRow.Controls.Count > 0)
            {
                columnsRow.Tag = "columns-row";
                contentPanel.Controls.Add(columnsRow);
            }

            groupPanel.Controls.Add(contentPanel);
            return groupPanel;
        }

        private Control CreateSnetColumn(string title, List<DeviceLayoutData> devices)
        {
            FlowLayoutPanel column = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 8, 0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            column.Tag = "snet-column";

            Button toggleButton = new Button
            {
                AutoSize = false,
                Width = 102,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 2),
                FlatStyle = FlatStyle.Flat,
                Text = "Netzwerk: " + title + "  [+]"
            };
            toggleButton.FlatAppearance.BorderColor = Color.FromArgb(160, 160, 160);
            toggleButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 235, 235);
            toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
            toggleButton.Tag = "snet-toggle";
            toggleButton.AccessibleDescription = title;

            FlowLayoutPanel cards = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0),
                Padding = new Padding(0),
                BackColor = Color.Transparent
            };
            cards.Tag = "snet-cards";
            cards.Visible = false;

            foreach (DeviceLayoutData device in devices)
            {
                Control childCard = CreateRouterCard(device.Device, device.DisplayMac, device.SnetText);
                childCard.Tag = "child-card";
                cards.Controls.Add(childCard);
            }

            toggleButton.Click += (sender, args) =>
            {
                cards.Visible = !cards.Visible;
                UpdateNetworkToggleButtonText(toggleButton, title, cards.Visible);
                _routerRowPanel.PerformLayout();
                _routerRowPanel.Invalidate();
                if (column.Parent != null)
                    column.Parent.Invalidate();
                column.Invalidate();
                UpdateGlobalToggleButtonText();
            };

            UpdateNetworkToggleButtonText(toggleButton, title, false);

            column.Controls.Add(toggleButton);
            column.Controls.Add(cards);
            return column;
        }

        private void ToggleAllNetworkColumns()
        {
            List<Tuple<Button, FlowLayoutPanel>> toggles = GetNetworkTogglePairs().ToList();
            if (toggles.Count == 0)
                return;

            bool expandAll = toggles.Any(p => !p.Item2.Visible);
            foreach (Tuple<Button, FlowLayoutPanel> pair in toggles)
            {
                pair.Item2.Visible = expandAll;
                string title = pair.Item1.AccessibleDescription ?? "n/a";
                UpdateNetworkToggleButtonText(pair.Item1, title, expandAll);
            }

            _routerRowPanel.PerformLayout();
            _routerRowPanel.Invalidate();
            UpdateGlobalToggleButtonText();
        }

        private IEnumerable<Tuple<Button, FlowLayoutPanel>> GetNetworkTogglePairs()
        {
            foreach (Control column in FindControlsByTag(_routerRowPanel, "snet-column"))
            {
                Button toggle = FindControlByTag(column, "snet-toggle") as Button;
                FlowLayoutPanel cards = FindControlByTag(column, "snet-cards") as FlowLayoutPanel;
                if (toggle != null && cards != null)
                    yield return Tuple.Create(toggle, cards);
            }
        }

        private void UpdateGlobalToggleButtonText()
        {
            List<Tuple<Button, FlowLayoutPanel>> toggles = GetNetworkTogglePairs().ToList();
            if (toggles.Count == 0)
            {
                _toggleAllButton.Text = "Alle ausklappen";
                return;
            }

            bool hasCollapsed = toggles.Any(p => !p.Item2.Visible);
            _toggleAllButton.Text = hasCollapsed ? "Alle ausklappen" : "Alle einklappen";
        }

        private static void UpdateNetworkToggleButtonText(Button button, string title, bool expanded)
        {
            button.Text = "Netzwerk: " + title + (expanded ? "  [-]" : "  [+]");
        }

        private void RouterRowPanel_Paint(object sender, PaintEventArgs e)
        {
            List<Control> routerCards = new List<Control>();
            foreach (Control group in _routerRowPanel.Controls)
            {
                Control card = FindControlByTag(group, "router-card");
                if (card != null)
                    routerCards.Add(card);
            }

            if (routerCards.Count < 2)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen linePen = new Pen(Color.FromArgb(120, 90, 90, 90), 1.6f))
            {
                List<Point> topCenters = routerCards
                    .Select(card => GetPointInParent(card, _routerRowPanel, new Point(card.Width / 2, 0)))
                    .OrderBy(p => p.X)
                    .ToList();

                int lineY = Math.Max(6, topCenters.Min(p => p.Y) - 14);
                e.Graphics.DrawLine(linePen, topCenters.First().X, lineY, topCenters.Last().X, lineY);

                foreach (Point topCenter in topCenters)
                    e.Graphics.DrawLine(linePen, topCenter.X, lineY, topCenter.X, topCenter.Y - 2);
            }
        }

        private void RouterGroupPanel_Paint(object sender, PaintEventArgs e)
        {
            Control groupPanel = sender as Control;
            if (groupPanel == null)
                return;

            Control routerCard = FindControlByTag(groupPanel, "router-card");
            Control columnsRow = FindControlByTag(groupPanel, "columns-row");
            if (routerCard == null || columnsRow == null)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen linePen = new Pen(Color.FromArgb(130, 70, 70, 70), 1.5f))
            {
                Point routerBottom = GetPointInParent(routerCard, groupPanel, new Point(routerCard.Width / 2, routerCard.Height));
                int branchY = routerBottom.Y + 10;

                List<Control> columns = columnsRow.Controls.Cast<Control>().ToList();
                if (columns.Count == 0)
                    return;

                List<Point> columnTopCenters = columns
                    .Select(c => GetPointInParent(c, groupPanel, new Point(c.Width / 2, 0)))
                    .OrderBy(p => p.X)
                    .ToList();

                List<int> columnCenters = columnTopCenters.Select(p => p.X).ToList();
                e.Graphics.DrawLine(linePen, routerBottom.X, routerBottom.Y, routerBottom.X, branchY);
                e.Graphics.DrawLine(linePen, columnCenters.First(), branchY, columnCenters.Last(), branchY);

                foreach (Control column in columns)
                {
                    FlowLayoutPanel cardsPanel = column.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                    Control toggleControl = FindControlByTag(column, "snet-toggle");

                    int columnCenterX;
                    Point columnTopCenter;
                    if (toggleControl != null)
                    {
                        columnTopCenter = GetPointInParent(toggleControl, groupPanel, new Point(toggleControl.Width / 2, 0));
                        columnCenterX = columnTopCenter.X;
                    }
                    else
                    {
                        columnTopCenter = GetPointInParent(column, groupPanel, new Point(column.Width / 2, 0));
                        columnCenterX = columnTopCenter.X;
                    }

                    e.Graphics.DrawLine(linePen, columnCenterX, branchY, columnCenterX, columnTopCenter.Y);

                    if (toggleControl == null || cardsPanel == null || !cardsPanel.Visible || cardsPanel.Controls.Count == 0)
                        continue;

                    int downStartY = GetPointInParent(toggleControl, groupPanel, new Point(toggleControl.Width / 2, toggleControl.Height)).Y;
                    Control firstCard = cardsPanel.Controls[0];
                    Control lastCard = cardsPanel.Controls[cardsPanel.Controls.Count - 1];
                    int firstCardY = GetPointInParent(firstCard, groupPanel, new Point(firstCard.Width / 2, 0)).Y;
                    int lastCardBottomY = GetPointInParent(lastCard, groupPanel, new Point(lastCard.Width / 2, lastCard.Height)).Y;

                    e.Graphics.DrawLine(linePen, columnCenterX, downStartY, columnCenterX, firstCardY - 3);
                    e.Graphics.DrawLine(linePen, columnCenterX, firstCardY + 2, columnCenterX, lastCardBottomY - 2);

                    foreach (Control card in cardsPanel.Controls)
                    {
                        Point cardCenter = GetPointInParent(card, groupPanel, new Point(card.Width / 2, card.Height / 2));
                        Point cardLeft = GetPointInParent(card, groupPanel, new Point(0, card.Height / 2));
                        e.Graphics.DrawLine(linePen, columnCenterX, cardCenter.Y, cardLeft.X + 2, cardCenter.Y);
                    }
                }
            }
        }

        private static Control FindControlByTag(Control root, string tag)
        {
            if (root == null)
                return null;

            foreach (Control control in root.Controls)
            {
                string controlTag = control.Tag as string;
                if (string.Equals(controlTag, tag, StringComparison.Ordinal))
                    return control;

                Control nested = FindControlByTag(control, tag);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static IEnumerable<Control> FindControlsByTag(Control root, string tag)
        {
            if (root == null)
                yield break;

            foreach (Control control in root.Controls)
            {
                string controlTag = control.Tag as string;
                if (string.Equals(controlTag, tag, StringComparison.Ordinal))
                    yield return control;

                foreach (Control nested in FindControlsByTag(control, tag))
                    yield return nested;
            }
        }

        private static Point GetCenterInParent(Control control, Control parent)
        {
            Point centerInScreen = control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
            return parent.PointToClient(centerInScreen);
        }

        private static Point GetPointInParent(Control control, Control parent, Point pointInControl)
        {
            Point pointInScreen = control.PointToScreen(pointInControl);
            return parent.PointToClient(pointInScreen);
        }

        private static string GetRouterKeyAddress(BACnetDevice device)
        {
            if (device == null || device.BacAdr == null)
                return "n/a";

            string value = device.BacAdr.ToString();
            return string.IsNullOrWhiteSpace(value) ? "n/a" : value;
        }

        private Control CreateRouterCard(BACnetDevice device, string macAddress, string snetText)
        {
            Panel card = new Panel
            {
                Width = 92,
                Height = 62,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(5)
            };

            Label macTitleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 10,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 6.6F, FontStyle.Bold),
                Text = "MAC"
            };

            Label macValueLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 12,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 6.2F, FontStyle.Regular),
                AutoEllipsis = true,
                Text = macAddress
            };

            Panel deviceSymbol = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(68, 114, 196)
            };

            Label deviceText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.6F, FontStyle.Bold),
                Text = device.deviceId.ToString()
            };
            deviceSymbol.Controls.Add(deviceText);

            Label networkNumbersLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 6.8F, FontStyle.Regular),
                Padding = new Padding(2, 1, 2, 1),
                Text = "SNET: " + snetText
            };

            card.Controls.Add(networkNumbersLabel);
            card.Controls.Add(macValueLabel);
            card.Controls.Add(macTitleLabel);
            card.Controls.Add(deviceSymbol);

            return card;
        }

        private static string GetMacAddress(BACnetDevice device)
        {
            if (device == null || device.BacAdr == null)
                return "n/a";

            if (device.BacAdr.RoutedSource != null && device.BacAdr.RoutedSource.net > 0)
            {
                string routedValue = device.BacAdr.RoutedSource.ToString();
                if (!string.IsNullOrWhiteSpace(routedValue))
                    return routedValue;
            }

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

        private static SnetResolution ResolveSnet(BACnetDevice device, string macAddress)
        {
            SnetResolution fromAddress = TryGetSnetFromAddressObject(device != null ? device.BacAdr : null);
            int? snet = fromAddress.Value;
            if (snet.HasValue)
                return fromAddress;

            int? fallback = TryExtractSnetFromText(macAddress);
            if (fallback.HasValue)
                return new SnetResolution(fallback, "Address.ToString fallback");

            return new SnetResolution(null, "not found");
        }

        private static SnetResolution TryGetSnetFromAddressObject(object address)
        {
            if (address == null)
                return new SnetResolution(null, "address=null");

            BacnetAddress bacnetAddress = address as BacnetAddress;
            if (bacnetAddress != null)
            {
                if (bacnetAddress.RoutedSource != null)
                {
                    ushort routedNet = bacnetAddress.RoutedSource.net;
                    if (routedNet > 0)
                        return new SnetResolution(routedNet, "BacAdr.RoutedSource.net");

                    return new SnetResolution(routedNet, "BacAdr.RoutedSource.net (0)");
                }

                if (bacnetAddress.net > 0)
                    return new SnetResolution(bacnetAddress.net, "BacAdr.net");
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;

            string[] preferredMemberNames =
            {
                "Snet",
                "SourceNetwork",
                "SourceNetworkNumber",
                "SourceNet",
                "Net",
                "net"
            };

            Type addressType = address.GetType();

            foreach (string memberName in preferredMemberNames)
            {
                int? value = TryGetNumericMemberValue(address, addressType, memberName, flags);
                if (value.HasValue && value.Value > 0)
                    return new SnetResolution(value, "member " + memberName);
            }

            foreach (PropertyInfo property in addressType.GetProperties(flags))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    continue;

                string name = property.Name ?? string.Empty;
                if (IsSnetMemberName(name))
                {
                    int? value = ToNetworkNumber(property.GetValue(address, null));
                    if (value.HasValue && value.Value > 0)
                        return new SnetResolution(value, "property " + name);
                }
            }

            foreach (FieldInfo field in addressType.GetFields(flags))
            {
                string name = field.Name ?? string.Empty;
                if (IsSnetMemberName(name))
                {
                    int? value = ToNetworkNumber(field.GetValue(address));
                    if (value.HasValue && value.Value > 0)
                        return new SnetResolution(value, "field " + name);
                }
            }

            return new SnetResolution(null, "no matching member value");
        }

        private static bool IsSnetMemberName(string memberName)
        {
            string name = (memberName ?? string.Empty).ToLowerInvariant();
            return name == "snet" || name == "net" || name == "sourcenetwork" || name == "sourcenetworknumber" || name == "sourcenet";
        }

        private static int? TryGetNumericMemberValue(object target, Type targetType, string memberName, BindingFlags flags)
        {
            PropertyInfo property = targetType.GetProperty(memberName, flags);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                int? propValue = ToNetworkNumber(property.GetValue(target, null));
                if (propValue.HasValue)
                    return propValue;
            }

            FieldInfo field = targetType.GetField(memberName, flags);
            if (field != null)
            {
                int? fieldValue = ToNetworkNumber(field.GetValue(target));
                if (fieldValue.HasValue)
                    return fieldValue;
            }

            return null;
        }

        private static int? ToNetworkNumber(object rawValue)
        {
            if (rawValue == null)
                return null;

            if (rawValue is byte)
                return (byte)rawValue;

            if (rawValue is sbyte)
            {
                sbyte signedByte = (sbyte)rawValue;
                return signedByte >= 0 ? (int?)signedByte : null;
            }

            if (rawValue is short)
            {
                short shortValue = (short)rawValue;
                return shortValue >= 0 ? (int?)shortValue : null;
            }

            if (rawValue is ushort)
                return (ushort)rawValue;

            if (rawValue is int)
            {
                int intValue = (int)rawValue;
                return intValue >= 0 && intValue <= ushort.MaxValue ? (int?)intValue : null;
            }

            if (rawValue is uint)
            {
                uint uintValue = (uint)rawValue;
                return uintValue <= ushort.MaxValue ? (int?)uintValue : null;
            }

            if (rawValue is long)
            {
                long longValue = (long)rawValue;
                return longValue >= 0 && longValue <= ushort.MaxValue ? (int?)longValue : null;
            }

            if (rawValue is ulong)
            {
                ulong ulongValue = (ulong)rawValue;
                return ulongValue <= ushort.MaxValue ? (int?)ulongValue : null;
            }

            if (rawValue is string)
                return TryExtractSnetFromText((string)rawValue);

            string asText = rawValue.ToString();
            return string.IsNullOrWhiteSpace(asText) ? null : TryExtractSnetFromText(asText);
        }

        private static int? TryExtractSnetFromText(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            Match snetMatch = Regex.Match(source, @"(?i)\bsnet\s*[:=#-]?\s*(\d+)\b");
            if (snetMatch.Success && snetMatch.Groups.Count > 1)
                return ParseNetworkNumber(snetMatch.Groups[1].Value);

            Match netMatch = Regex.Match(source, @"(?i)\b(?:source\s*network|source\s*net|network|net)\s*[:=#-]?\s*(\d+)\b");
            if (netMatch.Success && netMatch.Groups.Count > 1)
                return ParseNetworkNumber(netMatch.Groups[1].Value);

            return null;
        }

        private static int? ParseNetworkNumber(string text)
        {
            int parsed;
            if (!int.TryParse(text, out parsed))
                return null;

            return parsed >= 0 && parsed <= ushort.MaxValue ? (int?)parsed : null;
        }

        private struct SnetResolution
        {
            public int? Value { get; }
            public string Source { get; }

            public SnetResolution(int? value, string source)
            {
                Value = value;
                Source = source ?? string.Empty;
            }
        }

        private sealed class DeviceLayoutData
        {
            public BACnetDevice Device { get; set; }
            public uint DeviceId { get; set; }
            public string RouterKey { get; set; }
            public string DisplayMac { get; set; }
            public int? Snet { get; set; }
            public string SnetText { get; set; }
        }

    }
}
