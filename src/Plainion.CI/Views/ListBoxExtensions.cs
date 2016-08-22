using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Plainion.CI.Views
{
    // https://social.msdn.microsoft.com/Forums/vstudio/en-US/0f524459-b14e-4f9a-8264-267953418a2d/trivial-listboxlistview-autoscroll
    // http://druss.co/2013/10/wpf-auto-scroll-behavior-for-listbox/
    // https://michlg.wordpress.com/2010/01/17/listbox-automatically-scroll-to-bottom/
    public class ListBoxExtensions : DependencyObject
    {
        public static readonly DependencyProperty AutoScrollToEndProperty = DependencyProperty.RegisterAttached("AutoScrollToEnd",
            typeof (bool), typeof (ListBoxExtensions), new UIPropertyMetadata(default(bool), OnAutoScrollToEndChanged));

        public static bool GetAutoScrollToEnd(DependencyObject obj)
        {
            return (bool) obj.GetValue(AutoScrollToEndProperty);
        }

        public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
        {
            obj.SetValue(AutoScrollToEndProperty, value);
        }

        public static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = d as ListBox;
            var listBoxItems = listBox.Items;
            var data = listBoxItems.SourceCollection as INotifyCollectionChanged;

            var scrollToEndHandler = new NotifyCollectionChangedEventHandler(
                (s1, e1) =>
                {
                    if (listBox.Items.Count > 0)
                    {
                        object lastItem = listBox.Items[listBox.Items.Count - 1];
                        listBoxItems.MoveCurrentTo(lastItem);
                        listBox.ScrollIntoView(lastItem);
                    }
                });

            if ((bool) e.NewValue)
            {
                data.CollectionChanged += scrollToEndHandler;
            }
            else
            {
                data.CollectionChanged -= scrollToEndHandler;
            }
        }
    }
}
