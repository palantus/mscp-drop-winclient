using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Windows.Forms;
using System.Runtime.Serialization.Json;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FileDrop
{
    class Program
    {
        static void Main(string[] args)
        {
            WebClient myWebClient = new WebClient();
            byte[] responseArray = myWebClient.UploadFile("https://drop.ahkpro.dk/api/upload", args[0]);

            string responseStr = System.Text.Encoding.UTF8.GetString(responseArray);
            //MessageBox.Show(responseStr);

            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(responseStr), new System.Xml.XmlDictionaryReaderQuotas());

            // For that you will need to add reference to System.Xml and System.Xml.Linq
            var root = XElement.Load(jsonReader);
            XElement element = root.XPathSelectElement("//result");
            XElement firstFile = element.Elements().First();
            string raw = firstFile.Element("links").Element("raw").Value;
            string download = firstFile.Element("links").Element("download").Value;

            Form prompt = new Form()
            {
                Width = 450,
                Height = 210,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "File Drop",
                StartPosition = FormStartPosition.CenterScreen
            };

            Label textLabelDownload = new Label() { Left = 10, Top = 20, Text = "Download" };
            TextBox textBoxDownload = new TextBox() { Left = 10, Top = 40, Width = 400, Text = download };

            Label textLabelRaw = new Label() { Left = 10, Top = 70, Text = "Raw" };
            TextBox textBoxRaw = new TextBox() { Left = 10, Top = 90, Width = 400, Text = raw };

            Button confirmation = new Button() { Text = "Ok", Left = 175, Width = 100, Top = 130, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };

            prompt.Controls.Add(textBoxDownload);
            prompt.Controls.Add(textLabelDownload);
            prompt.Controls.Add(textBoxRaw);
            prompt.Controls.Add(textLabelRaw);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            prompt.ShowDialog();
        }
    }
}
