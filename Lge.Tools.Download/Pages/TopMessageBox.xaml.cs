using System;
using System.Collections.Generic;
using System.Linq;
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
    /// TopMessageBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TopMessageBox : Window
    {
        public TopMessageBox()
        {
            this.Owner = Application.Current.MainWindow;

            InitializeComponent();
        }

        public string Message
        {
            get { return _msgText.Text; }
            set { _msgText.Text = value; }
        }

        public bool IsSuccess { get; set; }

        public static void ShowMsg(string aMsg, bool aIsSuccess)
        {
            Extension.UIThread(delegate {
                var box = new TopMessageBox();
                box.Message = aMsg;
                box.IsSuccess = aIsSuccess;
                box.ShowDialog();
            });
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            this.Close();
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);
            this.Close();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var child = Owner.Content as FrameworkElement;

            Point pos = new Point(0, 0);
            pos = child.PointToScreen(pos);

            var h = child.ActualHeight;
            var w = child.ActualWidth;

            this.Height = h;
            this.Width = w;

            this.Top = pos.Y;//this.Owner.Top + pos.Y;
            this.Left = pos.X;//this.Owner.Left + pos.X;

            _msgText.Foreground = (Brush)this.Resources[this.IsSuccess ? "okTextColor" : "errTextColor"];
            _mid.Background = (Brush)this.Resources[this.IsSuccess ? "okBackground" : "errBackground"];

            for (int i = 0; i < 10; i++)
            {
                var abox = new Anibox(this._top.ActualWidth, this._top.ActualHeight);
                this._top.Children.Add(abox);
            }
        }
    }
}
