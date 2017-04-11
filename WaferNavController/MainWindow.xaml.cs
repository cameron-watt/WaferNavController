using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace WaferNavController {
    public partial class MainWindow : Window {
        //Adding a comment to test a first commit - Cameron Watt
        private readonly string BROKER_URL = "iot.eclipse.org"; // Defaults to port 1883
        private readonly string CLIENT_ID = Guid.NewGuid().ToString();
        private readonly string PUB_TOPIC = "wafernav/location_data";
        private readonly string SUB_TOPIC = "wafernav/location_requests";
        private readonly MqttClient mqttClient;
        private bool killMakeDotsThread = false;

        public MainWindow() {
            InitializeComponent();

            var bmp = Properties.Resources.nielsen_ninjas_LogoTranspBack;
            var hBitmap = bmp.GetHbitmap();
            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            Icon = wpfBitmap;

            AppendLine("ClIENT ID: " + CLIENT_ID);

            //TODO: move mqtt logic to app.xaml.cs hopefully possibly. The idea being this main window won't always be active.
            mqttClient = new MqttClient(BROKER_URL);
            mqttClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            mqttClient.Connect(CLIENT_ID);
            mqttClient.Subscribe(new[] {SUB_TOPIC}, new[] {MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE});
            AppendLine("Subscribed to " + SUB_TOPIC);
        }

        /// <summary>
        /// This method directs incoming mqtt messages to be further processed.
        /// </summary>
        /// <param name="directive">Enum-like string that directs what to do with messages.</param>
        /// <param name="messages">Contents of message.</param>
        private void incomingMessageProcessor(String directive, List<String> messages)
        {
            switch (directive)
            {
                case "GET_NEW_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                case "ACCEPT_NEW_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                case "COMPLETE_NEW_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                case "GET_NEW_SLT":
                    Console.WriteLine("Placeholder");
                    break;
                case "ACCEPT_NEW_SLT":
                    Console.WriteLine("Placeholder");
                    break;
                case "COMPLETE_NEW_SLT":
                    Console.WriteLine("Placeholder");
                    break;
                case "GET_DONE_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                case "ACCEPT_DONE_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                case "COMPLETE_DONE_BLU":
                    Console.WriteLine("Placeholder");
                    break;
                default:
                    Console.Error.WriteLine("Method incomingMessageProcessor default statement reached.");
                    break;
            }
        }

        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e) {
            // Print received message to window
            var receivedJsonStr = Encoding.UTF8.GetString(e.Message, 0, e.Message.Length);
            Dispatcher.Invoke(() => {
                textBlock.Text += DateTime.Now + "  Message arrived.  Topic: " + e.Topic + "  Message: '" + receivedJsonStr + "'" + "\n";
                scrollViewer.ScrollToVerticalOffset(double.MaxValue);
            });

            //Placing main processing method below, assuming this is where it goes. CHECK THIS.
            incomingMessageProcessor("directive placeholder", new List<string>());

            // Process mqtt message to get desired ID
            var resultMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(receivedJsonStr);
            var bibId = resultMap["id"];

            // Get first available BLU id
            var bluId = DatabaseHandler.GetFirstAvailableBluId();

            //HACK: Reset database and try again if no available BLUs
            if (bluId == null) {
                DatabaseHandler.ResetDatabase();
                bluId = DatabaseHandler.GetFirstAvailableBluId();
            }

            // Add BIB to active_bib
            DatabaseHandler.AddNewActiveBib(bibId);

            // Mark BLU as unavailable
            DatabaseHandler.SetBluToUnavailable(bluId);

            // Get BLU info - TODO combine with GetFirstAvailableBluId call above (?)
            var bluInfo = DatabaseHandler.GetBlu(bluId);

            // Create JSON string to send back
            var json = JsonConvert.SerializeObject(bluInfo);

            // Publish BLU info
            mqttClient.Publish(PUB_TOPIC, Encoding.UTF8.GetBytes(json));

            AppendCurrentDatabaseData();
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            var thread = new Thread(ConnectToDatabase);
            thread.Start();
        }

        private void ConnectToDatabase() {
            AppendText("Connecting to database . . .", true);

            killMakeDotsThread = false;
            var makeDotsThread = new Thread(MakeDots);
            makeDotsThread.Start();

            try {
                DatabaseHandler.ConnectToDatabase();
                DatabaseHandler.ResetDatabase();

                killMakeDotsThread = true;
                AppendLine(" success!", true);

                AppendCurrentDatabaseData();
            }

            catch (Exception exception) {
                killMakeDotsThread = true;
                AppendLine(" failed.", true);
                AppendLine("Exception message:\n " + exception, true);
            }
        }

        private void AppendCurrentDatabaseData() {
            var data = DatabaseHandler.GetAllBlus();
            AppendDatabaseDataToTextBox("\nAll BLUs:", data);

            data = DatabaseHandler.GetAllActiveBibs();
            AppendDatabaseDataToTextBox("Active BIBs: ", data);

            data = DatabaseHandler.GetAllHistoricBibs();
            AppendDatabaseDataToTextBox("Historic BIBs: ", data);
        }

        private void AppendDatabaseDataToTextBox(string header, List<Dictionary<string, string>> data) {
            AppendLine(header, true);
            AppendDatabaseDataToTextBox(data);
        }

        private void AppendDatabaseDataToTextBox(List<Dictionary<string, string>> data) {
            if (data.Count == 0) {
                AppendLine("    (empty)", true);
                return;
            }
            foreach (var row in data) {
                var outputStr = "";
                foreach (var col in row) {
                    outputStr += "    " + col.Key + ":" + col.Value + ", ";
                }
                outputStr = outputStr.Substring(0, outputStr.Length - 2);
                AppendLine(outputStr, true);
            }
        }

        private void MakeDots() {
            while (true) {
                Thread.Sleep(1000);
                if (killMakeDotsThread) {
                    break;
                }
                AppendText(" .", true);
            }
        }

        private void AppendText(string text, bool useDispatcher) {
            if (useDispatcher) {
                Dispatcher.Invoke(() => AppendText(text));
            } else {
                AppendText(text);
            }
        }

        private void AppendLine(string text, bool useDispatcher) {
            if (useDispatcher) {
                Dispatcher.Invoke(() => AppendLine(text));
            } else {
                AppendLine(text);
            }
        }

        private void AppendText(string text) {
            textBlock.Text += text;
            scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        private void AppendLine(string text) {
            textBlock.Text += text + "\n";
            scrollViewer.ScrollToVerticalOffset(double.MaxValue);
        }

        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            mqttClient.Disconnect();
            DatabaseHandler.CloseConnection();
            Application.Current.Shutdown();
        }
    }
}
