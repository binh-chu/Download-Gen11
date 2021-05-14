using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Lge.Tools.Download
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
		public App()
		{
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException; ;
		}

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.e("Unhandled Applicaton Exception: " + e.ToString());
        }

        public static void DoEvents()
		{
			Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,  new Action(delegate { }));
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.e("Unhandled Domain Exception: " + e.ToString());
        }

    }
}
