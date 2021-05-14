using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Lge.Tools.Download.Pages
{
    /// <summary>
    /// AppInfoBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AppInfoBox : Window
    {
        public AppInfoBox()
        {
            LoadInfo();

            InitializeComponent();

            this.Owner = Application.Current.MainWindow;
        }

        void LoadInfo()
        {
            var asm = Assembly.GetExecutingAssembly();
            var fi = FileVersionInfo.GetVersionInfo(asm.Location);
            var clrVersion = asm.ImageRuntimeVersion;
            string assemblyVersion = asm.GetName().Version.ToString();
            string fileVersion = fi.FileVersion;
            string productVersion = fi.ProductVersion;

            var vers = new List<string>();
            vers.Add("Assembly Version: " + assemblyVersion);
            vers.Add("File Version: " + fileVersion);
            vers.Add("Product Version: " + productVersion);
            vers.Add("CLR Version: " + clrVersion);
            // read protocol lib.dll version
            var tw = new TargetWrapper();
            if (tw.Load(Helper.ProtocolDllPath))
            {
                int pver = tw.GetVersion();
                vers.Add("Protocol Lib: " + string.Format("v{0}.{1:00}", pver/100, pver%100));
            }

            var myAttribute = asm.GetCustomAttributes(typeof(CustomVesionDateAttribute), false)
                                    .Cast<CustomVesionDateAttribute>().First();
            var prjAttribute = asm.GetCustomAttributes(typeof(CustomProjectAttribute), false)
                                    .Cast<CustomProjectAttribute>().First();
            var reqCcm = asm.GetCustomAttributes(typeof(CustomRequiredCcmVersionAttribute), false)
                                    .Cast<CustomRequiredCcmVersionAttribute>().First();
            var reqMicom = asm.GetCustomAttributes(typeof(CustomRequiredMicomVersionAttribute), false)
                                    .Cast<CustomRequiredMicomVersionAttribute>().First();


            this.Date = myAttribute.ToString();

            vers.Add("Project: " + prjAttribute);            
            vers.Add(reqCcm.ToString());
            vers.Add(reqMicom.ToString());

            this.Author = "telematics2team@lge.com";

            this.Versions = vers;

            this.PName = asm.GetName().Name;
        }

        public string PName { get; set; }
        
        public string Author { get; private set; }
        public string Date { get; private set; }
        public List<String> Versions { get; private set; }
    }
}
