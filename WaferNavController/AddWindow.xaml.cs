using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WaferNavController {
    /// <summary>
    /// Interaction logic for AddWindow.xaml
    /// </summary>
    public partial class AddWindow : BaseWindow {

        public AddWindow(Config configPage) {
            this.configPage = configPage;
            this.KeyDown += Esc_KeyDown;
            this.KeyDown += AddWindow_KeyDown;
            InitializeComponent();
        }

        private void AddWindow_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(AddButton);
                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            if (!AllTextBoxesHaveData()) {
                DialogResult = false;
                return;
            }
            var id = BarcodeTextBox.Text;
            var name = NameTextBox.Text;
            var description = DescriptionTextBox.Text;
            var location = LocationTextBox.Text;

            bool result;
            if (bluRadioButton.IsChecked != null && (bool) bluRadioButton.IsChecked) {
                result = DatabaseHandler.AddBlu(id, name, description, location);
            } else {
                result = DatabaseHandler.AddSlt(id, name, description, location);
            }
            DialogResult = result;
        }

        private bool AllTextBoxesHaveData() {
            return TextBoxHasData(BarcodeTextBox)
                && TextBoxHasData(NameTextBox)
                && TextBoxHasData(DescriptionTextBox)
                && TextBoxHasData(LocationTextBox);
        }

        private bool TextBoxHasData(TextBox textBox) {
            return !string.IsNullOrEmpty(textBox.Text) && textBox.Text != textBox.Tag.ToString();
        }
    }
}
