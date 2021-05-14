using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Lge.Tools.Download
{
    public static class Extension
    {
        public static int ToInt(this string aText)
        {
            int number;

            if (aText.StartsWith("0x"))
            {
                var hex = aText.Substring(2);
                int.TryParse(aText.Substring(2), System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture, out number);
            }
            else
            {
                if (!int.TryParse(aText, out number))
                    number = 0;
            }

            return number;
        }

        public static bool ToBool(this string aText)
        {
            bool ret;
            if (Boolean.TryParse(aText, out ret))
                return ret;

            int ivalue = aText.ToInt();

            return ivalue != 0;
        }

        static public void UIThread(this Models.ITabModel aModel, Action code)
        {
            if (!MainModel.Window.Dispatcher.CheckAccess())
            {
                MainModel.Window.Dispatcher.BeginInvoke(code);
                return;
            }
            code.Invoke();
        }

        static public void UIThread(Action code)
        {
            if (!MainModel.Window.Dispatcher.CheckAccess())
            {
                MainModel.Window.Dispatcher.BeginInvoke(code);
                return;
            }
            code.Invoke();
        }

        static public void UIThread(this FrameworkElement aElement, Action code)
        {
            if (!aElement.Dispatcher.CheckAccess())
            {
                aElement.Dispatcher.BeginInvoke(code);
                return;
            }
            code.Invoke();
        }

        static public void UIThreadInvoke(this Models.ITabModel aModel, Action code)
        {
            if (!MainModel.Window.Dispatcher.CheckAccess())
            {
                MainModel.Window.Dispatcher.Invoke(code);
                return;
            }
            code.Invoke();
        }

        static public string Strings(this IDictionary<string, string> aMap)
        {
            var strs = new StringBuilder();
            foreach(var m in aMap)
            {
                strs.AppendFormat("({0}={1}) ", m.Key, m.Value);
            }
            return strs.ToString();
        }

    }

    public class ScrollingTextBox : TextBox
    {
        public bool AutoScroll { get; set; }
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            this.AutoScroll = true;
        }

        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (this.AutoScroll)
            {
                CaretIndex = Text.Length;
                ScrollToEnd();
            }
        }

    }

}
