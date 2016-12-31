using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace Plainion.CI.Views
{
    public partial class BuildDefinitionView : UserControl
    {
        public BuildDefinitionView()
        {
            InitializeComponent();

            EventManager.RegisterClassHandler( typeof( UIElement ), Keyboard.PreviewGotKeyboardFocusEvent, ( KeyboardFocusChangedEventHandler )OnPreviewGotKeyboardFocus );
        }

        private void OnPreviewGotKeyboardFocus( object sender, KeyboardFocusChangedEventArgs e )
        {
            var control = sender as FrameworkElement;
            if( control == null )
            {
                return;
            }

            foreach( var child in Help.Children.OfType<FrameworkElement>() )
            {
                child.Visibility = Visibility.Collapsed;
            }

            if( control.Tag == null )
            {
                return;
            }

            var help = Help.Children.OfType<FrameworkElement>().SingleOrDefault( c => c.Name == control.Tag.ToString() );
            if( help == null )
            {
                return;
            }

            help.Visibility = Visibility.Visible;
        }
    }
}
