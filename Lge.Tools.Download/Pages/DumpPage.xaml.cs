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

namespace Lge.Tools.Download.Pages
{
    /// <summary>
    /// DumpPage.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class DumpPage : Page
    {
        public DumpPage()
        {
            InitializeComponent();

            _popItems.PreviewKeyDown += _popItems_PreviewKeyDown;
        }
        private void _popItems_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_popItems.IsOpen && (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Cancel))
            {
                _popItems.IsOpen = false;
            }
        }

        public DumpModel Model
        {
            get
            {
                return this.DataContext as DumpModel;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Model.SelectConfiguration();
        }

        private void AllItems_Checked(object sender, RoutedEventArgs e)
        {
            var cbox = sender as CheckBox;
            bool check = cbox.IsChecked ?? false;

            var items = Model.UsedItems;
            foreach (var m in items)
            {
                if (check)
                    m.Use = true;
                m.Enabled = !check;
            }
            _itemListView.ItemsSource = items;
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
                    bool found = x.Name == m.Name && m.Id != x.Id; // jwoh FileName->Name
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
                _portListView.Focus();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            _portListView.Items.Refresh();
        }
    }
}
