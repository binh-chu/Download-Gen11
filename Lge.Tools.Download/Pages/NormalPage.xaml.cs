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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lge.Tools.Download
{
    /// <summary>
    /// NormalPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NormalPage : Page
    {
        public NormalPage()
        {
            InitializeComponent();

            _popItems.PreviewKeyDown += _popItems_PreviewKeyDown;
            _popVersion.PreviewKeyDown += _popItems_PreviewKeyDown;
        }

        private void _popItems_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((_popItems.IsOpen || _popVersion.IsOpen) && (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Cancel))
            {
                _popItems.IsOpen = false;
                _popVersion.IsOpen = false;
            }
        }

        public NormalModel Model
        {
            get
            {
                return this.DataContext as NormalModel;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectConfiguration();
        }

        private void button_VersionInfo_Click(object sender, RoutedEventArgs e)
        {
            // Popup version info
            _popVersion.IsOpen = true;
            vinfoList.Focus();

            if (Model.VersionInfo.Count <= 1)
            {
                Log.v("Version information is not received.");
            }

        }

        private void AllItems_Checked(object sender, RoutedEventArgs e)
        {
            bool check = (sender as CheckBox).IsChecked ?? false;
            
            foreach (var m in Model.UsedItems)
            {
                if (check)
                    m.Use = true;
                m.Enabled = !check;
            }
            _itemListView.Items.Refresh();
        }

        private void _btnEdit_Click(object sender, RoutedEventArgs e)
        {
            _popItems.IsOpen = true;
            _itemListView.Focus();

        }

        private void _popItems_Closed(object sender, EventArgs e)
        {
            // 선택된 요소에서 중복 아이템에도 적용.
            foreach (var m in _itemListView.ItemsSource as List<ImageItem>)
            {
                Model.Items.ForEach(x => {
                    bool found = x.FileName == m.FileName && m.Id != x.Id;
                    if (found)
                        x.Use = m.Use;
                });
            }            
        }

        private void MenuItemClear_Click(object sender, RoutedEventArgs e)
        {
            Model.ErrorMessages = null; // clear
        }

        private void _portDrop_Click(object sender, RoutedEventArgs e)
        {
            _popPorts.IsOpen = !_popPorts.IsOpen;
            if (_popPorts.IsOpen)
                _popPorts.Focus();
        }

        private void _portDrop_ClickMicom(object sender, RoutedEventArgs e)
        {
            _popPortsMicom.IsOpen = !_popPortsMicom.IsOpen;
            if (_popPortsMicom.IsOpen)
                _portListViewMicom.Focus();
        }

    }

}
