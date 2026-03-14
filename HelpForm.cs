using System;
using System.Windows.Forms;

namespace ShowNetworkNumbers
{
    public class HelpForm : Form
    {
        public HelpForm()
        {
            this.Text = "Hilfe zu ShowNetworkNumbers";
            this.Size = new System.Drawing.Size(750, 650);
            var helpBox = new RichTextBox();
            helpBox.ReadOnly = true;
            helpBox.Dock = DockStyle.Fill;
            helpBox.Font = new System.Drawing.Font("Arial", 11);
            helpBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            helpBox.BorderStyle = BorderStyle.None;
            helpBox.Rtf =
                "{\\rtf1\\ansi\\deff0"
                + "{\\fonttbl{\\f0 Arial;}}"
                + "\\fs24 "
                + "{\\b ShowNetworkNumbers Plugin}\\par"
                + "{\\f0 ========================}\\par\\par"
                + "{\\f0 Dieses Plugin zeigt die BACnet-Netzwerktopologie der in Yabe gefundenen Geraete.}\\par"
                + "{\\f0 Die Darstellung erfolgt gruppiert nach Router und SNET (Source Network Number).}\\par\\par"
                + "{\\b Verwendung:}\\par"
                + "{\\f0 -----------}\\par"
                + "{\\f0 1. Scannen Sie zuerst Ihr BACnet-Netzwerk in Yabe.}\\par"
                + "{\\f0 2. Oeffnen Sie das Plugin ueber Plugins -> ShowNetworkNumbers.}\\par"
                + "{\\f0 3. Klicken Sie auf Refresh, um die aktuelle Geraeteliste zu laden.}\\par"
                + "{\\f0 4. Oeffnen Sie Netzwerkspalten mit [+] oder global mit Alle ausklappen.}\\par\\par"
                + "{\\b Hinweise:}\\par"
                + "{\\f0 ---------}\\par"
                + "{\\f0 - Das Plugin oeffnet nur, wenn bereits Geraete in Yabe gefunden wurden.}\\par"
                + "{\\f0 - Bei grossen Netzen stehen horizontale und vertikale Scrollbalken zur Verfuegung.}\\par\\par"
                + "{\\b Support:}\\par"
                + "{\\f0 --------}\\par"
                + "{\\f0 Bei Fragen oder Problemen wenden Sie sich bitte an den Entwickler oder konsultieren Sie die Projektdokumentation.}\\par\\par"
                + "{\\b Lizenz:}\\par"
                + "{\\f0 Siehe LICENSE im GitHub-Repository}\\par\\par"
                + "{\\b Autor:}\\par"
                + "{\\f0 Flecky13 (Pedro Tepe)}\\par\\par"
                + "{\\b Repository:}\\par"
                + "{\\field{\\*\\fldinst HYPERLINK \\\"https://github.com/Flecky13/Yabe-ShowNetworkNumbers\\\"}{\\fldrslt https://github.com/Flecky13/Yabe-ShowNetworkNumbers}}\\par\\par"
                + "}";
            this.Controls.Add(helpBox);
        }
    }
}
