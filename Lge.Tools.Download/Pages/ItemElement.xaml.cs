using Lge.Tools.Download.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Lge.Tools.Download
{
    /// <summary>
    /// ItemElement.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ItemElement : UserControl
    {
        public ItemElement()
        {
            InitializeComponent();
        }


        public TargetItem ItemInfo
        {
            get { return (TargetItem)GetValue(ItemInfoProperty); }
            set
            {
                SetValue(ItemInfoProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for ItemInfo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemInfoProperty =
            DependencyProperty.Register("ItemInfo", typeof(TargetItem), typeof(ItemElement), new PropertyMetadata(
                new TargetItem(0, null),
                new PropertyChangedCallback(OnItemInfoChanged)));

        private static void OnItemInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ItemElement me = d as ItemElement;
            TargetItem newInfo = (TargetItem)e.NewValue;
            TargetItem oldInfo = (TargetItem)e.OldValue;

            if (newInfo != oldInfo)
            {
                me.DataContext = newInfo;
            }
        }
    }

    public class IsLastItemConverter2 : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ContentPresenter contentPresenter = value as ContentPresenter;
            ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(contentPresenter);
            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(contentPresenter);
            return (index == (itemsControl.Items.Count - 1));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public IsLastItemConverter2() { }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    
}
