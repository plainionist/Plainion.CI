using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace Plainion.CI.Views
{
    // http://wpftutorial.net/PasswordBox.html
    public static class PasswordBinder
    {
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached( "Password", typeof( SecureString ), typeof( PasswordBinder ),
            new FrameworkPropertyMetadata( null, OnPasswordPropertyChanged ) );

        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached( "Attach",
            typeof( bool ), typeof( PasswordBinder ), new PropertyMetadata( false, Attach ) );

        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached( "IsUpdating", typeof( bool ),
           typeof( PasswordBinder ) );


        public static void SetAttach( DependencyObject dp, bool value )
        {
            dp.SetValue( AttachProperty, value );
        }

        public static bool GetAttach( DependencyObject dp )
        {
            return (bool)dp.GetValue( AttachProperty );
        }

        public static SecureString GetPassword( DependencyObject dp )
        {
            return (SecureString)dp.GetValue( PasswordProperty );
        }

        public static void SetPassword( DependencyObject dp, SecureString value )
        {
            dp.SetValue( PasswordProperty, value );
        }

        private static bool GetIsUpdating( DependencyObject dp )
        {
            return (bool)dp.GetValue( IsUpdatingProperty );
        }

        private static void SetIsUpdating( DependencyObject dp, bool value )
        {
            dp.SetValue( IsUpdatingProperty, value );
        }

        private static void OnPasswordPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs e )
        {
            var passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;

            if ( !(bool)GetIsUpdating( passwordBox ) )
            {
                passwordBox.Password = ( (SecureString)e.NewValue ).ToUnsecureString();
            }
            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void Attach( DependencyObject sender, DependencyPropertyChangedEventArgs e )
        {
            var passwordBox = sender as PasswordBox;

            if ( passwordBox == null )
                return;

            if ( (bool)e.OldValue )
            {
                passwordBox.PasswordChanged -= PasswordChanged;
            }

            if ( (bool)e.NewValue )
            {
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        private static void PasswordChanged( object sender, RoutedEventArgs e )
        {
            var passwordBox = sender as PasswordBox;
            SetIsUpdating( passwordBox, true );
            SetPassword( passwordBox, passwordBox.SecurePassword );
            SetIsUpdating( passwordBox, false );
        }
    }
}
