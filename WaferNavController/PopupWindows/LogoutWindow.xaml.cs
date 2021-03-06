using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for LogoutWindow.xaml
    /// </summary>
    public partial class LogoutWindow : BaseWindow {

        public LogoutWindow() {
            statusLogConfig = StatusLogConfig.Get();
            KeyDown += Esc_KeyDown;
            KeyDown += LogoutWindow_KeyDown;
            InitializeComponent();
        }

        private void LogoutWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                var peer = new ButtonAutomationPeer(LogoutButton);
                var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv?.Invoke();
            }
        }

        private void LogoutButton_Clicked(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void CancelButton_Clicked(object sender, RoutedEventArgs e) {
            DialogResult = false;
        }

        private void LogoutWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            DialogResult = false;
        }
    }
}
