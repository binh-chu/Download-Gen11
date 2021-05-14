using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Lge.Tools.Download
{
    public enum QProtocol : int
    {
        Unknown = -1,
        Sahara = 0,
        Firehose = 1,
        All = 100,
    }

    public class ImageItem
    {
        public ImageItem()
        {
            this.IsExist = false;
            this.Enabled = false;
            this.Id = -8;
        }

        public ImageItem(string aName, int aId, QProtocol aProtocol)
        {
            this.Name = aName;
            this.Id = aId;
            this.Protocol = aProtocol;
            this.Progress = 0;
            this.Enabled = true;
        }

        public string Name { get; private set; }
        public int Id { get; private set; }
        public string FileName { get; set; }

        public override string ToString()
        {
            return string.Format("{0} [ID:{1}, P:{2}]", Name, Id, Protocol);
        }
        public bool Use { get; set; }
        public bool Dump { get; set; }
        public int Progress { get; set; }
        public QProtocol Protocol { get; private set; }
        public bool Erase { get; set; }
        // for UI dispplay control
        public bool IsExist { get; set; }
        public bool Enabled { get; set; }

        static public List<ImageItem> Load(string aXml)
        {
            try
            {
                List<ImageItem> items = new List<ImageItem>();

                var strXml = File.ReadAllText(aXml, Encoding.GetEncoding("euc-kr"));
                
                XmlDocument xml = new XmlDocument(); 
                xml.LoadXml(strXml);

                // sahara - 이미지들 정보
                XmlNode saharaImage = xml.SelectSingleNode("/configuration/downloader/sahara/images/image");
                if (saharaImage != null)
                {
                    var name = saharaImage["name"].InnerText;
                    var id = saharaImage["id"].InnerText.ToInt();

                    var item = new ImageItem(name, id, QProtocol.Sahara);
                    item.Use = true;
                    item.FileName = Helper.ProgrammerPath;
                    items.Add(item);
                }
                else
                {
                    throw new ApplicationException("configuration.xml에 sahara에 대한 <image>정보가 없습니다.");
                }

                // firehose configure
                var conf = xml.SelectSingleNode("/configuration/downloader/firehose/configure") as XmlElement;
                var loglevel = xml.SelectSingleNode("/configuration/downloader/loglevel") as XmlElement;
                Log.LogLevel = loglevel.InnerText.ToInt();

                // firehose - 이미지들 정보
                XmlNodeList fhImages = xml.SelectNodes("/configuration/downloader/firehose/images/image");

                foreach (XmlNode xn in fhImages)
                {
                    var xa = xn.Attributes;
                    var name =  xa["name"].Value;
                    var id = xa["id"].Value.ToInt();
                    
                    var item        = new ImageItem(name, id, QProtocol.Firehose);
                    item.Use        = xa["use"].Value.ToBool();
                    item.FileName   = xa["filename"].Value;
                    item.Erase      = xa["erase"].Value.ToBool();
                    items.Add(item);
                }

                Log.v("Loaded a file for downloader's configuration. ({0})", aXml);

                return items;
            }
            catch (Exception e)
            {
                Log.e("{0} XML Reading Exception: {1}", aXml, e.ToString());
            }

            return null;
        }

        public static bool AllErase { get; set; }
        public static int Reset { get; set; }
        public static string Dir { get; set; }
        public static bool SkipEraseEfs { get; set; }
        public static bool CCMOnly { get; set; } // jwoh CCM Only function
        public static int SelBoard { get; set; } // jwoh add User/Factory mode
        public static int SelFirehose { get; set; } // jwoh add GM Signing
        public static int SelBaudrate { get; set; } // jwoh add Baudrate
        public static int SelModel { get; set; } // jwoh add Model
        public static bool ECCCheck { get; set; } // jwoh add ECCCheck
        public static bool MCP2K { get; set; }
        public static bool MCP4K { get; set; }
        public static string ConfigXmlPath
        {
            get
            {
                return System.IO.Path.Combine(Dir, Helper.ConfigXmlFileName);
            }
        }

        static public bool Save(string aXml, List<ImageItem> aItems)
        {
            try
            {
                var strXml = File.ReadAllText(aXml);

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(strXml);

                // sahara - 이미지들 정보
                XmlNode saharaImage = xml.SelectSingleNode("/configuration/downloader/sahara/images/image");

                if (saharaImage != null)
                {
                    var name = saharaImage["name"].InnerText;
                    var id = saharaImage["id"].InnerText.ToInt();

                    var sitem = aItems.FirstOrDefault(x => x.Id == id && x.Name == name && x.Protocol == QProtocol.Sahara);
                    
                    if (sitem != null && sitem.Id >= 0)
                    {
                        sitem.FileName = Helper.ProgrammerPath; // jwoh add GM Signing
                        saharaImage["path"].InnerText = sitem.FileName;
                    }
                }

                // firehose - 이미지들 정보
                XmlNodeList fhImages = xml.SelectNodes("/configuration/downloader/firehose/images/image");

                foreach (XmlNode xn in fhImages)
                {
                    var xa = xn.Attributes;
                    var name = xa["name"].Value;
                    var id = xa["id"].Value.ToInt();

                    var sitem = aItems.FirstOrDefault(x => x.Id == id && x.Name == name && x.Protocol == QProtocol.Firehose);

                    if (sitem != null && sitem.Id >= 0)
                    {
                        xa["use"].Value = sitem.Use ?  "1" : "0";
                        xa["erase"].Value = sitem.Erase ? "1" : "0";
                        xa["dump"].Value = sitem.Dump ? "1" : "0";
                        xa["filename"].Value = sitem.FileName;
                    }
                }

                XmlNodeList fhprogram = xml.SelectNodes("/configuration/downloader/firehose/images/image/program");

                foreach (XmlNode xnp in fhprogram)
                {
                    var xpa = xnp.Attributes;
                    if (ImageItem.ECCCheck)
                    {
                        xpa["SECTOR_SIZE_IN_BYTES"].Value = "2112"; //2112
                    }
                    else
                    {
                        if(MCP4K)
                            xpa["SECTOR_SIZE_IN_BYTES"].Value = "4096"; //2048
                        else if(MCP2K)
                            xpa["SECTOR_SIZE_IN_BYTES"].Value = "2048";
                    }
                }

                var conf = xml.SelectSingleNode("/configuration/downloader/firehose/configure");
                if (conf["skipEraseEfs"] == null)
                {
                    var skipEraseEfsNode = xml.CreateElement("skipEraseEfs");
                    skipEraseEfsNode.InnerText = "1";
                    conf.AppendChild(skipEraseEfsNode);
                }

                conf["allErase"].InnerText = ImageItem.AllErase ? "1" : "0";
                conf["reset"].InnerText = ImageItem.Reset.ToString();
                conf["dir"].InnerText = ImageItem.Dir;
                conf["skipEraseEfs"].InnerText = ImageItem.SkipEraseEfs ? "1" : "0";
                xml.SelectSingleNode("/configuration/downloader/loglevel").InnerText = Log.LogLevel.ToString();

                xml.Save(aXml);
                Log.v("Save a file for downloader's configuration. ({0})", aXml);
            }
            catch (Exception e)
            {
                Log.e("{0} XML Writing exception: {1}", aXml, e.ToString());
                return false;
            }
            return true;
        }
    }
}
