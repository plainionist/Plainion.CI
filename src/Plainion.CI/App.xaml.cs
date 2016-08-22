using System.ComponentModel.Composition.Hosting;
using System.Windows;
using System.Windows.Threading;

namespace Plainion.CI
{
    public partial class App : Application
    {
        private CompositionContainer myContainer;

        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            Application.Current.Exit += OnShutdown;
            Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

            var catalog = new AssemblyCatalog( GetType().Assembly );
            myContainer = new CompositionContainer( catalog, CompositionOptions.DisableSilentRejection );

            myContainer.Compose( new CompositionBatch() );

            Application.Current.MainWindow = myContainer.GetExportedValue<Shell>();
            Application.Current.MainWindow.Show();
        }

        private void OnDispatcherUnhandledException( object sender, DispatcherUnhandledExceptionEventArgs e )
        {
            MessageBox.Show( e.Exception.ToString(), "Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error );

            e.Handled = true;
        }

        private void OnShutdown( object sender, ExitEventArgs e )
        {
            myContainer.Dispose();
        }
    }
}
