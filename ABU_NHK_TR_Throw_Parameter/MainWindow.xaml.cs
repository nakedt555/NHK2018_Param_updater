using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace ABU_NHK_TR_Throw_Parameter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public System.IO.Ports.SerialPort serialPort;
        public bool serialInitializeFlag = false;
        public bool serialOpenFlag = false;

        public string[] TZ = { "TZ1", "TZ2", "TZ3" };
        public string[] ARM = { "LEFT", "RIGHT" };
        //public string SavePath = "C:\\Users\\greee\\Desktop\\Parameter\\" + DateTime.Now.ToString("MMdd");
        public string SavePath = AppDomain.CurrentDomain.BaseDirectory + "Parameter\\" + DateTime.Now.ToString("MMdd");

        public int av_start = 1;
        public int av_max = 20;
        public int av_finish = 0;
        public int av_accel_pos = 60;
        public int av_decel_pos = 60;

        public bool updateParameterFlag = true;

        public graph graphWindow;
        public bool graphDisplayFrag = false;
        private processRecivedData ProcessReciveData;

        public MainWindow()
        {
            InitializeComponent();

            graphWindow = new graph(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            graphWindow.MainWindowPointer = this;
            graphWindow.Show();

            setValue();

            foreach (string tz in TZ)
            {
                selectTZ.Items.Add(tz);
            }
            selectTZ.SelectedIndex = 0;

            foreach (string arm in ARM)
            {
                selectARM.Items.Add(arm);
            }
            selectARM.SelectedIndex = 0;

            connectSerialInterfase();

            //今日の日付ディレクトリを作成
            DirectoryUtils.SafeCreateDirectory(SavePath);

            ProcessReciveData = new processRecivedData(this);

        }

        private void connectNewBtn_Click(object sender, RoutedEventArgs e)
        {
            connectSerialInterfase();
        }

        void connectSerialInterfase()
        {
            if (serialInitializeFlag == false)
            {
                serialInitializeFlag = true;
            }
            else
            {
                setDebugPrint("[ INFO ] COM PORT ( " + serialPort.PortName + " ) を切断します．");
                serialOpenFlag = false;
                serialPort.Close();
            }
            var selectSerialWindow = new selectSerialPort();
            selectSerialWindow.MainWindowPointer = this;
            selectSerialWindow.Show();

            //取り敢えずCOM18に設定
            serialPort = new System.IO.Ports.SerialPort("COM18", 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            serialPort.NewLine = Environment.NewLine;
            serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceivedHandler);

            controlInitBtn.IsEnabled = false;
            controlUpdateBtn.IsEnabled = false;
            controlNextBtn.IsEnabled = false;
        }

        public bool openSerialPort(string portName, int baudRate)
        {
            if (portName != System.String.Empty)
            {
                serialPort.PortName = portName;
                serialPort.BaudRate = baudRate;
                try
                {
                    serialPort.Open();

                    if (serialPort.IsOpen)
                    {
                        //textBoxDebug.Text = serialPort.PortName + "をOpenしました．";
                        setDebugPrint("[ INFO ] COM PORT ( " + serialPort.PortName + " ) を開きました．");
                        controlInitBtn.IsEnabled = true;
                        controlUpdateBtn.IsEnabled = true;
                        controlNextBtn.IsEnabled = true;
                        serialOpenFlag = true;

                        return true;
                    }

                    return true;

                }
                catch
                {
                    setDebugPrint("[ ERROR ] COM PORT ( " + serialPort.PortName + " ) を開けませんでした．");
                    return false;
                }
            }
            else
            {
                textBoxDebug.Text = "[ ERROR ] Port nameを設定してください．";
                setDebugPrint("[ ERROR ] Wrong port name");
                return true;
            }
        }

        private void serialPort_DataReceivedHandler(object sender,
            System.IO.Ports.SerialDataReceivedEventArgs e)
        {

            try
            {
                textBoxDebug.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        string str = serialPort.ReadExisting();
                        //setDebugPrint(str);

                        foreach (byte c in str)
                        {
                            //setDebugPrint(c.ToString()); //debug
                            ProcessReciveData.inputData(c);
                        }
                    })
                );//beginInbokeだと呼び出し直後に元のコントロールに制御が戻る
            }
            catch
            {
                textBoxDebug.Dispatcher.Invoke(
                    new Action(() =>
                    {
                        setDebugPrint("[ ERROR ] COM PORT ( " + serialPort.PortName + " ) と通信できませんでした．");
                    })
                );
            }
        }

        void sendInitializeFrame()
        {
            if (serialOpenFlag == true)
            {
                byte[] sendData = { 0x50, 0xAA, 0x01 };
                //RightArmの場合
                if (ARM[selectARM.SelectedIndex] == "RIGHT")
                {
                    sendData[1] = 0xAB;
                }

                serialPort.Write(sendData, 0, 3);

                setDebugPrint("[ INFO ] アームを初期化します...");
            }
            else
            {
                setDebugPrint("[ ERROR ] COM PORTが接続されていません");
            }
        }

        void sendUpdateFrame()
        {
            if (serialOpenFlag == true)
            {
                byte[] sendData = { 0x50, 0xAA, 0x02, (byte)av_start, (byte)av_max, (byte)av_finish, (byte)av_accel_pos, (byte)av_decel_pos };
                //RightArmの場合
                if (ARM[selectARM.SelectedIndex] == "RIGHT")
                {
                    sendData[1] = 0xAB;
                }
                serialPort.Write(sendData, 0, 8);

                setDebugPrint("[ INFO ] アームの投擲パラメータを更新します...");

                controlUpdateBtn.IsEnabled = false;
                updateParameterFlag = false;
            }
            else
            {
                setDebugPrint("[ ERROR ] COM PORTが接続されていません");
            }
        }

        void sendNextActFrame()
        {
            if (serialOpenFlag == true)
            {
                if(updateParameterFlag == true)
                {
                    sendUpdateFrame();
                    updateParameterFlag = false;
                }

                byte[] sendData = { 0x50, 0xAA, 0x03 };
                //RightArmの場合
                if (ARM[selectARM.SelectedIndex] == "RIGHT")
                {
                    sendData[1] = 0xAB;
                }
                serialPort.Write(sendData, 0, 3);

                setDebugPrint("[ INFO ] アームを動作させます...");
            }
            else
            {
                setDebugPrint("[ ERROR ] COM PORTが接続されていません");
            }
        }

        void sendGetAngleFrame()
        {
            if (serialOpenFlag == true)
            {
                byte[] sendData = { 0x50, 0xAA, 0x04 };

                serialPort.Write(sendData, 0, 3);

                setDebugPrint("[ INFO ] 機体角度を取得します...");
            }
            else
            {
                setDebugPrint("[ ERROR ] COM PORTが接続されていません");
            }
        }

        private void controlInitBtn_Click(object sender, RoutedEventArgs e)
        {
            sendInitializeFrame();
        }

        private void controlUpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            sendUpdateFrame();
        }

        private void controlNextBtn_Click(object sender, RoutedEventArgs e)
        {
            sendNextActFrame();
        }

        private void getAngleBtn_Click(object sender, RoutedEventArgs e)
        {
            //float data_f = (float)0.0001234567;
            //byte[] data_b = BitConverter.GetBytes(data_f);
            //decimal data_f_2 = (decimal)BitConverter.ToSingle(data_b, 0);
            //foreach(byte b in data_b)
            //{
            //    setDebugPrint(b.ToString("x2"));
            //}
            //data_f_2 = Decimal.Round(data_f_2, 6);
            //setDebugPrint(data_f_2.ToString());
            sendGetAngleFrame();
        }

        void sendParameter2STM()
        {
            byte[] sendData = { (byte)av_start, (byte)av_max, (byte)av_finish, (byte)av_accel_pos, (byte)av_decel_pos };
            serialPort.Write(sendData, 0, 5);
        }


        public void setValue()
        {

            av_start_value_box.Text = av_start.ToString();
            av_start_slider.Value = av_start;
            av_max_value_box.Text = av_max.ToString();
            av_max_slider.Value = av_max;
            av_finish_value_box.Text = av_finish.ToString();
            av_finish_slider.Value = av_finish;
            av_accel_value_box.Text = av_accel_pos.ToString();
            av_accel_slider.Value = av_accel_pos;
            av_decel_value_box.Text = av_decel_pos.ToString();
            av_decel_slider.Value = av_decel_pos;
        }

        void changedParameter()
        {
            if(serialOpenFlag == true) {
                updateParameterFlag = true;
                controlUpdateBtn.IsEnabled = true;
            }
        }

        private void av_start_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            av_start = (int)Math.Round(av_start_slider.Value);
            if (av_start > av_max)
            {
                av_start = av_max;
                av_start_slider.Value = av_start;
            }
            av_start_value_box.Text = av_start.ToString();

            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            changedParameter();
        }

        private void av_max_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            av_max = (int)Math.Round(av_max_slider.Value);
            if (av_max < av_start)
            {
                av_max = av_start;
                av_max_slider.Value = av_max;
            }
            if (av_max < av_finish)
            {
                av_max = av_finish;
                av_max_slider.Value = av_max;
            }
            av_max_value_box.Text = av_max.ToString();

            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            changedParameter();
        }

        private void av_finish_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            av_finish = (int)Math.Round(av_finish_slider.Value);
            if (av_finish > av_max)
            {
                av_finish = av_max;
                av_finish_slider.Value = av_finish;
            }
            av_finish_value_box.Text = av_finish.ToString();

            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            changedParameter();
        }

        private void av_accel_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            av_accel_pos = (int)Math.Round(av_accel_slider.Value);
            if (av_accel_pos > av_decel_pos)
            {
                av_accel_pos = av_decel_pos;
                av_accel_slider.Value = av_accel_pos;
            }
            av_accel_value_box.Text = av_accel_pos.ToString();

            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            changedParameter();
        }

        private void av_decel_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            av_decel_pos = (int)Math.Round(av_decel_slider.Value);
            if (av_decel_pos < av_accel_pos)
            {
                av_decel_pos = av_accel_pos;
                av_decel_slider.Value = av_decel_pos;
            }
            av_decel_value_box.Text = av_decel_pos.ToString();

            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            changedParameter();
        }

        public void setDebugPrint(string text)
        {
            this.textBoxDebug.Text = text + "\n" + this.textBoxDebug.Text;
        }

        private void openBtn_Click(object sender, RoutedEventArgs e)
        {
            openFileSequence();
        }

        void openFileSequence()
        {
            var dialog = new OpenFileDialog
            {
                Title = "ファイルを開く",
                Filter = "csvファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*",
                //InitialDirectory = SavePath
                InitialDirectory = SavePath
            };

            if (dialog.ShowDialog() == (DialogResult)1)
            {
                var filename = dialog.FileName;
                openParameterFile(filename);
                setDebugPrint("[ INFO ] Path\"" + filename + "\"を開きました");
            }
            else
            {
                setDebugPrint("[ INFO ] FileOpenがキャンセルされました");
            }
        }

        void openParameterFile(string filename)
        {
            try
            {
                // csvファイルを開く
                using (var sr = new System.IO.StreamReader(filename))
                {
                    // ストリームの末尾まで繰り返す
                    while (!sr.EndOfStream)
                    {
                        // ファイルから一行読み込む
                        var line = sr.ReadLine();
                        // 読み込んだ一行をカンマ毎に分けて配列に格納する
                        var values = line.Split(',');
                        // 値を設定
                        av_start = int.Parse(values[0]);
                        av_max = int.Parse(values[1]);
                        av_finish = int.Parse(values[2]);
                        av_accel_pos = int.Parse(values[3]);
                        av_decel_pos = int.Parse(values[4]);
                        commentBox.Text = values[5];
                        selectARM.SelectedIndex = int.Parse(values[6]);
                        selectTZ.SelectedIndex = int.Parse(values[7]);
                        setValue();
                        graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
                    }
                }
            }
            catch (System.Exception e)
            {
                // ファイルを開くのに失敗したとき
                setDebugPrint(e.Message);
            }
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            saveFileSequence();
        }

        void saveFileSequence()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "ファイルを保存";
            dialog.Filter = "csvファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*";
            dialog.InitialDirectory = SavePath;
            dialog.FileName = DateTime.Now.ToString("HHmmss") + "_" + ARM[selectARM.SelectedIndex] + "_" + TZ[selectTZ.SelectedIndex] + ".csv";

            if (dialog.ShowDialog() == (DialogResult)1)
            {
                var filename = dialog.FileName;
                saveCurrentParameter(filename);
                setDebugPrint("[ INFO ] Path\"" + filename + "\"で保存しました");
            }
            else
            {
                setDebugPrint("[ INFO ] FileSaveがキャンセルされました");
            }
        }

        void saveCurrentParameter(string filename)
        {
            StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8);
            writer.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7}", av_start, av_max, av_finish, av_accel_pos, av_decel_pos, commentBox.Text, selectARM.SelectedIndex, selectTZ.SelectedIndex);
            writer.Close();
        }

        private void clearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            textBoxDebug.Clear();
        }

        private void displayGraphBtn_Click(object sender, RoutedEventArgs e)
        {
            graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
        }

        private void av_start_value_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                av_start = int.Parse(av_start_value_box.Text);
                setValue();
                graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            }
            catch
            {
                av_start_value_box.Text = "0";
            }

            changedParameter();

        }

        private void av_max_value_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                av_max = int.Parse(av_max_value_box.Text);
                setValue();
                graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            }
            catch
            {
                av_max_value_box.Text = "0";
            }

            changedParameter();
        }

        private void av_finish_value_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                av_finish = int.Parse(av_finish_value_box.Text);
                setValue();
                graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            }
            catch
            {
                av_finish_value_box.Text = "0";
            }

            changedParameter();
        }

        private void av_accel_value_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                av_accel_pos = int.Parse(av_accel_value_box.Text);
                setValue();
                graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            }
            catch
            {
                av_accel_value_box.Text = "0";
            }

            changedParameter();
        }

        private void av_decel_value_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                av_decel_pos = int.Parse(av_decel_value_box.Text);
                setValue();
                graphWindow.setArmParameter(av_start, av_max, av_finish, av_accel_pos, av_decel_pos);
            }
            catch
            {
                av_decel_value_box.Text = "0";
            }

            changedParameter();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                saveFileSequence();
            }
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                openFileSequence();
            }
            if (e.Key == Key.Escape)
            {
                sendInitializeFrame();
            }
            if (e.Key == Key.Enter)
            {
                sendNextActFrame();
            }
        }
    }

    /// <summary>
    /// Directory クラスに関する汎用関数を管理するクラス
    /// </summary>
    public static class DirectoryUtils
    {
        /// <summary>
        /// 指定したパスにディレクトリが存在しない場合
        /// すべてのディレクトリとサブディレクトリを作成します
        /// </summary>
        public static DirectoryInfo SafeCreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return null;
            }
            return Directory.CreateDirectory(path);
        }
    }

    class processRecivedData

    {
        private bool setStart = false;
        private int counter = 0;
        private byte[] dataBuffer = new byte[12];
        private MainWindow MainWindowPointer;

        public processRecivedData(MainWindow mainWindowPointer)
        {
            MainWindowPointer = mainWindowPointer;
        }

        public void inputData(byte data)
        {
            if(setStart == false && data == 0x50)
            {
                setStart = true;
                counter = 0;
            }
            else
            {
                dataBuffer[counter++] = data;

                if(counter > 1)
                {
                    //MainWindowPointer.setDebugPrint(counter.ToString()); //debubg
                    switch (dataBuffer[1])
                    {
                        case 0x10:
                            debugPrint("初期化しました．");
                            setStart = false;
                            break;
                        case 0x11:
                            debugPrint("パラメータを更新しました．");
                            setStart = false;
                            break;
                        case 0x12:
                            debugPrint("受け取り準備完了");
                            setStart = false;
                            break;
                        case 0x13:
                            debugPrint("シャトル掌握");
                            setStart = false;
                            break;
                        case 0x14:
                            debugPrint("投擲準備完了");
                            setStart = false;
                            break;
                        case 0x15:
                            debugPrint("投擲完了");
                            setStart = false;
                            break;

                        case 0x20:
                            setStart = true;

                            if (counter == 10)
                            {
                                decimal sideAngle, frontAngle;
                                sideAngle = (decimal)BitConverter.ToSingle(dataBuffer, 2);
                                frontAngle = (decimal)BitConverter.ToSingle(dataBuffer, 6);

                                sideAngle = Decimal.Round(sideAngle, 6);
                                frontAngle = Decimal.Round(frontAngle, 6);

                                MainWindowPointer.setDebugPrint("[ MAIN ] 機体角度取得完了");
                                MainWindowPointer.setDebugPrint("Side  Angle -> " + sideAngle.ToString());
                                MainWindowPointer.setDebugPrint("Front Angle -> " + frontAngle.ToString());

                                setStart = false;
                            }
                            break;
                    }
                }

            }


        }

        private void debugPrint(string text)
        {
            if(dataBuffer[0] == 0)
            {
                MainWindowPointer.setDebugPrint("[ MAIN ] (左アーム動作) " + text);
            }
            else
            {
                MainWindowPointer.setDebugPrint("[ MAIN ] (右アーム動作) " + text);
            }
        }
    }

}
