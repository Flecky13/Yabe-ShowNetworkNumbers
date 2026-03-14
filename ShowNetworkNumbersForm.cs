using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.BACnet;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Yabe;

namespace ShowNetworkNumbers
{
    public class ShowNetworkNumbersForm : Form
    {
        private readonly YabeMainDialog _yabeFrm;
        private readonly Button _refreshButton;
        private readonly Button _debugButton;
        private readonly Panel _topPanel;
        private readonly Panel _diagramHostPanel;
        private readonly FlowLayoutPanel _routerRowPanel;
        private string _lastDebugDump = string.Empty;

        public ShowNetworkNumbersForm(YabeMainDialog yabeFrm)
        {
            _yabeFrm = yabeFrm;

            Text = "ShowNetworkNumbers v" + Assembly.GetExecutingAssembly().GetName().Version;
            Width = 980;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;

            _refreshButton = new Button
            {
                Height = 30,
                Width = 110,
                Text = "Refresh"
            };
            _refreshButton.Click += (sender, args) => LoadNetworks();

            _debugButton = new Button
            {
                Height = 30,
                Width = 150,
                Text = "Debug anzeigen"
            };
            _debugButton.Click += (sender, args) => ShowDebugWindow();

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
            topButtons.Controls.Add(_debugButton);
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
                Padding = new Padding(8)
            };

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

            foreach (BACnetDevice device in devices)
            {
                string macAddress = GetMacAddress(device);
                SnetResolution snetResolution = ResolveSnet(device, macAddress);
                int? snetNumber = snetResolution.Value;
                string snetText = snetNumber.HasValue ? snetNumber.Value.ToString() : "n/a";
                _routerRowPanel.Controls.Add(CreateRouterCard(device, macAddress, snetText));
            }

            _lastDebugDump = BuildDebugDump(devices);

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

        private Control CreateRouterCard(BACnetDevice device, string macAddress, string snetText)
        {
            Panel card = new Panel
            {
                Width = 170,
                Height = 120,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(8)
            };

            Label macLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Text = "MAC: " + macAddress
            };

            Panel deviceSymbol = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(68, 114, 196)
            };

