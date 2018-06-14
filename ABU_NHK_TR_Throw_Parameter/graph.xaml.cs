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

using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;

namespace ABU_NHK_TR_Throw_Parameter
{
    /// <summary>
    /// graph.xaml の相互作用ロジック
    /// </summary>
    public partial class graph : Window
    {
        ObservableValue[] x = new ObservableValue[314];
        ObservableValue[] y = new ObservableValue[314];

        public MainWindow MainWindowPointer;
        public graph(int av_start, int av_max, int av_finish, int av_accel_pos, int av_decel_pos)
        {
            InitializeComponent();

            setArmParameterFirst( av_start, av_max, av_finish, av_accel_pos, av_decel_pos);

            Values = new ChartValues<ObservableValue>(y);

            //Lets define a custom mapper, to set fill and stroke
            //according to chart values...
            Mapper = Mappers.Xy<ObservableValue>()
                .X((item, index) => index)
                .Y(item => item.Value);

            Labels = new string[314];
            for(int i = 0; i < 314; i++)
            {
                Labels[i] = (i / 100.0).ToString();
            }

            DataContext = this;
        }

        public ChartValues<ObservableValue> Values { get; set; }
        public CartesianMapper<ObservableValue> Mapper { get; set; }
        public string[] Labels { get; set; }

        public void setArmParameterFirst(int av_start, int av_max, int av_finish, int av_accel_pos, int av_decel_pos)
        {
            for (int i = 0; i < 314; i++)
            {
                double angle = i / 100.0;

                if (angle < deg2rad(av_accel_pos))
                {
                    y[i] = new ObservableValue(((av_max - av_start) * 0.5) * (1 - Math.Cos((angle / deg2rad(av_accel_pos)) * Math.PI)) + av_start);
                    var tmp = Math.Cos(((Math.PI - angle) / (Math.PI - deg2rad(av_accel_pos))) * Math.PI);
                }
                else if (angle < deg2rad(av_decel_pos))
                {
                    y[i] = new ObservableValue(av_max);
                }
                else
                {
                    y[i] = new ObservableValue(((av_max - av_finish) * 0.5) * (1 + (Math.Cos(((angle - deg2rad(av_decel_pos)) / (Math.PI - deg2rad(av_decel_pos)) * Math.PI)))) + av_finish);
                }
                x[i] = new ObservableValue(angle);
            }
        }

        public void setArmParameter(int av_start, int av_max, int av_finish, int av_accel_pos, int av_decel_pos)
        {
            for (int i = 0; i < 314; i++)
            {
                double angle = i / 100.0;

                if (angle < deg2rad(av_accel_pos))
                {
                    y[i].Value =((av_max - av_start) * 0.5) * (1 - Math.Cos((angle / deg2rad(av_accel_pos)) * Math.PI)) + av_start;
                }
                else if (angle < deg2rad(av_decel_pos))
                {
                    y[i].Value = av_max;
                }
                else
                {
                    y[i].Value = ((av_max - av_finish) * 0.5) * (1 + (Math.Cos(((angle - deg2rad(av_decel_pos)) / (Math.PI - deg2rad(av_decel_pos)) * Math.PI)))) + av_finish;
                }
                x[i].Value = angle;
            }
        }

        public void drawArmParameter(int av_start, int av_max, int av_finish, int av_accel_pos, int av_decel_pos)
        {
            var r = new Random();
            foreach (var observable in Values)
            {
                observable.Value = r.Next(10, 400);
            }

        }

        private double deg2rad(double deg)
        {
            return ((deg * Math.PI) / 180.0);
        }
    }
}
