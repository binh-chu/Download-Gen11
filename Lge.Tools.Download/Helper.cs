using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lge.Tools.Download
{
    public static class Helper
    {
        public static string AppDir
        {
            get
            {
                var fi = new System.IO.FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
                return fi.DirectoryName;
            }
        }

        public static readonly string ProtocolFileName = "DownloadLib.v2.dll";
        public static readonly string ConfigXmlFileName = "configuration.xml";
        static public readonly string ProgrammerName = "LGE_prog_nand_firehose_9x45.mbn"; // jwoh add GM Signing
        static public readonly string ProgrammerName_GM = "GM_prog_nand_firehose_9x45.mbn"; // jwoh add GM Signing

        public static string ProtocolDllPath
        {
            get
            {
                return System.IO.Path.Combine(AppDir, ProtocolFileName);
            }
        }

        public static string ProgrammerPath
        {
            get
            {
                if (ImageItem.SelFirehose == 0) // jwoh add GM Signing
                {
                    return System.IO.Path.Combine(AppDir, ProgrammerName);
                }
                else
                {
                    return System.IO.Path.Combine(AppDir, ProgrammerName_GM); // jwoh add GM Signing
                }
            }
        }

        public static string LogDirPath
        {
            get
            {
                var dirpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Gen11Downloader\Log";
                return dirpath;
            }
        }

        public static string LogFile
        {
            get
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Gen11Downloader\Log";
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);

                path += string.Format(@"\dl_{0}.log", DateTime.Now.ToString("yyyyMMdd"));

                return path;
            }
        }

        public static string TempConfigFile(Models.ITabModel aModel)
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Gen11Downloader";
            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            return path + @"\" + aModel.TabId.ToString() + ConfigXmlFileName;            
        }

        public static string MultiConfigDir
        {
            get
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Gen11Downloader";
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
                var dir = path + @"\" + "multimode";

                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                return dir;
            }
        }

        public static string MultiConfigPath(int aId)
        {
            var path = string.Format("{0}\\conf_{1}.xml", MultiConfigDir, aId);

            return path;
        }

    }
    
}
