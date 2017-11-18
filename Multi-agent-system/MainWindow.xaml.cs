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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using GoogleChartSharp;

namespace Multi_agent_system
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public struct Storage
    {
        public int product_id;
        public int count;
        public int day_delivery;
    }
    public partial class MainWindow : Window
    {
        //MySqlConnection db = new MySqlConnection("Database=multi_agent;Data Source=127.0.0.1;User Id=root;Password=");
        MySqlConnection db = new MySqlConnection("Database=multi_agent_system;Data Source=31.131.20.33;User Id=multiagentsys;Password=A9CZlYMgLm");
        public MainWindow()
        {
            InitializeComponent();
            //load_statistics_zakaz_for_product(678);
            //load_statistics_zakaz_for_product(1);
            db.Open();
            int product_id = 1;

            ///////////GETERATION INFORMATION
            //generate_data();
            //generate_prognoz();
            Dictionary<DateTime, int> stat_prodazh = load_statistics_zakaz_for_product(product_id);
            Dictionary<DateTime, int> prognoz = load_prognoz_for_product(product_id);
            int[] data = new int[] { 0, 50, 20, 10, 40 };

            int[] l = new int[stat_prodazh.Values.Count];
            int[] p = new int[stat_prodazh.Values.Count];
            int[] r = new int[stat_prodazh.Values.Count];
            int i = 0;
            DateTime save_key = new DateTime(2016, 1, 1);
            string[] days = new string[7];
            foreach (var item in stat_prodazh.Keys)
            {
                if(item.Day == 1)
                days[i/60] = item.ToString("yyyy.MM.dd");
                l[i] = stat_prodazh[item] * 30;
                if (prognoz.ContainsKey(item))
                {
                    p[i] = (100 + prognoz[item] * 10) * 10;
                    if (p[i] < 0)
                        p[i] = 0;
                    save_key = item;
                }
                else if (prognoz.ContainsKey(save_key))
                {
                    p[i] = (100 + prognoz[save_key] * 10) * 10;
                    if (p[i] < 0)
                        p[i] = 0;

                }
                i++;
            }
            for (int j = 0; j < l.Length; j++)
            {
                r[j] = l[j] + l[j] * ((p[j] / 10 - 100) / 10) / 10;
                if (r[j] < 0)
                    r[j] = 0;
            }

            List<int[]> data_print = new List<int[]>();
            data_print.Add(l);
            // data_print.Add(p);
            data_print.Add(r);
            LineChart chart = new LineChart(700, 300);
            chart.SetData(data_print);
            string[] colors = new string[] { "004411", "D41C1C" };
            chart.SetTitle("Попит на товар");
            string[] legends = new string[] { "Дані за минулий рік", "Прогнозовані дані" };

            chart.SetLegend(legends);
            chart.SetDatasetColors(colors);
            //chart.SetDatasetColors("#004411");

            chart.SetGrid(10, 7);
            ChartAxis ca = new ChartAxis(ChartAxisType.Bottom, days);
            chart.AddAxis(ca);
            
            
            string url = chart.GetUrl();


            BitmapImage myBitmapImage = new BitmapImage();

            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = new Uri(url);
            myBitmapImage.EndInit();
            image.Source = myBitmapImage;


            int[] aproximate_l = new int[13];
            int[] aproximate_r = new int[13];

            for (int j = 0; j < r.Length; j++)
            {
                aproximate_r[j / 30] += r[j];
                aproximate_l[j / 30] += l[j];
            }
            for (int j = 0; j < aproximate_r.Length; j++)
            {
                aproximate_r[j] /= 30;
                aproximate_l[j] /= 30;
            }

            LineChart chart_aprox = new LineChart(700, 300);
            List<int[]> data2 = new List<int[]>();
            data2.Add(aproximate_l);
            data2.Add(aproximate_r);
            chart_aprox.SetData(data2);
            chart_aprox.SetTitle("Попит на товар");
            chart_aprox.SetLegend(legends);

            chart_aprox.SetDatasetColors(colors);

            chart_aprox.AddAxis(ca);

            chart_aprox.SetGrid(10, 7);
            //ChartAxis ca = new ChartAxis(ChartAxisType.Left);


            url = chart_aprox.GetUrl();


            myBitmapImage = new BitmapImage();

            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = new Uri(url);
            myBitmapImage.EndInit();
            image2.Source = myBitmapImage;



            //////////////////////////SKLAD

            Storage storage = LoadStorage(product_id);

            label_product_id.Content = storage.product_id;
            label_date_now.Content = DateTime.Now.ToString("yyyy.MM.dd");
            label_storage_count.Content = storage.count;
            DateTime date_next_delivery = DateNextDelivery(storage);
            label_date_nex_delivery.Content = date_next_delivery.ToString("yyyy.MM.dd");
            int count_day_to_next_delivery = (date_next_delivery - DateTime.Now).Days;
            label_day_to_next_delivery.Content = count_day_to_next_delivery;
            int nead_product_to_text_delivery = NeadProductToNextDelivery(count_day_to_next_delivery, r);
            label_nead_products.Content = nead_product_to_text_delivery;
            int count_excess_product = nead_product_to_text_delivery;
            label_count_excess_product.Content = storage.count - nead_product_to_text_delivery;
            int nead_shop_product = NeadProductToNextDelivery((date_next_delivery.AddMonths(1) - DateTime.Now).Days, r);
            label_nead_shop_product.Content = nead_shop_product- storage.count;






            db.Clone();


        }
        public int NeadProductToNextDelivery(int days, int[] data)
        {
            DateTime nullDate = DateTime.Now.AddMonths(1 - DateTime.Now.Month).AddDays(1 - DateTime.Now.Day);
            int d = DateTime.Compare(DateTime.Now, nullDate);
            int count = 0;
            for (int i = 0; i < d + days; i++)
            {
                count += data[i+ d] / 30;
            }
            return count;
        }
        public DateTime DateNextDelivery(Storage storage)
        {
            DateTime today = DateTime.Now;
            DateTime nextDay = DateTime.Now;

            if (today.Day > storage.day_delivery)
                nextDay = DateTime.Now.AddDays(today.Day - storage.day_delivery);
            else
            {
                nextDay.AddMonths(1);
                nextDay.AddDays(today.Day - storage.day_delivery);
                //nextDay. = storage.day_delivery;
            }

            return nextDay;
        }
        public Storage LoadStorage(int product_id)
        {
            string sql = String.Format("SELECT product_id, count,day_delivery FROM `storage`" +
                   "WHERE  product_id = {0} LIMIT 1 "
                   , product_id);
            MySqlCommand mc = new MySqlCommand(sql, db);
            MySqlDataReader MyDataReader = mc.ExecuteReader();
            Storage storage = new Storage();
            while (MyDataReader.Read())
            {
                storage.product_id = MyDataReader.GetInt32(0);
                storage.count = MyDataReader.GetInt32(1);
                storage.day_delivery = MyDataReader.GetInt32(2);
            }
            //list_data
            MyDataReader.Close();
            return storage;

        }
        public Dictionary<DateTime, int> load_statistics_zakaz_for_product(int product_id)
        {
            string sql = String.Format("SELECT date, SUM(count) as count " +
                "FROM `statistics_zakaz` " +
                "WHERE `date` >= '2016-01-01' AND product_id = {0} " +
                "GROUP BY date " +
                "ORDER BY date ASC", product_id);
            MySqlCommand mc = new MySqlCommand(sql, db);


            //List<statistic> list_data = new List<statistic>();
            Dictionary<DateTime, int> list_data = new Dictionary<DateTime, int>();
            MySqlDataReader MyDataReader = mc.ExecuteReader();
            while (MyDataReader.Read())
            {
                //statistic item = new statistic();
                list_data.Add(MyDataReader.GetDateTime(0).Date, MyDataReader.GetInt32(1));
                /*item.id = MyDataReader.GetInt32(1);
                item.date = MyDataReader.GetDateTime(0).Date;
                list_data.Add(item);*/
            }
            //list_data
            MyDataReader.Close();
            return list_data;
        }
        public Dictionary<DateTime, int> load_prognoz_for_product(int product_id)
        {
            string sql = String.Format("SELECT date, count " +
                "FROM `prognoz` " +
                "WHERE `date` >= '2016-01-01' AND product_id = {0} " +
                "GROUP BY date " +
                "ORDER BY date ASC", product_id);
            MySqlCommand mc = new MySqlCommand(sql, db);


            //List<statistic> list_data = new List<statistic>();
            Dictionary<DateTime, int> list_data = new Dictionary<DateTime, int>();
            MySqlDataReader MyDataReader = mc.ExecuteReader();
            while (MyDataReader.Read())
            {
                //statistic item = new statistic();
                list_data.Add(MyDataReader.GetDateTime(0).Date, MyDataReader.GetInt32(1));
                /*item.id = MyDataReader.GetInt32(1);
                item.date = MyDataReader.GetDateTime(0).Date;
                list_data.Add(item);*/
            }
            MyDataReader.Close();

            //list_data
            return list_data;
        }
        public void generate_data()
        {
            DateTime start_data = new DateTime(2016, 1, 1);
            db.Open();

            for (int i = 0; i < 365; i++)
            {
                string sql = String.Format(@"INSERT INTO `multi_agent_system`.`statistics_zakaz` 
(`id`, `product_id`, `date`, `count`) 
VALUES (NULL, '1', '{0}', '{1}')", start_data.AddDays(i).ToString("yyyy-MM-dd"), new Random().Next(50));
                MySqlCommand mc = new MySqlCommand(sql, db);
                mc.ExecuteNonQuery();
            }
            db.Clone();
        }
        public void generate_prognoz()
        {
            DateTime start_data = new DateTime(2016, 1, 1);
            db.Open();

            for (int i = 0; i < 12; i++)
            {
                Random r = new Random();
                string sql = String.Format(@"INSERT INTO `prognoz` 
(`id`, `product_id`, `date`, `count`) 
VALUES (NULL, '1', '{0}', '{1}')", start_data.AddMonths(i).ToString("yyyy-MM-dd"), r.Next(5) - r.Next(5));
                MySqlCommand mc = new MySqlCommand(sql, db);
                mc.ExecuteNonQuery();
            }
            db.Clone();
        }
    }
}