            Label deviceText = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Text = device.deviceId.ToString()
            };
            deviceSymbol.Controls.Add(deviceText);

            Label networkNumbersLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Padding = new Padding(4, 2, 4, 4),
                Text = "SNET: " + snetText
            };

            card.Controls.Add(networkNumbersLabel);
            card.Controls.Add(macLabel);
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

        private void ShowDebugWindow()
        {
            Form debugForm = new Form
            {
                Text = "ShowNetworkNumbers Debug",
                Width = 980,
                Height = 620,
                StartPosition = FormStartPosition.CenterParent
            };

            TextBox debugText = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                ReadOnly = true,
                WordWrap = false,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                Text = string.IsNullOrWhiteSpace(_lastDebugDump) ? "Keine Debugdaten. Bitte zuerst Refresh ausfuehren." : _lastDebugDump
            };

            Button copyButton = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Text = "In Zwischenablage kopieren"
            };
            copyButton.Click += (sender, args) =>
            {
                Clipboard.SetText(debugText.Text ?? string.Empty);
                MessageBox.Show(debugForm, "Debugtext wurde kopiert.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            debugForm.Controls.Add(debugText);
            debugForm.Controls.Add(copyButton);
            debugForm.Show(this);
        }

        private string BuildDebugDump(IEnumerable<BACnetDevice> devices)
        {
            StringBuilder sb = new StringBuilder();
            List<BACnetDevice> list = devices != null ? devices.ToList() : new List<BACnetDevice>();

            sb.AppendLine("ShowNetworkNumbers Debug Dump");
            sb.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Anzahl Geraete: " + list.Count);
            sb.AppendLine(new string('-', 80));

            foreach (BACnetDevice device in list)
            {
                string macAddress = GetMacAddress(device);
                object address = device != null ? device.BacAdr : null;
                SnetResolution snet = ResolveSnet(device, macAddress);

                sb.AppendLine("DeviceId: " + (device != null ? device.deviceId.ToString() : "n/a"));
                sb.AppendLine("BacAdr.ToString: " + macAddress);
                sb.AppendLine("SNET resolved: " + (snet.Value.HasValue ? snet.Value.Value.ToString() : "n/a"));
                sb.AppendLine("SNET source: " + snet.Source);
                sb.AppendLine("Address type: " + (address != null ? address.GetType().FullName : "null"));

                BacnetAddress bacAddress = address as BacnetAddress;
                if (bacAddress != null)
                {
                    sb.AppendLine("BacAdr.net: " + bacAddress.net);
                    sb.AppendLine("BacAdr.type: " + bacAddress.type);
                    sb.AppendLine("BacAdr.adr (hex): " + FormatByteArray(bacAddress.adr));
                    sb.AppendLine("BacAdr.VMac (hex): " + FormatByteArray(bacAddress.VMac));
                    sb.AppendLine("BacAdr.RoutedSource: " + (bacAddress.RoutedSource != null ? bacAddress.RoutedSource.ToString() : "<null>"));
                    if (bacAddress.RoutedSource != null)
                    {
                        sb.AppendLine("BacAdr.RoutedSource.net: " + bacAddress.RoutedSource.net);
                        sb.AppendLine("BacAdr.RoutedSource.type: " + bacAddress.RoutedSource.type);
                        sb.AppendLine("BacAdr.RoutedSource.adr (hex): " + FormatByteArray(bacAddress.RoutedSource.adr));
                        sb.AppendLine("BacAdr.RoutedSource.VMac (hex): " + FormatByteArray(bacAddress.RoutedSource.VMac));
                    }
                }

                foreach (string line in DescribeAddressMembers(address))
                    sb.AppendLine("  " + line);

                sb.AppendLine(new string('-', 80));
            }

            return sb.ToString();
        }

        private static IEnumerable<string> DescribeAddressMembers(object address)
        {
            if (address == null)
                yield break;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type addressType = address.GetType();
            HashSet<string> emitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in addressType.GetProperties(flags).OrderBy(p => p.Name))
            {
                if (!property.CanRead || property.GetIndexParameters().Length != 0)
                    continue;

                string name = property.Name ?? string.Empty;
                if (!ContainsDebugKeyword(name) || !emitted.Add("P:" + name))
                    continue;

                object rawValue = null;
                string error = null;
                try
                {
                    rawValue = property.GetValue(address, null);
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name;
                }

                if (!string.IsNullOrEmpty(error))
                    yield return "property " + name + " = <error: " + error + ">";
                else
                    yield return "property " + name + " = " + SafeToString(rawValue);
            }

            foreach (FieldInfo field in addressType.GetFields(flags).OrderBy(f => f.Name))
            {
                string name = field.Name ?? string.Empty;
                if (!ContainsDebugKeyword(name) || !emitted.Add("F:" + name))
                    continue;

                object rawValue = null;
                string error = null;
                try
                {
                    rawValue = field.GetValue(address);
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name;
                }

                if (!string.IsNullOrEmpty(error))
                    yield return "field " + name + " = <error: " + error + ">";
                else
                    yield return "field " + name + " = " + SafeToString(rawValue);
            }
        }

        private static bool ContainsDebugKeyword(string memberName)
        {
            string name = (memberName ?? string.Empty).ToLowerInvariant();
            return name.Contains("net") || name.Contains("source") || name.Contains("adr") || name.Contains("mac");
        }

        private static string SafeToString(object value)
        {
            if (value == null)
                return "<null>";

            byte[] bytes = value as byte[];
            if (bytes != null)
                return FormatByteArray(bytes);

            try
            {
                return value.ToString();
            }
            catch (Exception ex)
            {
                return "<error: " + ex.GetType().Name + ">";
            }
        }

        private static string FormatByteArray(byte[] bytes)
        {
            if (bytes == null)
                return "<null>";

            if (bytes.Length == 0)
                return "<empty>";

            return string.Join("-", bytes.Select(b => b.ToString("X2")));
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

    }
}
