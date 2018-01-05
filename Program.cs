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
using System.IO;

namespace FileDrop
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                uploadFile(args[0]);
            } else
            {
                attemptDownloadFileUsingClipboard();
            }
        }

        private static void attemptDownloadFileUsingClipboard()
        {
            string text;
            try
            {
                text = getClipboardText();
            }catch(Exception e)
            {
                return;
            }

            if (text != null && text.StartsWith("https://drop."))
            {
                string hash = text.Substring(text.LastIndexOf("/")+1);
                
                WebClient myWebClient = new WebClient();
                string filename = getFilename(hash);

                if (!File.Exists(filename))
                {
                    myWebClient.DownloadFile("https://drop.ahkpro.dk/api/download/" + hash, filename);
                    MessageBox.Show("File downloaded to: " + filename);
                }
                else
                {
                    MessageBox.Show("File not downloaded, because '" + filename + "' already exists");
                }
            }

        }

        private static string getClipboardText()
        {
            string idat = null;
            Exception threadEx = null;
            System.Threading.Thread staThread = new System.Threading.Thread(
                delegate ()
                {
                    try
                    {
                        idat = Clipboard.GetText();
                    }

                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                });
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.Start();
            staThread.Join();
            return idat;
        }

        private static void setClipboardText(string text)
        {
            Exception threadEx = null;
            System.Threading.Thread staThread = new System.Threading.Thread(
                delegate ()
                {
                    try
                    {
                        Clipboard.SetText(text);
                    }

                    catch (Exception ex)
                    {
                        threadEx = ex;
                    }
                });
            staThread.SetApartmentState(System.Threading.ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        private static string md5file(string filename)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private static string getFilename(string hash)
        {
            WebClient myWebClient = new WebClient();
            Stream stream = myWebClient.OpenRead("https://drop.ahkpro.dk/api/file/" + hash);

            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(stream, new System.Xml.XmlDictionaryReaderQuotas());
            var root = XElement.Load(jsonReader);
            return root.XPathSelectElement("//result/filename").Value;
        }

        private static bool fileExists(string hash)
        {
            WebClient myWebClient = new WebClient();
            Stream stream = myWebClient.OpenRead("https://drop.ahkpro.dk/api/exists/" + hash);

            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(stream, new System.Xml.XmlDictionaryReaderQuotas());
            var root = XElement.Load(jsonReader);
            return root.XPathSelectElement("//result").Value == "true";
        }

        private static byte[] readStreamFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private static void uploadFile(string filename)
        {
            WebClient myWebClient = new WebClient();
            string responseStr;
            byte[] responseArray;
            bool expectArray = true;
            
            string hash = md5file(filename);
            if (fileExists(hash))
            {
                //MessageBox.Show("https://drop.ahkpro.dk/api/file/" + hash);
                Stream stream = myWebClient.OpenRead("https://drop.ahkpro.dk/api/file/" + hash);
                responseArray = readStreamFully(stream);
                expectArray = false;
            }
            else
            {
                responseArray = myWebClient.UploadFile("https://drop.ahkpro.dk/api/upload", filename);
            }

            responseStr = System.Text.Encoding.UTF8.GetString(responseArray);
            
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(responseStr), new System.Xml.XmlDictionaryReaderQuotas());

            // For that you will need to add reference to System.Xml and System.Xml.Linq
            var root = XElement.Load(jsonReader);
            XElement element = root.XPathSelectElement("//result");

            if(expectArray && element.Elements().Count() < 1)
            {
                MessageBox.Show("Cannot upload empty files");
                return;
            }

            XElement firstFile;
            if (expectArray)
                firstFile = element.Elements().First();
            else
                firstFile = element;
            string raw = firstFile.Element("links").Element("raw").Value;
            string download = firstFile.Element("links").Element("download").Value;

            setClipboardText(download);

            Form prompt = new Form()
            {
                Width = 450,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "File Drop",
                StartPosition = FormStartPosition.CenterScreen
            };
            
            Label textLabelDownload = new Label() { Left = 10, Top = 20, Text = "Download:" };
            TextBox textBoxDownload = new TextBox() { Left = 10, Top = 40, Width = 400, Text = download };

            Label textLabelRaw = new Label() { Left = 10, Top = 70, Text = "Raw:" };
            TextBox textBoxRaw = new TextBox() { Left = 10, Top = 90, Width = 400, Text = raw };

            Label textLabelClipboardNotice = new Label() { Left = 10, Top = 120, Width = 400, Text = "Note: Download link copied to clipboard!" };

            Button confirmation = new Button() { Text = "Ok", Left = 175, Width = 100, Top = 145, DialogResult = DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            
            prompt.Controls.Add(textBoxDownload);
            prompt.Controls.Add(textLabelDownload);
            prompt.Controls.Add(textBoxRaw);
            prompt.Controls.Add(textLabelRaw);
            prompt.Controls.Add(textLabelClipboardNotice);
            prompt.Controls.Add(confirmation);

            prompt.AcceptButton = confirmation;

            prompt.ShowDialog();

        }
    }
}
