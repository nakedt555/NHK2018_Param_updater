using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ABU_NHK_TR_Throw_Parameter
{
    /// <summary>
    /// selectSerialPort.xaml の相互作用ロジック
    /// </summary>
    public partial class selectSerialPort : Window
    {
        public MainWindow MainWindowPointer;
        int[] baudRate = { 9600, 115200};

        public selectSerialPort()
        {
            InitializeComponent();
            
            setSerialPortName();
            setBaudRate();
        }

        private void SerialStartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowPointer.openSerialPort(getPortName(), baudRate[SerialBaudRate.SelectedIndex]))
            {
                this.Close();
            }
        }

        public void setSerialPortName()
        {
            var CheckComNum = new System.Text.RegularExpressions.Regex("COM[1-9][0-9]?[0-9]?");

            System.Management.ManagementClass mcPnPEntity = new System.Management.ManagementClass("Win32_PnPEntity");
            System.Management.ManagementObjectCollection manageObjCol = mcPnPEntity.GetInstances();

            foreach (System.Management.ManagementObject manageObj in manageObjCol)
            {
                var namePropertyValue = manageObj.GetPropertyValue("Name");
                if (namePropertyValue == null)
                {
                    continue;
                }
                string name = namePropertyValue.ToString();

                if (CheckComNum.IsMatch(name))
                {
                    SerialComPort.Items.Add(name);
                }
            }
            SerialComPort.SelectedIndex = 0;
        }

        private string getPortName()
        {
            var ExtractPortNum = new System.Text.RegularExpressions.Regex(".*(COM[1-9][0-9]?[0-9]?).*");
            if (SerialComPort.SelectedItem == null)
            {
                //textBoxTextArea.Text += "No Port Selected\n";
                return System.String.Empty;
            }
            string name = (string)SerialComPort.SelectedItem;
            string portName = ExtractPortNum.Replace(name, "$1");
            return portName;
        }

        public void setBaudRate()
        {
            foreach(int i in baudRate)
            {
                SerialBaudRate.Items.Add(i.ToString());
            }
            SerialBaudRate.SelectedIndex = 1;
        }

        private void SerialUpdadteBtn_Click(object sender, RoutedEventArgs e)
        {
            SerialComPort.Items.Clear();
            setSerialPortName();
        }

        private void SerialCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
