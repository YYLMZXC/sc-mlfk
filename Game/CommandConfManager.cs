using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Engine;
using XmlUtilities;
using Game;

namespace Mlfk
{
    public class CommandConfManager
    {
        public static bool Initialize()
        {
            string commandPath = DataHandle.GetCommandPath();
            string path = Storage.CombinePaths(commandPath, "Settings.xml");
            if (Storage.FileExists(path))
            {
                Stream stream = Storage.OpenFile(path, OpenFileMode.Read);
                try
                {
                    XElement xElement = XmlUtils.LoadXmlFromStream(stream, Encoding.UTF8, throwOnError: true);
                    stream.Dispose();
                    foreach (XElement item in xElement.Elements("Settings").ToList())
                    {
                        switch (item.Attribute("Name").Value)
                        {
                            case "WithdrawMode":
                                WithdrawBlockManager.WithdrawMode = item.Value.Contains("True");
                                break;
                            case "WithdrawSteps":
                                WithdrawBlockManager.MaxSteps = int.Parse(item.Value);
                                break;
                            case "ShowRay":
                                SubsystemCmdRodBlockBehavior.ShowRay = item.Value.Contains("True");
                                break;
                        }
                    }
                }
                catch
                {
                    stream.Dispose();
                    Log.Warning("Load Settings Fail");
                }

                return true;
            }

            SaveWhenDispose();
            foreach (ContentInfo item2 in ContentManager.List())
            {
                if (item2.AbsolutePath.StartsWith("Command/"))
                {
                    string systemPath = Storage.GetSystemPath(Storage.CombinePaths(commandPath, item2.Filename));
                    FileStream fileStream = new FileStream(systemPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    byte[] array = new byte[item2.ContentStream.Length];
                    item2.ContentStream.Read(array, 0, array.Length);
                    fileStream.Write(array, 0, array.Length);
                    fileStream.Flush();
                    fileStream.Dispose();
                }
            }

            return false;
        }

        public static void SaveWhenDispose()
        {
            string commandPath = DataHandle.GetCommandPath();
            string path = Storage.CombinePaths(commandPath, "Settings.xml");
            Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
            try
            {
                XElement xElement = new XElement("CommandConf");
                AddSaveAttribute(xElement, "WithdrawMode", WithdrawBlockManager.WithdrawMode.ToString());
                AddSaveAttribute(xElement, "WithdrawSteps", WithdrawBlockManager.MaxSteps.ToString());
                AddSaveAttribute(xElement, "ShowRay", SubsystemCmdRodBlockBehavior.ShowRay.ToString());
                XmlUtils.SaveXmlToStream(xElement, stream, Encoding.UTF8, throwOnError: true);
                stream.Dispose();
            }
            catch
            {
                stream.Dispose();
                Log.Warning("Save Settings Fail");
            }
        }

        public static void AddSaveAttribute(XElement xSettings, string name, string value)
        {
            XElement xElement = new XElement("Settings");
            XmlUtils.SetAttributeValue(xElement, "Name", name);
            xElement.Value = value;
            xSettings.Add(xElement);
        }
    }
}