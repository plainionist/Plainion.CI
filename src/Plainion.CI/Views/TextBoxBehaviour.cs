using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Plainion.CI.Views
{
    //http://stackoverflow.com/questions/10097417/how-do-i-create-an-autoscrolling-textbox
    public class TextBoxBehaviour
    {
        static readonly Dictionary<TextBox, Capture> myAssociations = new Dictionary<TextBox, Capture>();

        public static bool GetScrollOnTextChanged(DependencyObject dependencyObject)
        {
            return (bool)dependencyObject.GetValue(ScrollOnTextChangedProperty);
        }

        public static void SetScrollOnTextChanged(DependencyObject dependencyObject, bool value)
        {
            dependencyObject.SetValue(ScrollOnTextChangedProperty, value);
        }

        public static readonly DependencyProperty ScrollOnTextChangedProperty =
            DependencyProperty.RegisterAttached("ScrollOnTextChanged", typeof(bool), typeof(TextBoxBehaviour), new UIPropertyMetadata(false, OnScrollOnTextChanged));

        static void OnScrollOnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var textBox = dependencyObject as TextBox;
            if(textBox == null)
            {
                return;
            }
            bool oldValue = (bool)e.OldValue, newValue = (bool)e.NewValue;
            if(newValue == oldValue)
            {
                return;
            }
            if(newValue)
            {
                textBox.Loaded += TextBoxLoaded;
                textBox.Unloaded += TextBoxUnloaded;
            }
            else
            {
                textBox.Loaded -= TextBoxLoaded;
                textBox.Unloaded -= TextBoxUnloaded;
                if(myAssociations.ContainsKey(textBox))
                {
                    myAssociations[textBox].Dispose();
                }
            }
        }

        static void TextBoxUnloaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var textBox = (TextBox)sender;
            myAssociations[textBox].Dispose();
            textBox.Unloaded -= TextBoxUnloaded;
        }

        static void TextBoxLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var textBox = (TextBox)sender;
            textBox.Loaded -= TextBoxLoaded;
            myAssociations[textBox] = new Capture(textBox);
        }

        class Capture : IDisposable
        {
            private TextBox myTextBox;
            private bool myScrollingPending;

            public Capture(TextBox textBox)
            {
                myTextBox = textBox;
                myTextBox.TextChanged += OnTextBoxOnTextChanged;
            }

            private void OnTextBoxOnTextChanged(object sender, TextChangedEventArgs args)
            {
                // in order to avoid blocking the UI we queue the scrolling into the Dispatcher
                // but only if there is no pending scrolling
                if(!myScrollingPending)
                {
                    myScrollingPending = true;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        myTextBox.ScrollToEnd();
                        myScrollingPending = false;
                    }));
                }
            }

            public void Dispose()
            {
                myTextBox.TextChanged -= OnTextBoxOnTextChanged;
            }
        }

    }
}
