using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Lge.Tools.Download
{
    public class IsProgressedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((values[0] is ContentPresenter &&
                values[1] is int) == false)
            {
                return Visibility.Collapsed;
            }
            bool checkNextItem = System.Convert.ToBoolean(parameter.ToString());
            ContentPresenter contentPresenter = values[0] as ContentPresenter;
            int progress = (int)values[1];
            ItemsControl itemsControl = ItemsControl.ItemsControlFromItemContainer(contentPresenter);
            if (itemsControl == null)
                return Visibility.Collapsed;

            int index = itemsControl.ItemContainerGenerator.IndexFromContainer(contentPresenter);
            if (checkNextItem == true)
            {
                index++;
            }
            WizardProgressBar wizardProgressBar = itemsControl.TemplatedParent as WizardProgressBar;
            int percent = (int)(((double)index / wizardProgressBar.Items.Count) * 100);
            if (percent < progress)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class IsLastItemConverter : MarkupExtension, IValueConverter
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

        public IsLastItemConverter() { }
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
