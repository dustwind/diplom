using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using LinqToVisualTree;

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        bdBusEntities be = new bdBusEntities();
        int sel = 0;
        int select_type_day=0;
        int srez = 0;

        List<way_repos> w_repos = new List<way_repos>();
 
        Window1 window_report = new Window1();

        bool remove_message;
        int select
        {
            get
            {
                return sel;
            }
            set //установка в текстбокс действия
            {
                string s="";
                switch (value)
                {
                    case 0: s = "Ничего не выбранно"; break;
                    case 1: s = "Работа с локациями"; break;
                    case 2: s = "Создание локаций"; break;
                    case 3: s = "Удаление локаций"; break;
                    case 4: s = "Отобразить все точки"; break;
                    case 5: s = "Создать точки"; break;
                    case 6: s = "Удалить точки"; break;
                    case 7: s = "Формирование маршрута"; break;
                    case 8: s = "Выбор остановок"; break;
                    case 9: s = "Прогноз прибытия"; break;
                    case 666: s = "Поиск пути по локациям Муравьиный алгоритм"; break;
                    case 667: s = "Выбор точек для маршрута"; break;
                    case 668: s = "Поиск пути Муравьиный алгоритм: Расстояние"; break;
                    case 669: s = "Поиск пути Муравьиный алгоритм: Время"; break;
                    case 777: s = "Поиск пути алгоритм Дейкстры: Расстояние"; break;
                    case 778: s = "Поиск пути алгоритм Дейкстры: Время"; break;
                    case 779: s = "Поиск пути алгоритм А*: Расстояние"; break;
                    case 780: s = "Поиск пути алгоритм А*: Время"; break;
                    case 200: s = "Поиск пути в глубину: Расстояние"; break;
                    case 300: s = "Поиск пути в ширину: Расстояние"; break;
                }
                txtSelect.Text = s;
                sel = value;
            }
        }

        //что это за херня?
        static System.Net.WebClient wb = new System.Net.WebClient();
        
        List<bz_day> bbz_day=new List<bz_day>();
        List<bz_week> bbz_week = new List<bz_week>();
        List<bz_year> bbz_year = new List<bz_year>();
        List<sLocation> bslocation = new List<sLocation>();

        public MainWindow()
        {
            InitializeComponent();
            map1.LayoutUpdated += (sender, args) =>
            {
                if (!remove_message)
                {
                    RemoveOverlayTextBlock();
                }
            };
        bslocation = be.sLocation.ToList();
        }

        private void RemoveOverlayTextBlock()
        {
            var textBlock = map1.DescendantsAndSelf()
                           .OfType<TextBlock>()
                           .SingleOrDefault(d => d.Text.Contains("Invalid Credentials") ||
                                                 d.Text.Contains("Unable to contact Server"));

            if (textBlock != null)
            {
                var parentBorder = textBlock.Parent as Border;
                if (parentBorder != null)
                {
                    parentBorder.Visibility = Visibility.Collapsed;
                }

                remove_message = true;
            }
        }
        private void Report_open(object sender, RoutedEventArgs e)
        {
            window_report.Owner = this;
            window_report.Show();
            this.Activate();
        }

        #region Карта
        //щелчок по карте
        private void map1_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (select == 5 && e.RightButton == System.Windows.Input.MouseButtonState.Pressed) //создание точек
            {
                Point p = e.GetPosition(this.map1);
                Location l = map1.ViewportPointToLocation(p);
                sKoord_point sp = new sKoord_point();
                sp.Latitude = l.Latitude;
                sp.Longitude = l.Longitude;
                be.sKoord_point.Add(sp);
                be.SaveChanges();

                Pushpin tempP = new Pushpin { Location = l, Background = new SolidColorBrush(Colors.Black), Tag = sp, Content = sp.id };
                tempP.MouseDown += tempP_MouseDoubleClick;
                //tempP.MouseDoubleClick += tempP_MouseDoubleClick;
                map1.Children.Add(tempP);
            }
        }
        //очистка карты
        private void Button_clear(object sender, RoutedEventArgs e)
        {
            clear_map();
        }

        private void clear_map()
        {
            select = 0;
            map1.Children.Clear();
            //очищаем Pushpin
            p1 = null;
            p2 = null;
            foreach (object ob in map1.Children)
            {
                Pushpin pp = (ob is Pushpin) ? (Pushpin)ob : null;
                if (pp != null)
                    pp.Background = new SolidColorBrush(Colors.Black);
            }
        }

        //смена типа карты
        private void Button_Type_map(object sender, RoutedEventArgs e)
        {
            if (map1.Mode is Microsoft.Maps.MapControl.WPF.RoadMode)
                map1.Mode = new Microsoft.Maps.MapControl.WPF.AerialMode();
            else
                map1.Mode = new Microsoft.Maps.MapControl.WPF.RoadMode();
        }

        private void map1_Loaded(object sender, RoutedEventArgs e)
        {
            List<sMarshruts> l = be.sMarshruts.ToList();
            cbMarsh.ItemsSource = l;
            Button_clear(null, null);
            //Button_speed_update(null, null);
        }

        //отрисовка выбранных путей из хранилища
        public void draw_way(List <way_repos> wr)
        {
            clear_map();
            for (int i = 0; i < wr.Count; i++)
            {
                draw_way_on_map(wr[i]);
            }
        }

        //отрисовка пока без временного среза
        private void draw_way_on_map(way_repos wr)
        {
            p1 = new Pushpin();
            p1.Content = "A";
            p1.Location = new Location();
            p1.Location.Latitude = (double)wr.a.Latitude;
            p1.Location.Longitude = (double)wr.a.Longitude;
            p1.Background = new SolidColorBrush(Colors.Red);
            map1.Children.Add(p1);

            p2 = new Pushpin();
            p2.Content = "B";
            p2.Location = new Location();
            p2.Location.Latitude = (double)wr.b.Latitude;
            p2.Location.Longitude = (double)wr.b.Longitude;
            p2.Background = new SolidColorBrush(Colors.Red);
            map1.Children.Add(p2);

            for (int i = 0; i < wr.way.Count; i++)
            {
                map1.Children.Add(PrintWayOnMap(wr.way[i], wr.name_algoritm));
            }

        }

        private MapPolyline PrintWayOnMap(MyLocation lom, string algoritm)
        {
            double a = My.azimuth(lom.PointA.Longitude, lom.PointA.Latitude, lom.PointB.Longitude, lom.PointB.Latitude) / 180.0 * Math.PI;
            double s = 0.00001;

            var mpp = new MapPolyline();
            mpp.Locations = new LocationCollection();
            var l1 = new Microsoft.Maps.MapControl.WPF.Location { Latitude = lom.PointA.Latitude, Longitude = lom.PointA.Longitude };
            var l2 = new Microsoft.Maps.MapControl.WPF.Location { Latitude = lom.PointB.Latitude, Longitude = lom.PointB.Longitude };

            Location l3 = new Location();
            l3.Longitude = (s * 3) * Math.Cos(a) - (-s * 4) * Math.Sin(a) + lom.PointB.Longitude;
            l3.Latitude = (s * 3) * Math.Sin(a) + (-s * 4) * Math.Cos(a) + lom.PointB.Latitude;

            Location l4 = new Location();
            l4.Longitude = (-s * 3) * Math.Cos(a) - (-s * 4) * Math.Sin(a) + lom.PointB.Longitude;
            l4.Latitude = (-s * 3) * Math.Sin(a) + (-s * 4) * Math.Cos(a) + lom.PointB.Latitude;

            Location l5 = l2;

            mpp.Locations.Add(l1);
            mpp.Locations.Add(l2);
            mpp.Locations.Add(l3);
            mpp.Locations.Add(l4);
            mpp.Locations.Add(l5);


            mpp.StrokeEndLineCap = PenLineCap.Triangle;
            mpp.Stroke = lom.Color;
            mpp.StrokeThickness = 3;
            mpp.ToolTip = algoritm;
            mpp.MouseLeftButtonDown += mpp_MouseLeftButtonDown;
            mpp.Tag = algoritm;
            mpp.Uid = algoritm.ToString();
            return mpp;
        }
        #endregion

        #region Маршрут

        private void Button_Find_TC(object sender, RoutedEventArgs e)
        {
            map1.Children.Clear();
            if (cbMarsh.SelectedItem != null)
            {
                //Загрузить все локации
                sMarshruts sm = ((sMarshruts)cbMarsh.SelectedItem);
                List<MyLocation> lml = new List<MyLocation>();
                //foreach (sLocation sl in be.sLocation.Where(o => o.sMarhList == be.sMarhList.Where(k => k.Id_Marsh == sm.id)))
                foreach (sMarhList sml in be.sMarhList.Where(o=>o.Id_Marsh==sm.id))
                {
                    sLocation sl = sml.sLocation;
                    MyLocation temp = new MyLocation();
                    temp.nom = sl.Id;
                    temp.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                    temp.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                    temp.way = (sl.length.HasValue) ? sl.length.Value : 0;
                    temp.Color = Brushes.Yellow;
                    lml.Add(temp);
                }

                //загрузить все автобусы
                List<TC> ltc = new List<TC>();
                {
                    DateTime dt = DateTime.Now.AddMinutes(-5);
                    List<aBus> lab = be.aBus.Where(o => o.DateUpdate > dt).ToList();
                    List<sBuses> lsb = be.sBuses.Where(o => o.id_marsh == sm.id).ToList();
                    foreach (sBuses sb in lsb)
                    {
                        aBus ab = lab.Where(o => o.Id_bus == sb.nom).FirstOrDefault();
                        if (ab != null)
                        {
                            TC temp = new TC();
                            temp.Date = ab.DateUpdate;
                            temp.ID_Marh = sm.id;
                            temp.Latitude = ab.Latitude;
                            temp.Longitude = ab.Longitude;
                            temp.num = ab.Id_bus;
                            temp.Ugol = ab.Azimuth.Value;
                            ltc.Add(temp);
                        }
                    }
                }

                foreach (TC tc in ltc)
                {
                    //проверить принадлежность автобусов к локации
                    tc.MyLocation = My.GiveLocation(tc.ID_Marh, tc.Ugol, tc.Longitude, tc.Latitude);
                    if (tc.MyLocation != null)
                    {
                        tc.MyPercent = My.GivePersent(tc.MyLocation.nom, tc.Longitude, tc.Latitude);
                        //выбрать эти локации и пометить красным цветом
                        MyLocation ml = lml.Where(o => o.nom == tc.MyLocation.nom).FirstOrDefault();
                        ml.Color = Brushes.Red;
                    }
                }

                //показать все локации и ТС на карте
                foreach (MyLocation temp in lml)
                    map1.Children.Add(PrintLocationOnMap(temp));
                foreach (TC tc in ltc)
                {
                    Pushpin tempP = new Pushpin
                    {
                        Location = new Location(tc.Latitude, tc.Longitude),
                        Background = new SolidColorBrush(Colors.Black),
                        //Tag = tc.num,
                        Content = sm.name_marshrut
                    };
                    if (tc.MyPercent != 0) tempP.Tag = (tc.MyPercent * 100).ToString("##");
                    map1.Children.Add(tempP);
                }
            }
        }

        private MapPolyline PrintLocationOnMap(MyLocation lom)
        {
            double a = My.azimuth(lom.PointA.Longitude, lom.PointA.Latitude, lom.PointB.Longitude, lom.PointB.Latitude) / 180.0 * Math.PI;
            double s = 0.00001;

            var mpp = new MapPolyline();
            mpp.Locations = new LocationCollection();
            var l1 = new Microsoft.Maps.MapControl.WPF.Location { Latitude = lom.PointA.Latitude, Longitude = lom.PointA.Longitude };
            var l2 = new Microsoft.Maps.MapControl.WPF.Location { Latitude = lom.PointB.Latitude, Longitude = lom.PointB.Longitude };

            Location l3 = new Location();
            l3.Longitude = (s * 3) * Math.Cos(a) - (-s * 4) * Math.Sin(a) + lom.PointB.Longitude;
            l3.Latitude = (s * 3) * Math.Sin(a) + (-s * 4) * Math.Cos(a) + lom.PointB.Latitude;

            Location l4 = new Location();
            l4.Longitude = (-s * 3) * Math.Cos(a) - (-s * 4) * Math.Sin(a) + lom.PointB.Longitude;
            l4.Latitude = (-s * 3) * Math.Sin(a) + (-s * 4) * Math.Cos(a) + lom.PointB.Latitude;

            Location l5 = l2;

            mpp.Locations.Add(l1);            
            mpp.Locations.Add(l2);
            mpp.Locations.Add(l3);
            mpp.Locations.Add(l4);
            mpp.Locations.Add(l5);


            mpp.StrokeEndLineCap = PenLineCap.Triangle;
            mpp.Stroke = lom.Color;
            mpp.StrokeThickness = 3;
            mpp.ToolTip = lom.nom;
            mpp.MouseLeftButtonDown += mpp_MouseLeftButtonDown;
            mpp.Tag = lom;
            mpp.Uid = lom.nom.ToString();
            return mpp;
        }

        private void Button_CreateMarsh(object sender, RoutedEventArgs e)
        {
            select = 7;
        }
        #endregion

        #region Локации
        Pushpin p1;
        Pushpin p2;

        int b_loc1;
        int b_loc2;

        //все локации
        private void Button_Click_Enable_Location(object sender, RoutedEventArgs e)
        {
            //select = 1;
            List<MyLocation> ml = new List<MyLocation>();
            //заполняем ml всеми данными из бд
            foreach (sLocation sl in be.sLocation)
            {
                MyLocation temp = new MyLocation();
                temp.nom = sl.Id;
                temp.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                temp.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                temp.way = (sl.length.HasValue) ? sl.length.Value : 0;
                temp.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#616161"));
                ml.Add(temp);
            }

            if (cbMarsh.SelectedItem != null)
            {
                sMarshruts sel_marh = (sMarshruts)cbMarsh.SelectedItem;
                //List<sMarhList> l_marshList = be.sMarhList.Where(o => o.Id_Marsh == sel_marh.id).ToList();
                List<sMarhList> l_marshList = sel_marh.sMarhList.ToList();
                foreach (sMarhList temp in l_marshList)
                {
                    MyLocation sl = ml.Where(o => o.nom == temp.sLocation.Id).FirstOrDefault();
                    if (sl != null)
                        sl.Color = Brushes.Red;
                }
            }
            //перебираем ml и рисуем на карте
            foreach (MyLocation temp in ml)
                map1.Children.Add(PrintLocationOnMap(temp));
        }
        private void Button_Click_Enable_Location2(object sender, RoutedEventArgs e)
        {
            //select = 1;
            List<MyLocation> ml = new List<MyLocation>();
            //заполняем ml всеми данными из бд
            foreach (Big_Location sl in be.Big_Location)
            {
                MyLocation temp = new MyLocation();
                temp.nom = sl.Id_big;
                temp.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                temp.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                temp.way = sl.length;
                ml.Add(temp);
            }

            if (cbMarsh.SelectedItem != null)
            {
                sMarshruts sel_marh = (sMarshruts)cbMarsh.SelectedItem;
                //List<sMarhList> l_marshList = be.sMarhList.Where(o => o.Id_Marsh == sel_marh.id).ToList();
                List<sMarhList> l_marshList = sel_marh.sMarhList.ToList();
                foreach (sMarhList temp in l_marshList)
                {
                    MyLocation sl = ml.Where(o => o.nom == temp.sLocation.Id).FirstOrDefault();
                    if (sl != null)
                        sl.Color = Brushes.Red;
                }
            }
            //перебираем ml и рисуем на карте
            foreach (MyLocation temp in ml)
                map1.Children.Add(PrintLocationOnMap(temp));
        }

        //при добавлении локации добавлять её длину и азимут
        private void Button_create_location(object sender, RoutedEventArgs e)
        {
            select = 2;
            if (p1 != null && p2 != null)
            {
                sLocation sl = new sLocation();
                sl.Id_point1 = ((sKoord_point)(p1.Tag)).id;
                sl.Id_point2 = ((sKoord_point)(p2.Tag)).id;
                be.sLocation.Add(sl);
                be.SaveChanges();
                sl.length = My.distance(sl.sKoord_point.Longitude.Value, sl.sKoord_point.Latitude.Value, sl.sKoord_point1.Longitude.Value, sl.sKoord_point1.Latitude.Value);
                sl.azimuth = My.azimuth(sl.sKoord_point.Longitude.Value, sl.sKoord_point.Latitude.Value, sl.sKoord_point1.Longitude.Value, sl.sKoord_point1.Latitude.Value);
                be.SaveChanges();

                foreach (object ob in map1.Children)
                {
                    Pushpin pp = (ob is Pushpin) ? (Pushpin)ob : null;
                    if (pp != null)
                        pp.Background = new SolidColorBrush(Colors.Black);
                }
                MyLocation temp = new MyLocation();
                temp.nom = sl.Id;
                temp.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                temp.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                temp.way = (sl.length.HasValue) ? sl.length.Value : 0;
                map1.Children.Add(PrintLocationOnMap(temp));
                p1 = null;
                p2 = null;
            }
        }
        private void Button_delete_location(object sender, RoutedEventArgs e)
        {
            select = 3;
        }

        //щелчок по локации
        void mpp_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (select == 7) //формирование маршрута
            {
                MapPolyline m = (sender is MapPolyline) ? (MapPolyline)sender : null;//выбранная локация на карте
                if (m != null)
                {
                    if (cbMarsh.SelectedItem != null)
                    {
                        sMarshruts sel_marh = (sMarshruts)cbMarsh.SelectedItem;//выбранный маршрут
                        MyLocation selLoc = (MyLocation)m.Tag;//конкретная инфа по локации
                        if (selLoc.Color == Brushes.Yellow)//значит не принадлежала
                        {
                            sMarhList newMarsh = new sMarhList();
                            newMarsh.Id_Location = selLoc.nom;
                            newMarsh.Id_Marsh = sel_marh.id;
                            be.sMarhList.Add(newMarsh);

                            selLoc.Color = Brushes.Red;
                        }
                        else
                        {
                            sMarhList dsad = be.sMarhList.Where(o => o.Id_Location == selLoc.nom && o.Id_Marsh == sel_marh.id).FirstOrDefault();
                            be.sMarhList.Remove(dsad);

                            selLoc.Color = Brushes.Yellow;
                        }
                        //нужно найти, определить цвет
                        //добавить или удалить запись принадлежности
                        //и поменять цвет
                        m.Stroke = selLoc.Color;
                        be.SaveChanges();
                        //Button_Click_Enable_Location(null, null);
                    }
                }
            }
            if (select == 3) //удаление локаций
            {
                MapPolyline m = (sender is MapPolyline) ? (MapPolyline)sender : null;//выбранная локация на карте
                if (m != null)
                {
                    if (MessageBox.Show("Вы действительно хотите удалить эту локацию?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        MyLocation selLoc = (MyLocation)m.Tag;//конкретная инфа по локации
                        var del = be.sLocation.Where(o => o.Id == selLoc.nom).FirstOrDefault();
                        be.sLocation.Remove(del);
                        be.SaveChanges();

                        map1.Children.Remove(m);
                    }
                }
            }
            if (select == 666) //муравьи
            {
                MapPolyline m = (sender is MapPolyline) ? (MapPolyline)sender : null;//выбранная локация на карте
                if (m != null)
                {
                    MyLocation selLoc = (MyLocation)m.Tag;//конкретная инфа по локации
                    string sss = selLoc.Color.ToString();

                    if (b_loc1 == 0 && b_loc2 == 0)
                    {
                        b_loc1 = selLoc.nom;
                    }
                    else if (b_loc1 != 0 && b_loc2 == 0)
                    {
                        b_loc2 = selLoc.nom;
                        map1.Children.Clear();

                        Short_way_ant_by_location(b_loc1, b_loc2);
                    }
                    else
                    {
                        b_loc1 = selLoc.nom;
                        b_loc2 = 0;
                    }
                }
            }
            if (select == 667)//выбор точек для поиска по точкам
            {
                MapPolyline m = (sender is MapPolyline) ? (MapPolyline)sender : null;//выбранная локация на карте
                if (m != null)
                {
                    MyLocation selLoc = (MyLocation)m.Tag;//конкретная инфа по локации
                    string sss = selLoc.Color.ToString();

                    if (p1 == null)
                    {
                        p1 = new Pushpin();
                        p1.Content = "A";
                        p1.Location = new Location();
                        p1.Location.Latitude = selLoc.PointA.Latitude;
                        p1.Location.Longitude = selLoc.PointA.Longitude;

                        foreach (sKoord_point kp in be.sKoord_point)
                        {
                            if (kp.Latitude == p1.Location.Latitude && kp.Longitude == p1.Location.Longitude)
                            {
                                p1.Tag = kp;
                            }
                        }

                        p1.Background = new SolidColorBrush(Colors.Red);
                        map1.Children.Add(p1);
                    }
                    else if (p2 == null)
                    {
                        p2 = new Pushpin();
                        p2.Content = "B";
                        p2.Location = new Location();
                        p2.Location.Latitude = selLoc.PointB.Latitude;
                        p2.Location.Longitude = selLoc.PointB.Longitude;

                        foreach (sKoord_point kp in be.sKoord_point)
                        {
                            if (kp.Latitude == p2.Location.Latitude && kp.Longitude == p2.Location.Longitude)
                            {
                                p2.Tag = kp;
                            }
                        }

                        p2.Background = new SolidColorBrush(Colors.Red);
                        map1.Children.Add(p2);
                    }
                }
            }
        }
        
        //создание больших локаций
        private void Button_create_big_location(object sender, RoutedEventArgs e)
        {
            //foreach (var l in be.sLocation)
            //    l.id_big = null;
            //foreach (var l in be.Big_Location)
             //   be.Big_Location.Remove(l);
            //be.SaveChanges();

            //List<sLocation> bml = new List<sLocation>();
            List<Big_Location> bml2 = new List<Big_Location>();
            List<sLocation> b_temp = new List<sLocation>();
            var MyLoc = be.sLocation.ToList();
            var sssss = be.Big_Location.ToList();
            sLocation tempLoc = null;
            int countLoc = MyLoc.Count;
            pbMarsh.Value = 0;
            pbMarsh.Maximum = countLoc;
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);
            do
            {
                pbMarsh.Value = countLoc - MyLoc.Count;
                Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value });

                if (tempLoc == null)
                {
                    tempLoc = MyLoc[0];
                    bool k = true;
                    int kk = 0;
                    do
                    {
                        var lll = MyLoc.Where(o => o.Id_point2 == tempLoc.Id_point1).ToList();
                        var lll2 = MyLoc.Where(o => o.Id_point2 == tempLoc.Id_point1).ToList();
                        //var lll3 = Convert.ToInt32(MyLoc.Where(o => o.length == tempLoc.length).ToList());//?????

                        if (lll.Count == 1 && lll2.Count == 1)
                        {
                            tempLoc = lll[0];
                            kk++;
                        }
                        else
                            k = false;
                    } while (k && kk < 20);
                }

                b_temp.Add(tempLoc);
                MyLoc.Remove(tempLoc);
                var tempLoc2 = be.sLocation.Where(o => o.Id_point1 == tempLoc.Id_point2).ToList();
                var temploc3 = be.sLocation.Where(o => o.Id_point2 == tempLoc.Id_point2).ToList();
                if (tempLoc2.Count == 1 && temploc3.Count == 1)
                {
                    tempLoc = tempLoc2[0];
                }
                else
                {
                    //создат большую локацию
                    Big_Location sl2 = new Big_Location();
                    //sl.sKoord_point = b_temp.FirstOrDefault().sKoord_point;
                    //sl.sKoord_point1 = b_temp.LastOrDefault().sKoord_point1;
                    //bml.Add(sl);
                    sl2.id_koord1 = b_temp.FirstOrDefault().sKoord_point.id;
                    sl2.id_koord2 = b_temp.LastOrDefault().sKoord_point1.id;

                    sl2.length = b_temp.Sum(o => o.length).Value; //?????
                    bml2.Add(sl2);
                    //сохранить в бд в таблицу больших локаций
                    be.Big_Location.Add(sl2);
                    be.SaveChanges();
                    //сбросить все временные лококации в бд
                    //записать id для всех локаций btemp
                    foreach (var l in b_temp)
                    {
                        if (!l.id_big.HasValue)
                            l.id_big = sl2.Id_big;
                    }
                    be.SaveChanges();
                    b_temp = new List<sLocation>();
                    tempLoc = null;
                }
            } while (MyLoc.Count > 0);

            var sssfs = be.Big_Location.ToList();

            foreach (var bl in bml2)
            {
                bl.sLocation = sort_big_local(bl);
                MyLocation ml = new MyLocation();
                ml.PointA = new MyPoint() { Latitude = bl.sKoord_point.Latitude.Value, Longitude = bl.sKoord_point.Longitude.Value };
                ml.PointB = new MyPoint() { Latitude = bl.sKoord_point1.Latitude.Value, Longitude = bl.sKoord_point1.Longitude.Value };
                map1.Children.Add(PrintLocationOnMap(ml));
            }
        }

        //сортировка малых локаций в большой по пордку
        private List<sLocation> sort_big_local(Big_Location b)
        {
            List<sLocation> sloc = new List<sLocation>();
            sLocation s = b.sLocation.Where(o => o.Id_point1 == b.id_koord1).FirstOrDefault();
            sloc.Insert(0, s);

            for (int i = 1; i < b.sLocation.Count; i++)
            {
                s = b.sLocation.Where(o => o.Id_point1 == sloc.ElementAt(i - 1).Id_point2).FirstOrDefault();
                sloc.Insert(i, s);
            }

            return sloc;
        }
        #endregion

        #region Точки
        private void Button_all_point(object sender, RoutedEventArgs e)
        {
            select = 4;
            p1 = null;
            p2 = null;
            foreach (sKoord_point kp in be.sKoord_point)
            {
                //if (kp.sOstnovkis.Count != 0)
                //{
                    Location lc = new Location();
                    lc.Latitude = kp.Latitude.Value;
                    lc.Longitude = kp.Longitude.Value;
                    Pushpin tempP = new Pushpin { Location = lc, Background = new SolidColorBrush(Colors.Black), Tag = kp, Content = kp.id };
                    tempP.MouseDown += tempP_MouseDoubleClick;
                    //tempP.MouseDoubleClick += tempP_MouseDoubleClick;
                    map1.Children.Add(tempP);
                //}
            }
        }
        private void Button_create_point(object sender, RoutedEventArgs e)
        {
            select = 5;
        }
        private void Button_delete_point(object sender, RoutedEventArgs e)
        {
            select = 6;
        }
        
        //щелчок по точке
        void tempP_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (select == 2)//значит работаем с созданием локаций
            {
                foreach (object ob in map1.Children)
                {
                    Pushpin pp = (ob is Pushpin) ? (Pushpin)ob : null;
                    if (pp != null)
                        pp.Background = new SolidColorBrush(Colors.Black);
                }
                //((Pushpin)sender).Background = new SolidColorBrush(Colors.Red);

                if (p1 == null)
                    p1 = (Pushpin)sender;
                else if (p2 == null)
                    p2 = (Pushpin)sender;
                else
                    p1 = (Pushpin)sender;

                if (p1 != null) p1.Background = new SolidColorBrush(Colors.Red);
                if (p2 != null) p2.Background = new SolidColorBrush(Colors.Red);

                Button_create_location(null, null);
            }
            if (select == 6) //удаление точек
            {
                Pushpin pp = (sender is Pushpin) ? (Pushpin)sender : null;
                if (pp != null)
                {
                    sKoord_point selPoint = be.sKoord_point.Where(o => o.id == ((sKoord_point)(pp.Tag)).id).FirstOrDefault();
                    if (selPoint != null)
                    {
                        if (MessageBox.Show("Вы действительно хотите удалить эту точку?", "Внимание", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            be.sKoord_point.Remove(selPoint);
                            be.SaveChanges();
                            map1.Children.Remove(pp);
                        }
                    }
                }
            }

            if (select == 8) //выбор остановок
            {
                foreach (object ob in map1.Children)
                {
                    Pushpin pp = (ob is Pushpin) ? (Pushpin)ob : null;
                    if (pp != null)
                        pp.Background = new SolidColorBrush(Colors.Black);
                }

                if (p1 == null)
                    p1 = (Pushpin)sender;
                else if (p2 == null)
                    p2 = (Pushpin)sender;
                else
                    p1 = (Pushpin)sender;

                if (p1 != null) p1.Background = new SolidColorBrush(Colors.Red);
                if (p2 != null) p2.Background = new SolidColorBrush(Colors.Red);
            }

            if (select == 667)//выбор точек для поиска по точкам
            {
                foreach (object ob in map1.Children)
                {
                    Pushpin pp = (ob is Pushpin) ? (Pushpin)ob : null;
                    if (pp != null)
                        pp.Background = new SolidColorBrush(Colors.Black);
                }

                if (p1 == null)
                    p1 = (Pushpin)sender;
                else if (p2 == null)
                    p2 = (Pushpin)sender;
                else
                    p1 = (Pushpin)sender;

                if (p1 != null) p1.Background = new SolidColorBrush(Colors.Red);
                if (p2 != null) p2.Background = new SolidColorBrush(Colors.Red);

            }
        }

        //Выбрать точки для поиска по точкам
        private void Button_select_points(object sender, RoutedEventArgs e)
        {
            select = 667;
            p1 = null;
            p2 = null;
        }
        #endregion

        #region База знаний
        bool flag_bz = true;
        
        
        private void Button_speed_update(object sender, RoutedEventArgs e)
        {

            flag_bz = false;
            List<TimePoint> li = new List<TimePoint>();
            switch (srez)
            {
                case 0:
                    {
                        List<int> bz_time = new List<int>();
                        bz_time.AddRange(be.bz_day.Where(o => o.Days == select_type_day).Select(o => o.Times).Distinct().ToList());
                        bz_time.AddRange(be.bz_week.Where(o => o.Days == select_type_day).Select(o => o.Times).Distinct().ToList());
                        bz_time.AddRange(be.bz_year.Where(o => o.Days == select_type_day).Select(o => o.Times).Distinct().ToList());
                        bz_time = bz_time.Distinct().ToList();
                        foreach (var bz_time_one in bz_time)
                        {
                            li.Add(new TimePoint() { timeInt = bz_time_one });
                        }
                        break;
                    }
                case 1:
                    {
                        var bz_time = be.bz_day.Where(o=>o.Days==select_type_day).Select(o => o.Times).Distinct();
                        foreach (var bz_time_one in bz_time)
                        {
                            li.Add(new TimePoint() { timeInt = bz_time_one });
                        }
                        break;
                    }
                case 2:
                    {
                        var bz_time = be.bz_week.Where(o => o.Days == select_type_day).Select(o => o.Times).Distinct();
                        foreach (var bz_time_one in bz_time)
                        {
                            li.Add(new TimePoint() { timeInt = bz_time_one });
                        }
                        break;
                    }
                case 3:
                    {
                        var bz_time = be.bz_year.Where(o => o.Days == select_type_day).Select(o => o.Times).Distinct();
                        foreach (var bz_time_one in bz_time)
                        {
                            li.Add(new TimePoint() { timeInt = bz_time_one });
                        }
                        break;
                    }
            }
            
            li.Sort(delegate(TimePoint tl1, TimePoint tl2)
            { return tl1.timeInt.CompareTo(tl2.timeInt); });
            cbTime.ItemsSource = li;
            flag_bz = true;
        }
        
        
        private void Button_koeff(object sender, RoutedEventArgs e)
        {
            var lkoef = be.bz_koeff.ToList();
            foreach (bz_koeff koef in lkoef)
            {
                sLocation sl = be.sLocation.Where(o => o.Id == koef.Location).FirstOrDefault();
                MyLocation ml = new MyLocation();
                ml.nom = sl.Id;
                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                MapPolyline mpl = PrintLocationOnMap(ml);
                int sp = Convert.ToInt32(koef.Koeff * 100);
                mpl.ToolTip = sp.ToString() + "%";

                if (sp > 800) mpl.Stroke = Brushes.Red;
                if (sp <= 800 && sp > 400) mpl.Stroke = Brushes.Orange;
                if (sp <= 400 && sp > 200) mpl.Stroke = Brushes.Yellow;
                if (sp <= 200 && sp >= 50) mpl.Stroke = Brushes.Green;
                if (sp < 50 && sp >= 25) mpl.Stroke = Brushes.Yellow;
                if (sp < 25 && sp >= 12) mpl.Stroke = Brushes.Orange;
                if (sp < 12) mpl.Stroke = Brushes.Red;
                
                map1.Children.Add(mpl);
            }
        }
        private void cbTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flag_bz) BZ_mourning();
                //BZ();

        }
        private void select_type_day_Checked(object sender, RoutedEventArgs e)
        {
            if (flag_bz)
            {
                select_type_day = Convert.ToInt16(((RadioButton)sender).Tag);
                Button_speed_update(null, null);
            }
            BZ_mourning();
            //BZ();
        }
        private void srez_Checked(object sender, RoutedEventArgs e)
        {
            srez = Convert.ToInt16(((RadioButton)sender).Tag);

            if (srez == 1)
            {
                flag_bz = false;
                int k = My.GetTypeDay();
                rbTypeDay0.IsChecked = k == 0;
                rbTypeDay1.IsChecked = k == 1;
                rbTypeDay2.IsChecked = k == 2;
                flag_bz = true;
            }

            Button_speed_update(null, null); 
            //BZ();
            BZ_mourning();
        }
        private void BZ()
        {
            map1.Children.Clear();
            TimePoint k = (TimePoint)cbTime.SelectedItem;
            if (k != null)
                switch (srez)
                {
                    case 0:
                        {
                            List<int> lsl = new List<int>();
                            foreach (bz_day bzd in bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            foreach (bz_week bzd in bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            foreach (bz_year bzd in bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            lsl = lsl.Distinct().ToList();
                            foreach (int ii in lsl)
                            //foreach (sLocation sl in be.sLocation)
                            {
                                double speed = 0;
                                bz_day bzd = bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();
                                bz_week bzw = bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();
                                bz_year bzy = bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();

                                double speed_day = (bzd != null) ? bzd.Val : 0;
                                double speed_week = (bzw != null) ? bzw.Val : 0;
                                double speed_year = (bzy != null) ? bzy.Val : 0;
                                speed = AverageVal(speed_day, speed_week, speed_year);
                                if (speed > 0)
                                {
                                    sLocation sl = bslocation.Where(o => o.Id == ii).FirstOrDefault();
                                    MyLocation ml = new MyLocation();
                                    ml.nom = sl.Id;
                                    ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                    ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                    ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                    MapPolyline mpl = PrintLocationOnMap(ml);
                                    int sp = (int)(ml.way * 3.6 / speed);
                                    mpl.ToolTip = sp.ToString() + " км/ч";
                                    if (sp > 40) mpl.Stroke = Brushes.Green;
                                    if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                    if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                    if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                    if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;
                                    //if (sp == 0) mpl.Stroke = Brushes.White;
                                    map1.Children.Add(mpl);
                                }
                            }
                            break;
                        }
                    case 1:
                        {
                            #region Дневныые данные
                            var bzd = bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();
                            foreach (bz_day bzd_one in bzd)
                            {
                                sLocation sl = bslocation.Where(o => o.Id == bzd_one.Location).FirstOrDefault();
                                MyLocation ml = new MyLocation();
                                ml.nom = sl.Id;
                                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                MapPolyline mpl = PrintLocationOnMap(ml);
                                int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                mpl.ToolTip = sp.ToString() + " км/ч";
                                if (sp > 40) mpl.Stroke = Brushes.Green;
                                if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                map1.Children.Add(mpl);
                            }
                            #endregion
                            break;
                        }
                    case 2:
                        {
                            #region Недельные данные
                            var bzd = bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();
                            foreach (bz_week bzd_one in bzd)
                            {
                                sLocation sl = bslocation.Where(o => o.Id == bzd_one.Location).FirstOrDefault();
                                MyLocation ml = new MyLocation();
                                ml.nom = sl.Id;
                                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                MapPolyline mpl = PrintLocationOnMap(ml);
                                int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                mpl.ToolTip = sp.ToString() + " км/ч";
                                if (sp > 40) mpl.Stroke = Brushes.Green;
                                if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                map1.Children.Add(mpl);
                            }
                            #endregion
                            break;
                        }
                    case 3:
                        {
                            #region Годовые данные
                            var bzd = bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();
                            foreach (bz_year bzd_one in bzd)
                            {
                                sLocation sl = bslocation.Where(o => o.Id == bzd_one.Location).FirstOrDefault();
                                MyLocation ml = new MyLocation();
                                ml.nom = sl.Id;
                                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                MapPolyline mpl = PrintLocationOnMap(ml);
                                int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                mpl.ToolTip = sp.ToString() + " км/ч";
                                if (sp > 40) mpl.Stroke = Brushes.Green;
                                if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                map1.Children.Add(mpl);
                            }
                            #endregion
                            break;
                        }
                }
        }
        static double AverageVal(double ValDay, double ValWeek, double ValYear)// Определение среднего
        {
            double s;
            double k3_1 = 0.6; double k3_2 = 0.25; double k3_3 = 0.15;
            double k2_1 = 0.7; double k2_2 = 0.3; double k1_1 = 1;

            if (!Double.IsNaN(ValDay) && ValDay != 0)
                if (!Double.IsNaN(ValWeek) && ValWeek != 0)
                    if (!Double.IsNaN(ValYear) && ValYear != 0)
                        s = k3_1 * ValDay + k3_2 * ValWeek + k3_3 * ValYear;
                    else
                        s = k2_1 * ValDay + k2_2 * ValWeek;
                else
                    if (!Double.IsNaN(ValYear) && ValYear != 0)
                        s = k2_1 * ValDay + k2_2 * ValYear;
                    else
                        s = k1_1 * ValDay;
            else
                if (!Double.IsNaN(ValWeek) && ValWeek != 0)
                    if (!Double.IsNaN(ValYear) && ValYear != 0)
                        s = k2_1 * ValWeek + k2_2 * ValYear;
                    else
                        s = k1_1 * ValWeek;
                else
                    if (!Double.IsNaN(ValYear) && ValYear != 0)
                        s = k1_1 * ValYear;
                    else
                        s = 0;
            return s;
        }
        
        //интерполируемые данные

        //6:10
        private void BZ_mourning()
        {
            map1.Children.Clear();
            TimePoint k = (TimePoint)cbTime.SelectedItem;
            if (k != null)
                switch (srez)
                {
                    case 0:
                        {
                            #region Дифф
                            List<int> lsl = new List<int>();
                            foreach (bz_day bzd in bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            foreach (bz_week bzd in bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            foreach (bz_year bzd in bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day))
                            {
                                lsl.Add(bzd.Location);
                            }
                            lsl = lsl.Distinct().ToList();
                            foreach (int ii in lsl)
                            //foreach (sLocation sl in be.sLocation)
                            {
                                double speed = 0;
                                bz_day bzd = bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();
                                bz_week bzw = bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();
                                bz_year bzy = bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day && o.Location == ii).FirstOrDefault();

                                double speed_day = (bzd != null) ? bzd.Val : 0;
                                double speed_week = (bzw != null) ? bzw.Val : 0;
                                double speed_year = (bzy != null) ? bzy.Val : 0;
                                speed = AverageVal(speed_day, speed_week, speed_year);
                                if (speed > 0)
                                {
                                    sLocation sl = bslocation.Where(o => o.Id == ii).FirstOrDefault();
                                    MyLocation ml = new MyLocation();
                                    ml.nom = sl.Id;
                                    ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                    ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                    ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                    MapPolyline mpl = PrintLocationOnMap(ml);
                                    int sp = (int)(ml.way * 3.6 / speed);
                                    mpl.ToolTip = sp.ToString() + " км/ч";
                                    if (sp > 40) mpl.Stroke = Brushes.Green;
                                    if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                    if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                    if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                    if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;
                                    //if (sp == 0) mpl.Stroke = Brushes.White;
                                    map1.Children.Add(mpl);
                                }
                            }
                            break;
                            #endregion
                        }
                    case 1:
                        {
                            #region Дневныые данные
                            var bzd = bbz_day.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();
                            foreach (bz_day bzd_one in bzd)
                            {
                                sLocation sl = bslocation.Where(o => o.Id == bzd_one.Location).FirstOrDefault();
                                MyLocation ml = new MyLocation();
                                ml.nom = sl.Id;
                                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                MapPolyline mpl = PrintLocationOnMap(ml);
                                int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                mpl.ToolTip = sp.ToString() + " км/ч";
                                if (sp > 40) mpl.Stroke = Brushes.Green;
                                if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                map1.Children.Add(mpl);
                            }
                            #endregion
                            break;
                        }
                    case 2:
                        {
                            #region Недельные данные
                            var bzd = bbz_week.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();
                            foreach (bz_week bzd_one in bzd)
                            {
                                sLocation sl = bslocation.Where(o => o.Id == bzd_one.Location).FirstOrDefault();
                                MyLocation ml = new MyLocation();
                                ml.nom = sl.Id;
                                ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                MapPolyline mpl = PrintLocationOnMap(ml);
                                int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                mpl.ToolTip = sp.ToString() + " км/ч";
                                if (sp > 40) mpl.Stroke = Brushes.Green;
                                if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                map1.Children.Add(mpl);
                            }

                            #endregion
                            break;
                        }
                    case 3:
                        {
                            #region Годовые данные
                            //bbz_day = be.bz_day.ToList();
                            //bbz_week = be.bz_week.ToList();
                            bbz_year = be.bz_year.ToList().Where(o=>o.Times==k.timeInt&&o.Days==select_type_day).ToList();
                            //var bzd = bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();

                            foreach (sLocation sl in bslocation)
                            {
                                bz_year bzd_one = bbz_year.Where(o => o.Location == sl.Id).FirstOrDefault();
                                //есть статистика
                                if (bzd_one != null)
                                {
                                    MyLocation ml = new MyLocation();
                                    ml.nom = sl.Id;
                                    ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                    ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                    ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                    MapPolyline mpl = PrintLocationOnMap(ml);
                                    int sp = (int)(ml.way * 3.6 / bzd_one.Val);
                                    mpl.ToolTip = sp.ToString() + " км/ч";
                                    if (sp > 40) mpl.Stroke = Brushes.Green;
                                    if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                    if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                    if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                    if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                    map1.Children.Add(mpl);
                                }

                                //нет статистики
                                else{
                                    List<sLocation> pre_loc = find_first(sl);//список предыдущих. последняя с данными о времени
                                    List<sLocation> past_loc = find_last(sl);// спосок последующих. последняя с данными о времени

                                    if (pre_loc.Last() != null && past_loc.Last() != null)
                                    {
                                        List<sLocation> order_loc = new List<sLocation>();

                                        for (int i = pre_loc.Count - 1; i > 0; i--) { order_loc.Add(pre_loc[i]); }
                                        for (int i = 0; i < past_loc.Count; i++) { order_loc.Add(past_loc[i]); }

                                        interpolation_value_speed(order_loc);

                                        //bzd.Clear();
                                        //bzd = bbz_year.Where(o => o.Times == k.timeInt && o.Days == select_type_day).ToList();

                                        MyLocation ml = new MyLocation();
                                        ml.nom = sl.Id;
                                        ml.PointA = new MyPoint() { id = sl.sKoord_point.id, Latitude = sl.sKoord_point.Latitude.Value, Longitude = sl.sKoord_point.Longitude.Value };
                                        ml.PointB = new MyPoint() { id = sl.sKoord_point1.id, Latitude = sl.sKoord_point1.Latitude.Value, Longitude = sl.sKoord_point1.Longitude.Value };
                                        ml.way = (sl.length.HasValue) ? sl.length.Value : 0;

                                        bz_year bzd_o = bbz_year.Where(o => o.Location == sl.Id).FirstOrDefault();

                                        MapPolyline mpl = PrintLocationOnMap(ml);
                                        int sp = (int)(ml.way * 3.6 / bzd_o.Val);
                                        mpl.ToolTip = sp.ToString() + " км/ч";
                                        if (sp > 40) mpl.Stroke = Brushes.Green;
                                        if (sp > 30 && sp <= 40) mpl.Stroke = Brushes.YellowGreen;
                                        if (sp > 20 && sp <= 30) mpl.Stroke = Brushes.Yellow;
                                        if (sp > 10 && sp <= 20) mpl.Stroke = Brushes.Orange;
                                        if (sp > 0 && sp <= 10) mpl.Stroke = Brushes.Red;

                                        map1.Children.Add(mpl);
                                    }
                                }
                            }

                            }
                            #endregion
                            break;
                        }
            }

        public void interpolation_value_speed(List<sLocation> sloc)
        {
            bz_year bzd_one = bbz_year.Where(o => o.Location == sloc.First().Id).FirstOrDefault();
            bz_year bzd_two = bbz_year.Where(o => o.Location == sloc.Last().Id).FirstOrDefault();

                List<bz_year> zz = new List<bz_year>();
                zz.Add(bzd_one);
                for (int i = 1; i < sloc.Count - 1; i++)
                {
                    bz_year temp = new bz_year();
                    temp.Location = sloc[i].Id;
                    temp.Times = bzd_one.Times;
                    temp.Days = select_type_day;
                    temp.Val = zz.Last().Val + (bzd_two.Val - zz.Last().Val) / ((sloc.Count-1) -(zz.Count-1)) * (i-zz.Count-1);
                    zz.Add(temp);
                    bbz_year.Add(temp);
                }
        }

        public List<sLocation> find_first(sLocation k)
        {
            bool f = false;
            List<sLocation> pre = new List<sLocation>();
            pre.Add(k);
            do
            {
                pre.Add(bslocation.Where(o => o.Id_point2 == pre.Last().Id_point1).FirstOrDefault());
                if (pre.Last() == null) break;
                bz_year bzd_one = bbz_year.Where(o => o.Location == pre.Last().Id).FirstOrDefault();
                if (bzd_one != null) { f = true; break; };
            } while (f != true);
            return pre;
        }

        public List<sLocation> find_last(sLocation k)
        {
            bool f = false;
            List<sLocation> past = new List<sLocation>();
            past.Add(k);
            do
            {
                past.Add(bslocation.Where(o => o.Id_point1 == past.Last().Id_point2).FirstOrDefault());
                if (past.Last() == null) break;
                bz_year bzd_one = bbz_year.Where(o => o.Location == past.Last().Id).FirstOrDefault();
                if (bzd_one != null) { f = true; break; };
            } while (f != true);
            return past;
        }
        #endregion

        #region Прогноз автобуса
        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);
        
        private void Button_bus_prognoz(object sender, RoutedEventArgs e)
        {
            select = 9;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));

                var spis = GetBusFromAtoB_points(pp1.id,pp2.id);
                lbBus.ItemsSource = spis;

                if (spis.Count > 0)
                {
                    //карту очистить, скинуть все локации в один массив и построить все их желтым цветом
                    //добавить в конце автобусы, точки. При выборе маршрута красить его локации в красный

                    map1.Children.Clear();
                    //очистить карту, отобразить автобусы запомнить координаты точек А и Б и отобразить их
                    List<MyTimeLocation> AllLocation = new List<MyTimeLocation>();
                    foreach (var temp in spis)//автобусы
                    {
                        AllLocation.AddRange(temp.Location);
                    }

                    foreach (var loc in AllLocation.GroupBy(o => o.IdLocation))
                    {
                        var ll = be.sLocation.Where(o => o.Id == loc.Key).FirstOrDefault();
                        MyLocation ml = new MyLocation();
                        ml.nom = ll.Id;
                        ml.PointA = new MyPoint() { id = ll.sKoord_point.id, Latitude = ll.sKoord_point.Latitude.Value, Longitude = ll.sKoord_point.Longitude.Value };
                        ml.PointB = new MyPoint() { id = ll.sKoord_point.id, Latitude = ll.sKoord_point1.Latitude.Value, Longitude = ll.sKoord_point1.Longitude.Value };
                        ml.Color = Brushes.Yellow;
                        map1.Children.Add(PrintLocationOnMap(ml));
                    }

                    foreach (var temp in spis)//автобусы
                    {
                        Pushpin tempP = new Pushpin
                        {
                            Location = new Location(temp.Latitude, temp.Longitude),
                            Background = new SolidColorBrush(Colors.Black),
                            Tag = temp.Marsh.id,
                            Content = temp.Marsh.name
                        };
                        map1.Children.Add(tempP);
                    }

                    Pushpin tempP1 = new Pushpin
                    {
                        Location = new Location() { Latitude = pp1.Latitude.Value, Longitude = pp1.Longitude.Value },
                        Background = new SolidColorBrush(Colors.Black),
                        Tag = pp1.id,
                        Content = "A"
                    };
                    Pushpin tempP2 = new Pushpin
                    {
                        Location = new Location() { Latitude = pp2.Latitude.Value, Longitude = pp2.Longitude.Value },
                        Background = new SolidColorBrush(Colors.Black),
                        Tag = pp2.id,
                        Content = "B"
                    };

                    map1.Children.Add(tempP1);
                    map1.Children.Add(tempP2);
                }
                else
                    MessageBox.Show("Вариантов не найдено");
            }
        }
       
        private void lbBus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //локации от автобуса до А и Б
            MyTimeBus loc = (MyTimeBus)((ListBox)sender).SelectedItem;
            if (loc != null)
            {
                foreach (var ob in map1.Children)
                {
                    if (ob is MapPolyline)
                    {
                        MapPolyline mpl = (MapPolyline)ob;
                        MyLocation ml = (MyLocation)mpl.Tag;
                        if (loc.Location.Where(o => o.IdLocation == ml.nom).FirstOrDefault() != null)
                        {
                            mpl.Stroke = Brushes.Red;
                        }
                        else
                        {
                            mpl.Stroke = Brushes.Yellow;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="lPoint_Start">ID точки начала</param>
        /// <param name="lPoint_End">ID точки окончания</param>
        /// <returns>Список автобусов с временем прибытия и списком локаций</returns>
        public List<MyTimeBus> GetBusFromAtoB_points(int lPoint_Start, int lPoint_End)
        {
            List<MyTimeBus> mtp = new List<MyTimeBus>();
            //mtp.LocMarh = new List<sLocation>();
            //затем определить все маршруты, которые включают эти точки (от А до Б)
            /* !!!!!*/
            pbMarsh.Value = 0;
            pbMarsh.Maximum = be.sMarshruts.Count();
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);

            foreach (var m1 in be.sMarshruts)//проходим по всем маршрутам
            {
                List<MyBusOnMarsh> BusOnMarsh = new List<MyBusOnMarsh>();
                //уже известные точки
                var p1 = lPoint_Start;
                var p2 = lPoint_End;
                //foreach (var p1 in lPoint_Start)//и всем сочитаниям точек
                //  foreach (var p2 in lPoint_End)
                {
                    sMarhList l1 = m1.sMarhList.Where(o => o.sLocation.Id_point1 == p1).FirstOrDefault();
                    sMarhList l2 = m1.sMarhList.Where(o => o.sLocation.Id_point2 == p2).FirstOrDefault();
                    if (l1 != null && l2 != null)//если эти точки присудствуют в маршруте
                    {
                        pbStep.Value = 0;
                        UpdateProgressBarDelegate updatepbStepDelegate = new UpdateProgressBarDelegate(pbStep.SetValue);

                        #region//загружаем список автобусов на маршруте
                        List<sBuses> sb = be.sBuses.Where(o => o.id_marsh == m1.id).ToList();
                        DateTime dtime = DateTime.Now.AddMinutes(-5);
                        List<aBus> temp = be.aBus.Where(o => (o.DateUpdate > dtime)).ToList();
                        pbBus.Value = 0;
                        pbBus.Maximum = temp.Count();
                        UpdateProgressBarDelegate updatepbBusDelegate = new UpdateProgressBarDelegate(pbBus.SetValue);
                        foreach (aBus ab in temp)
                        {
                            if (BusOnMarsh.Where(o => o.Latitude == ab.Latitude && o.Longitude == ab.Longitude).FirstOrDefault() == null)//проверяем, не было ли его до этого
                            {
                                if (sb.Where(o => o.nom == ab.Id_bus).FirstOrDefault() != null)
                                {
                                    MyBusOnMarsh mbom = new MyBusOnMarsh()//и записываем его в коллекцию
                                    {
                                        dtUpdate = ab.DateUpdate,
                                        Latitude = ab.Latitude,
                                        Longitude = ab.Longitude,
                                        azimuth = (ab.Azimuth.HasValue) ? ab.Azimuth.Value : -1,
                                        TimeLocation = new List<MyTimeLocation>(),
                                        temp1 = p1, //l1.Id,
                                        temp2 = p2 //l2.Id
                                    };
                                    BusOnMarsh.Add(mbom);
                                }

                            }
                            pbBus.Value++;
                            Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                        }
                        #endregion
                        pbStep.Value=1;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                        #region определяем местоположение
                        pbBus.Value = 0;
                        pbBus.Maximum = BusOnMarsh.Count();
                        updatepbBusDelegate = new UpdateProgressBarDelegate(pbBus.SetValue);
                        foreach (var bom in BusOnMarsh)
                        {
                            MyLocation temp_location = My.GiveLocation(m1.id, bom.azimuth, bom.Longitude, bom.Latitude);
                            if (temp_location != null)
                            {
                                bom.Id_Activ_Location = temp_location.nom;
                                bom.Pos_Activ_Location = My.GivePersent(bom.Id_Activ_Location, bom.Longitude, bom.Latitude);
                            }
                            pbBus.Value++;
                            Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                        }
                        #endregion
                        pbStep.Value=2;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                        var temp_m = be.sMarhList.Where(o => o.Id_Marsh == m1.id).Select(o => o.sLocation);//локации выбранного маршрута
                        #region //локации от Б до А
                        pbBus.Value = 0;
                        pbBus.Maximum = temp_m.Count();
                        updatepbBusDelegate = new UpdateProgressBarDelegate(pbBus.SetValue);
                        List<MyTimeLocation> LocationsBtoA = new List<MyTimeLocation>();
                        {
                            sLocation x = temp_m.Where(o => o.Id_point2 == p2).FirstOrDefault();
                            LocationsBtoA.Add(new MyTimeLocation() { IdLocation = x.Id });
                            bool flag = true;
                            while (flag)
                            {
                                x = temp_m.Where(o => o.Id_point2 == x.Id_point1).FirstOrDefault();
                                if (x != null)
                                {
                                    LocationsBtoA.Add(new MyTimeLocation() { IdLocation = x.Id });
                                    flag = (x.Id != p1);
                                }
                                else
                                    flag = false;
                                pbBus.Value++;
                                Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                            }
                        }
                        #endregion
                        pbStep.Value=3;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                        #region //Затем от А до первых 3х автобусов.
                        pbBus.Value = 0;
                        pbBus.Maximum = 100;
                        updatepbBusDelegate = new UpdateProgressBarDelegate(pbBus.SetValue);
                        List<MyBusOnMarsh> FirstBusOnMarsh = new List<MyBusOnMarsh>();
                        {
                            int count_loc = 0;
                            bool flag = true;
                            sLocation x = temp_m.Where(o => o.Id_point2 == p1).FirstOrDefault();//локация до точки A
                            FirstBusOnMarsh.Add(new MyBusOnMarsh() { TimeLocation = new List<MyTimeLocation>() });
                            FirstBusOnMarsh.Add(new MyBusOnMarsh() { TimeLocation = new List<MyTimeLocation>() });
                            FirstBusOnMarsh.Add(new MyBusOnMarsh() { TimeLocation = new List<MyTimeLocation>() });
                            int i = 0;
                            FirstBusOnMarsh[0].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                            FirstBusOnMarsh[1].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                            FirstBusOnMarsh[2].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                            
                            while (flag && count_loc < 100)
                            {
                                x = temp_m.Where(o => o.Id_point2 == x.Id_point1).FirstOrDefault();//берём предыдёщую локацию

                                if (x != null)
                                {
                                    if (i < FirstBusOnMarsh.Count()-2) FirstBusOnMarsh[0].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                                    if (i < FirstBusOnMarsh.Count() - 1) FirstBusOnMarsh[1].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                                    if (i < FirstBusOnMarsh.Count() - 0) FirstBusOnMarsh[2].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });

                                    var temp1 = BusOnMarsh.Where(o => o.Id_Activ_Location == x.Id).FirstOrDefault();
                                    if (temp1 != null)//проверяем нет ли там автобуса
                                    {
                                        FirstBusOnMarsh[i].TimeLocation.Add(new MyTimeLocation() { IdLocation = x.Id });
                                        FirstBusOnMarsh[i].Longitude = temp1.Longitude;
                                        FirstBusOnMarsh[i].Latitude = temp1.Latitude;
                                        FirstBusOnMarsh[i].Pos_Activ_Location = temp1.Pos_Activ_Location;
                                        FirstBusOnMarsh[i].azimuth = temp1.azimuth;
                                        FirstBusOnMarsh[i].dtUpdate = temp1.dtUpdate;
                                        i++;
                                        if (i == FirstBusOnMarsh.Count()) flag = false;
                                        
                                    }
                                }
                                else
                                    flag = false;
                                count_loc++;
                                pbBus.Value = count_loc;
                                Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                            }
                        }
                        #endregion
                        pbStep.Value=4;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                        #region Расчет времени прибытия
                        foreach (var bus in FirstBusOnMarsh)
                        {
                            if (bus.Longitude != 0 && bus.Latitude != 0)
                            {
                                MyTimeBus mtb = new MyTimeBus();//создаем этот автобус
                                mtb.Latitude = bus.Latitude;
                                mtb.Longitude = bus.Longitude;
                                mtb.Marsh = new MyMarsh()
                                {
                                    id = m1.id,
                                    name = m1.name_marshrut
                                };
                                mtb.Location = new List<MyTimeLocation>();
                                double Time = My.GetTimeDouble(bus.dtUpdate);
                                int TypeDay = My.GetTypeDay();
                                //сперва идём от автобуса до точки А
                                double AvgSpeed = 0;
                                pbBus.Value = 0;
                                pbBus.Maximum = bus.TimeLocation.Count() + LocationsBtoA.Count;
                                updatepbBusDelegate = new UpdateProgressBarDelegate(pbBus.SetValue);

                                for (int i = bus.TimeLocation.Count() - 1; i >= 0; i--)
                                {
                                    MyTimeLocation TempLoc = bus.TimeLocation[i];
                                    #region Расчет времени прохождения локации
                                    double dtDay = 0;
                                    double dtWeek = 0;
                                    double dtYear = 0;

                                    //запрос о времени прохождения на текущие время из БЗ дня, недели, года
                                    if (Time % 1 == 0)//промежуток целый
                                    {
                                        dtDay = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                        dtDay = (dtDay == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtDay;

                                        dtWeek = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                        dtWeek = (dtWeek == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtWeek;

                                        dtYear = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                        dtYear = (dtYear == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;
                                    }
                                    else
                                    {
                                        int dt1 = Convert.ToInt32(Math.Floor(Time));
                                        int dt2 = dt1 + 1;
                                        double koeff1 = Time - dt1;
                                        double koeff2 = 1.0 - koeff1;

                                        var tabDay1 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                        tabDay1 = (tabDay1 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabDay1;
                                        var tabDay2 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                        tabDay2 = (tabDay2 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabDay2;

                                        var tabWeek1 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                        tabWeek1 = (tabWeek1 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabWeek1;
                                        var tabWeek2 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                        tabWeek2 = (tabWeek2 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabWeek2;

                                        var tabYear1 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                        tabYear1 = (tabYear1 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabYear1;
                                        var tabYear2 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                        tabYear2 = (tabYear2 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabYear2;

                                        dtDay = (tabDay1 > 0 && tabDay2 > 0) ? koeff2 * tabDay1 + koeff1 * tabDay2 : 0;
                                        dtWeek = (tabWeek1 > 0 && tabWeek2 > 0) ? koeff2 * tabWeek1 + koeff1 * tabWeek2 : 0;
                                        dtYear = (tabYear1 > 0 && tabYear2 > 0) ? koeff2 * tabYear1 + koeff1 * tabYear2 : 0;
                                    }

                                    //вычисление среднего с учетом поправкок если есть коэффециент поправочный
                                    if (dtDay > 0 || dtWeek > 0 || dtYear > 0)
                                    {
                                        /*!!!!*/
                                        var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                                        double srZnach = ((koeff != 0) ? koeff : 1) * AverageVal(dtDay, dtWeek, dtYear);
                                        //запись в лист
                                        TempLoc.TimeLocation = srZnach;

                                        //сдвиг времени на значение среднего
                                        Time += srZnach / (10.0 * 60.0);//сдвигаемся на сколько то секунд и определяем номер текущего среза

                                        //определение средней скорости
                                        sLocation select_location = be.sLocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                                        AvgSpeed += select_location.length.Value / TempLoc.TimeLocation;
                                    }
                                    else
                                        break;//нет данных о локациях (мало статистики)

                                    #endregion
                                    mtb.Location.Add(TempLoc);

                                    pbBus.Value++;
                                    Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                                }
                                if (bus.TimeLocation[0].TimeLocation != 0)
                                {
                                    mtb.AtPointA = new TimeSpan(0, 0, (int)bus.TimeLocation.Sum(o => o.TimeLocation));
                                    for (int i = LocationsBtoA.Count - 1; i >= 0; i--)
                                    {
                                        MyTimeLocation TempLoc = LocationsBtoA[i];
                                        #region Расчет времени прохождения локации
                                        double dtDay = 0;
                                        double dtWeek = 0;
                                        double dtYear = 0;

                                        //запрос о времени прохождения на текущие время из БЗ дня, недели, года
                                        if (Time % 1 == 0)//промежуток целый
                                        {
                                            dtDay = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                            dtDay = (dtDay == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtDay;

                                            dtWeek = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                            dtWeek = (dtWeek == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtWeek;

                                            dtYear = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                                            dtYear = (dtYear == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;
                                        }
                                        else
                                        {
                                            int dt1 = Convert.ToInt32(Math.Floor(Time));
                                            int dt2 = dt1 + 1;
                                            double koeff1 = Time - dt1;
                                            double koeff2 = 1.0 - koeff1;

                                            var tabDay1 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                            tabDay1 = (tabDay1 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabDay1;
                                            var tabDay2 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                            tabDay2 = (tabDay2 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabDay2;

                                            var tabWeek1 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                            tabWeek1 = (tabWeek1 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabWeek1;
                                            var tabWeek2 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                            tabWeek2 = (tabWeek2 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabWeek2;

                                            var tabYear1 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                                            tabYear1 = (tabYear1 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabYear1;
                                            var tabYear2 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                                            tabYear2 = (tabYear2 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabYear2;

                                            dtDay = (tabDay1 > 0 && tabDay2 > 0) ? koeff2 * tabDay1 + koeff1 * tabDay2 : 0;
                                            dtWeek = (tabWeek1 > 0 && tabWeek2 > 0) ? koeff2 * tabWeek1 + koeff1 * tabWeek2 : 0;
                                            dtYear = (tabYear1 > 0 && tabYear2 > 0) ? koeff2 * tabYear1 + koeff1 * tabYear2 : 0;
                                        }

                                        //вычисление среднего с учетом поправкок если есть коэффециент поправочный
                                        if (dtDay > 0 || dtWeek > 0 || dtYear > 0)
                                        {
                                            /*!!!!*/
                                            var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                                            double srZnach = ((koeff != 0) ? koeff : 1) * AverageVal(dtDay, dtWeek, dtYear);
                                            //запись в лист
                                            TempLoc.TimeLocation = srZnach;

                                            //сдвиг времени на значение среднего
                                            Time += srZnach / (10.0 * 60.0);//сдвигаемся на сколько то секунд и определяем номер текущего среза

                                            //определение средней скорости
                                            sLocation select_location = be.sLocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                                            AvgSpeed += select_location.length.Value / TempLoc.TimeLocation;
                                        }
                                        else
                                            break;//нет данных о локациях (мало статистики)

                                        #endregion
                                        mtb.Location.Add(TempLoc);

                                        pbBus.Value++;
                                        Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                                    }
                                    if (LocationsBtoA.Count > 0 && LocationsBtoA[0].TimeLocation != 0)
                                    {
                                        mtb.AtPointB = new TimeSpan(0, 0, (int)LocationsBtoA.Sum(o => o.TimeLocation));
                                        mtb.AtPointB += mtb.AtPointA;
                                    }
                                    mtp.Add(mtb);
                                }
                                else
                                    mtb.Location.Clear();
                                pbBus.Value=pbBus.Maximum;
                                Dispatcher.Invoke(updatepbBusDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbBus.Value });
                            }
                        }
                        #endregion
                        pbStep.Value=5;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                    }
                }
                pbMarsh.Value++;
                Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value });

            }
            //собрать для каждого маршрута перечень локаций с автобусами GetLocationNextVal
            //по очередь перебрать все маршрты и сохранить в лист, при этом собирая уникальные локации в отдельную коллекцию
            return mtp.OrderBy(o => o.AtPointA.Value).ToList();
        }
        /// <summary>
        /// Возврат списка локаций вперёд (на n шагов, на определённое время, до определнной точки)
        /// </summary>
        /// <param name="m">Выбранный маршрут</param>
        /// <param name="Time">Временной срез</param>
        /// <param name="TypeDay">Тип дня</param>
        /// <param name="StartLocation">Начальное положение автобуса</param>
        /// <param name="MaxSeconds">Необ.: на сколько сек. вперёд</param>
        /// <param name="EndLocation">Необ.: до определённой локации</param>
        /// <param name="MaxStep">Необ.: максимальное количество шагов</param>
        /// <returns>Список локаций и время их прохождения</returns>
        public List<MyTimeLocation> GetLocationNextVal(MyMarsh m, double Time, double TypeDay, MyBusOnMarsh StartLocation, int MaxSeconds, int EndPoint, int MaxStep)
        {
            int Id_Start_Location = StartLocation.Id_Activ_Location;

            #region Условия выхода
            double TimeEnd = (MaxSeconds != 0) ? MaxSeconds + (DateTime.Now - StartLocation.dtUpdate).TotalSeconds : 0;//смотрим вперёд на 1 минуту + время последнего обновления
            int Id_End_Point = EndPoint;
            bool flag = true;
            #endregion

            List<MyTimeLocation> lmt = new List<MyTimeLocation>();
            double AvgSpeed = 0;
            var temp_m = be.sMarhList.Where(o => o.Id_Marsh == m.id).Select(o => o.sLocation);
            sLocation x1 = temp_m.Where(o => o.Id==Id_Start_Location).FirstOrDefault();//локация до точки A
            if (x1 != null)
            {
                //sLocation x1 = x.sLocation;
                //while (protect>0)
                while (flag)
                {
                    MyTimeLocation TempLoc = new MyTimeLocation() { IdLocation = x1.Id };

                    #region Расчет времени прохождения локации
                    double dtDay = 0;
                    double dtWeek = 0;
                    double dtYear = 0;

                    //запрос о времени прохождения на текущие время из БЗ дня, недели, года
                    if (Time % 1 == 0)//промежуток целый
                    {
                        dtDay = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                        dtDay = (dtDay == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtDay;

                        dtWeek = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                        dtWeek = (dtWeek == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtWeek;

                        dtYear = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                        dtYear = (dtYear == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;
                    }
                    else
                    {
                        int dt1 = Convert.ToInt32(Math.Floor(Time));
                        int dt2 = dt1 + 1;
                        double koeff1 = Time - dt1;
                        double koeff2 = 1.0 - koeff1;

                        var tabDay1 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                        tabDay1 = (tabDay1 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabDay1;
                        var tabDay2 = be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                        tabDay2 = (tabDay2 == 0) ? be.bz_day.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabDay2;

                        var tabWeek1 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                        tabWeek1 = (tabWeek1 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabWeek1;
                        var tabWeek2 = be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                        tabWeek2 = (tabWeek2 == 0) ? be.bz_week.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabWeek2;

                        var tabYear1 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1).Select(o => o.Val).FirstOrDefault();
                        tabYear1 = (tabYear1 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt1 - 1).Select(o => o.Val).FirstOrDefault() : tabYear1;
                        var tabYear2 = be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2).Select(o => o.Val).FirstOrDefault();
                        tabYear2 = (tabYear2 == 0) ? be.bz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == dt2 + 1).Select(o => o.Val).FirstOrDefault() : tabYear2;

                        dtDay = (tabDay1 > 0 && tabDay2 > 0) ? koeff2 * tabDay1 + koeff1 * tabDay2 : 0;
                        dtWeek = (tabWeek1 > 0 && tabWeek2 > 0) ? koeff2 * tabWeek1 + koeff1 * tabWeek2 : 0;
                        dtYear = (tabYear1 > 0 && tabYear2 > 0) ? koeff2 * tabYear1 + koeff1 * tabYear2 : 0;
                    }

                    //вычисление среднего с учетом поправкок если есть коэффециент поправочный
                    if (dtDay > 0 || dtWeek > 0 || dtYear > 0)
                    {
                        /*!!!!*/
                        var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                        double srZnach = ((koeff != 0) ? koeff : 1) * AverageVal(dtDay, dtWeek, dtYear);
                        //запись в лист
                        TempLoc.TimeLocation = srZnach;
                        lmt.Add(TempLoc);

                        //сдвиг времени на значение среднего
                        Time += srZnach / (10.0 * 60.0);//сдвигаемся на сколько то секунд и определяем номер текущего среза
                        TimeEnd -= srZnach;

                        //определение средней скорости
                        sLocation select_location = be.sLocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                        AvgSpeed += select_location.length.Value / TempLoc.TimeLocation;
                    }
                    else
                    {
                        //если нет информации, то используем среднюю скорость на предыдущих участках
                        if (AvgSpeed != 0)
                        {
                            //запись в лист средней скорости
                            sLocation select_location = be.sLocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                            TempLoc.TimeLocation = select_location.length.Value / (AvgSpeed / lmt.Count);
                            lmt.Add(TempLoc);

                            //сдвиг времени на значение среднего
                            Time += TempLoc.TimeLocation / (10.0 * 60.0);
                            TimeEnd -= TempLoc.TimeLocation;

                            //определение средней скорости
                            AvgSpeed += AvgSpeed / lmt.Count;
                        }
                        else
                            return null;//нет данных о локациях (мало статистики)
                    }

                    #endregion

                    #region Условия выхода
                    if (MaxSeconds != 0) if (TimeEnd <= 0) flag = false;                //прошло заданное количество сек
                    if (EndPoint != 0) if (x1.Id_point2 == Id_End_Point) flag = false;  //подошли до нужной точке
                    if (MaxStep != 0) if (lmt.Count > MaxStep) flag = false;              //превысилось количество вычислений
                    #endregion

                    if (flag)//если ещё не конец, ищём следущую локацию
                    {
                        List<sLocation> temp = temp_m.Where(o => o.Id_point1 == x1.Id_point2 ).ToList();
                        if (temp.Count > 0)
                        {
                            #region Определение следущей локации
                            if (temp.Count == 1)
                            {
                                if (temp[0].Id_point1 == temp[0].Id_point2)
                                    return null;//ошибочная локация

                                //TempLoc = new MyTimeLocation() { IdLocation = temp[0].Id };
                                x1 = temp[0];
                            }
                            else if (temp.Count == 2)
                            {
                                //имеется 2 варианта пути, нужно найти верный
                                sLocation l1 = temp[0];
                                sLocation l2 = temp[1];
                                //ищем разницу между векторами текущей и следущей локации
                                double err_l1 = Math.Min(Math.Abs(l1.azimuth.Value - x1.azimuth.Value), Math.Abs(Math.Abs(l1.azimuth.Value - x1.azimuth.Value) - 360));
                                double err_l2 = Math.Min(Math.Abs(l2.azimuth.Value - x1.azimuth.Value), Math.Abs(Math.Abs(l2.azimuth.Value - x1.azimuth.Value) - 360));

                                if (err_l1 < err_l2)
                                    x1 = l1;//TempLoc = new MyTimeLocation() { IdLocation = l1.Id };
                                else
                                    x1 = l2;//TempLoc = new MyTimeLocation() { IdLocation = l2.Id };
                            }
                            else
                            {
                                //если возможных локаций много, то следует ввести поле "следущая локация" и проверять сдесь наличие её
                                return null;
                            }
                            #endregion
                        }
                        else
                            return null;//не обнаружина следущая локация (обрыв маршрута?)
                    }
                }
            }
            else
                return null;//не найдена активная локация
            return lmt;
        }
        #endregion

        #region Муравьи
        //поиск пути по точкам
        private void Button_ant_point(object sender, RoutedEventArgs e)
        {
            select = 668;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                Short_way_ant(pp1, pp2);
                //Short_way_ant_by_point(pp1, pp2);
            }
        }

        private void Button_ant_point_time(object sender, RoutedEventArgs e)
        {
            select = 669;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                Short_way_ant_time(pp1, pp2);
                //Short_way_ant_by_point(pp1, pp2);
            }
        }

        //кратчайший путь по точкам, перебор больших локаций
        //кроме первой и последней-их рисуем по малым.
        private void Short_way_ant_by_point(sKoord_point a, sKoord_point b)
        {
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);
            UpdateProgressBarDelegate updatepbStepDelegate = new UpdateProgressBarDelegate(pbStep.SetValue);
            int maxStep = 10;
            int MxAnt = 500;

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            way_repos wr = new way_repos();

            Stopwatch time = new Stopwatch();

            pbMarsh.Maximum = maxStep;
            pbMarsh.Value = 0;
            pbStep.Maximum = MxAnt;
            pbStep.Value = 0;
            Random rnd = new Random();
            List <Big_Location> BbLoc = new List<Big_Location>();
            
            time.Start();
            for (int i=0;i<b.sLocation1.Count;i++)
            {
                int r = rnd.Next(0, b.sLocation.Count);
                sLocation B=((sLocation)b.sLocation1.ElementAt(i));
                BbLoc.Add(be.Big_Location.Where(o => o.Id_big == B.id_big).FirstOrDefault());
            }

            List<feromon> feromon_list = new List<feromon>();
            ant besAnt = new ant();
            double p = 0.5;
            double dobav = 0.7;
            foreach (var l in be.Big_Location)
            {
                feromon tempf = new feromon();
                tempf.big_loc = l;
                tempf.feromon_value = 1;
                feromon_list.Add(tempf);
            }
            for (int t = 0; t < maxStep; t++)
            {
                List<ant> tant = new List<ant>();
                for (int j = 0; j < MxAnt; j++)
                {
                    ant tempant = new ant();
                    tempant.locAnt = new List<Big_Location>();
                    if (a.sLocation.Count != 0)
                    {
                        int r = rnd.Next(0, a.sLocation.Count);
                        sLocation A = ((sLocation)a.sLocation.ElementAt(r));
                        Big_Location AA = be.Big_Location.Where(o => o.Id_big == A.id_big).FirstOrDefault();
                        tempant.locAnt.Add(AA);
                    }
                    else
                    {
                        int r = rnd.Next(0, a.sLocation1.Count);
                        sLocation A = ((sLocation)a.sLocation1.ElementAt(r));
                        Big_Location AA = be.Big_Location.Where(o => o.Id_big == A.id_big).FirstOrDefault();
                        tempant.locAnt.Add(AA);
                    }

                    tant.Add(tempant);
                }
                int nomAnt = 0;
                foreach (var q in tant)
                {
                    do
                    {
                        if (q.locAnt.Count() > 500)
                        {
                            q.locAnt.Clear();
                            break;
                        }

                        var listt = feromon_list.Where(o => o.big_loc.id_koord1 == q.locAnt.LastOrDefault().id_koord2).ToList();
                        if (listt.Count() == 0)
                        {
                            q.locAnt.Clear();
                            break;
                        }
                        else if (listt.Count() == 1)
                        {
                            q.locAnt.Add(listt[0].big_loc);
                        }
                        else
                        {
                            double sumplo = listt.Sum(o => o.plot);
                            double arnd = rnd.NextDouble();
                            double pp = 0;
                            int k = 0;
                            for (int j = 0; j < listt.Count; j++)
                            {
                                pp += listt[j].plot / sumplo;
                                if (arnd <= pp)
                                { k = j; break; }
                            }
                            q.locAnt.Add(listt[k].big_loc);
                        }
                    } while (q.locAnt.Last() != BbLoc.FirstOrDefault());

                    pbStep.Value = nomAnt;
                    nomAnt++;
                    Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                }

                foreach (var fer1 in feromon_list)
                {
                    fer1.feromon_value *= (1 - p);
                }

                foreach (var q2 in tant)
                {
                    foreach (var fer1 in q2.locAnt)
                    {
                        feromon_list.Where(o => o.big_loc.Id_big == fer1.Id_big).FirstOrDefault().feromon_value += dobav;
                    }
                }


                besAnt = tant.Where(o => o.locAnt.Count() > 0).OrderBy(o => o.locAnt.Sum(q => q.length)).FirstOrDefault();

                pbMarsh.Value = t;
                Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value });
            }
            time.Stop();
            double length_way =0;
            if (besAnt == null)
                MessageBox.Show("Ничего не найдено");
            else
            {

                TimeSpan ts = time.Elapsed; 
                wr.name_algoritm = "Муравьиный";
                wr.a = a;
                wr.b = b;

                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                               
                if (cbTime.SelectedItem!=null) wr.t = cbTime.SelectedItem.ToString();
                wr.length = Math.Round((double) print_way_ant_final(a, b, besAnt, (SolidColorBrush)(new BrushConverter().ConvertFrom("#CE93D8")), wr));

                if (tp != null)
                {
                    wr.t = tp.timeString;
                    math_time_motion(wr, tp);
                }
                wr.colour = wr.way.ElementAt(0).Color;
                w_repos.Add(wr);
                window_report.review(w_repos);

            }
        
        }

        //этот работает
        private void Short_way_ant(sKoord_point a, sKoord_point b)
        {
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);
            UpdateProgressBarDelegate updatepbStepDelegate = new UpdateProgressBarDelegate(pbStep.SetValue);
            int maxStep = 10;//количество колоний
            int MxAnt = 300;

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            way_repos wr = new way_repos();

            Stopwatch time = new Stopwatch();

            pbMarsh.Maximum = maxStep;
            pbMarsh.Value = 1;
            pbStep.Maximum = MxAnt;
            pbStep.Value = 1;
            Random rnd = new Random();
            List<Big_Location> BbLoc = new List<Big_Location>();

            time.Start();
            for (int i = 0; i < b.sLocation1.Count; i++)
            {
                sLocation B = ((sLocation)b.sLocation1.ElementAt(i));
                BbLoc.Add(be.Big_Location.Where(o => o.Id_big == B.id_big).FirstOrDefault());
            }

            List<feromon> feromon_list = new List<feromon>();
            ant besAnt = new ant();
            double p = 0.3;
            double dobav = 0.05;

            List<ant> fin_ant = new List<ant>();

            var ramBiglocation = filter_location();
            //создание листа феромонов и заполнение его единицами
            foreach (var l in ramBiglocation)
            {
                feromon tempf = new feromon();
                tempf.big_loc = l;
                tempf.feromon_value = 3;
                feromon_list.Add(tempf);
            }

            if (a.sLocation.Count != 0)
            {
                for (int t = 0; t < maxStep; t++)
                {
                    List<ant> tant = new List<ant>();
                    //создаем колонию
                    for (int j = 0; j < MxAnt; j++)
                    {
                        ant tempant = new ant();
                        tempant.locAnt = new List<Big_Location>();
                        int r = rnd.Next(0, a.sLocation.Count);
                        sLocation A = ((sLocation)a.sLocation.ElementAt(r));
                        Big_Location AA = ramBiglocation.Where(o => o.Id_big == A.id_big).FirstOrDefault();
                        tempant.locAnt.Add(AA);
                        tant.Add(tempant);
                    }

                    int nomAnt = 0;
                    foreach (var q in tant)
                    {
                        do
                        {
                            if (q.locAnt.Count() > 200)
                            {
                                q.locAnt.Clear();
                                break;
                            }

                            var listt = feromon_list.Where(o => o.big_loc.id_koord1 == q.locAnt.LastOrDefault().id_koord2).ToList();
                            if (listt.Count() == 0)
                            {
                               q.locAnt.Clear();
                               break;
                            }
                            else if (listt.Count() == 1)
                            {
                                q.locAnt.Add(listt[0].big_loc);
                            }
                            else
                            {
                                double sumplo = listt.Sum(o => o.plot);
                                double arnd = rnd.NextDouble();
                                double pp = 0;
                                int k = 0;
                                for (int j = 0; j < listt.Count; j++)
                                {
                                    pp += listt[j].plot / sumplo;
                                    if (arnd <= pp)
                                    { k = j; break; }
                                }
                                q.locAnt.Add(listt[k].big_loc);
                                
                            }
                        } while (q.locAnt.Last() != BbLoc.FirstOrDefault());

                        pbStep.Value = nomAnt;
                        nomAnt++;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value+1 });
                    }

                    if (tant.Count != 0)
                    {
                        foreach (var fer1 in feromon_list)
                        {
                            fer1.feromon_value *= (1 - p);
                        }
                    }

                    foreach (var q2 in tant)
                    {
                        foreach (var fer1 in q2.locAnt)
                        {
                            feromon_list.Where(o => o.big_loc.Id_big == fer1.Id_big).FirstOrDefault().feromon_value += dobav;
                        }
                    }


                    besAnt = tant.Where(o => o.locAnt.Count() > 0).OrderBy(o => o.locAnt.Sum(q => q.length)).FirstOrDefault();

                    fin_ant.Add(besAnt);

                    pbMarsh.Value = t;
                    Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value+1 });
                }
            }
            time.Stop();

            if (besAnt == null)
                MessageBox.Show("Ничего не найдено");
            else
            {

                TimeSpan ts = time.Elapsed; wr.name_algoritm = "Муравьиный";
                wr.a = a;
                wr.b = b;
                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();

                if (cbTime.SelectedItem != null) wr.t = cbTime.SelectedItem.ToString();
                wr.length = Math.Round((double)print_way_ant_final(a, b, fin_ant.Where(o => o.locAnt.Count() > 0).OrderBy(o => o.locAnt.Sum(q => q.length)).FirstOrDefault(), (SolidColorBrush)(new BrushConverter().ConvertFrom("#8E24AA")), wr));
                if (tp != null)
                {
                    wr.t = tp.timeString;
                    math_time_motion(wr, tp);
                }
                wr.colour = wr.way.ElementAt(0).Color;
                w_repos.Add(wr);
                window_report.review(w_repos);

            }
        }

        private void Short_way_ant_time(sKoord_point a, sKoord_point b)
        {
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);
            UpdateProgressBarDelegate updatepbStepDelegate = new UpdateProgressBarDelegate(pbStep.SetValue);
            int maxStep = 10;//количество колоний
            int MxAnt = 300;

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            way_repos wr = new way_repos();

            Stopwatch time = new Stopwatch();

            pbMarsh.Maximum = maxStep;
            pbMarsh.Value = 1;
            pbStep.Maximum = MxAnt;
            pbStep.Value = 1;
            Random rnd = new Random();
            List<Big_Location> BbLoc = new List<Big_Location>();

            time.Start();
            for (int i = 0; i < b.sLocation1.Count; i++)
            {
                sLocation B = ((sLocation)b.sLocation1.ElementAt(i));
                BbLoc.Add(be.Big_Location.Where(o => o.Id_big == B.id_big).FirstOrDefault());
            }

            List<feromon> feromon_list = new List<feromon>();
            ant besAnt = new ant();
            double p = 0.3;
            double dobav = 0.05;

            var ramBiglocation = filter_location();
            //создание листа феромонов и заполнение его единицами
            foreach (var l in ramBiglocation)
            {
                feromon tempf = new feromon();
                tempf.big_loc = l;
                tempf.feromon_value = 3;
                feromon_list.Add(tempf);
            }

            List<ant> final_ant = new List<ant>();

            if (a.sLocation.Count != 0)
            {
                for (int t = 0; t < maxStep; t++)
                {
                    List<ant> tant = new List<ant>();
                    //создаем колонию
                    for (int j = 0; j < MxAnt; j++)
                    {
                        ant tempant = new ant();
                        tempant.locAnt = new List<Big_Location>();
                        int r = rnd.Next(0, a.sLocation.Count);
                        sLocation A = ((sLocation)a.sLocation.ElementAt(r));
                        Big_Location AA = ramBiglocation.Where(o => o.Id_big == A.id_big).FirstOrDefault();
                        tempant.locAnt.Add(AA);
                        tant.Add(tempant);
                    }

                    int nomAnt = 0;
                    foreach (var q in tant)
                    {
                        do
                        {
                            if (q.locAnt.Count() > 200)
                            {
                                q.locAnt.Clear();
                                break;
                            }

                            var listt = feromon_list.Where(o => o.big_loc.id_koord1 == q.locAnt.LastOrDefault().id_koord2).ToList();
                            if (listt.Count() == 0)
                            {
                                q.locAnt.Clear();
                                break;
                            }
                            else if (listt.Count() == 1)
                            {
                                q.locAnt.Add(listt[0].big_loc);
                            }
                            else
                            {
                                double sumplo = listt.Sum(o => o.plot);
                                double arnd = rnd.NextDouble();
                                double pp = 0;
                                int k = 0;
                                for (int j = 0; j < listt.Count; j++)
                                {
                                    pp += listt[j].plot / sumplo;
                                    if (arnd <= pp)
                                    { k = j; break; }
                                }
                                q.locAnt.Add(listt[k].big_loc);

                            }
                        } while (q.locAnt.Last() != BbLoc.FirstOrDefault());

                        pbStep.Value = nomAnt;
                        nomAnt++;
                        Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value + 1 });
                    }

                    if (tant.Count != 0)
                    {
                        foreach (var fer1 in feromon_list)
                        {
                            fer1.feromon_value *= (1 - p);
                        }
                    }

                    foreach (var q2 in tant)
                    {
                        foreach (var fer1 in q2.locAnt)
                        {
                            feromon_list.Where(o => o.big_loc.Id_big == fer1.Id_big).FirstOrDefault().feromon_value += dobav;
                        }
                    }

                    ant tmp = new ant();
                    double time_motion = 0;
                    foreach(var temp in tant)
                    {
                        if (temp.locAnt.Count > 0)
                        {
                            time.Stop();
                            double ttt = math_average_time_loc_ant(tp.timeString, temp.locAnt, null);
                            if (ttt > 0)
                            {
                                if (time_motion == 0 || ttt < time_motion)
                                {
                                    time_motion = ttt;
                                    tmp = temp;
                                }
                            }
                            time.Start();
                            
                        }
                    }

                    besAnt = tmp;
                    final_ant.Add(tmp);

                    pbMarsh.Value = t;
                    Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value + 1 });
                }
            }
            time.Stop();

            if (final_ant.Count() == null)
                MessageBox.Show("Ничего не найдено");
            else
            {

                TimeSpan ts = time.Elapsed; wr.name_algoritm = "Муравьиный";
                wr.a = a;
                wr.b = b;
                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();

                if (cbTime.SelectedItem != null) wr.t = cbTime.SelectedItem.ToString();

                ant tmp = new ant();
                double time_motion = 0;
                foreach (var temp in final_ant)
                {
                    if (temp.locAnt.Count > 0)
                    {
                        time.Stop();
                        double ttt = math_average_time_loc_ant(tp.timeString, temp.locAnt, null);
                        if (ttt > 0)
                        {
                            if (time_motion == 0 || ttt < time_motion)
                            {
                                time_motion = ttt;
                                tmp = temp;
                            }
                        }
                        time.Start();
                    }
                }

                wr.length = Math.Round((double)print_way_ant_final(a, b, tmp, (SolidColorBrush)(new BrushConverter().ConvertFrom("#607D8B")), wr));
                if (tp != null)
                {
                    wr.t = tp.timeString;
                    math_time_motion(wr, tp);
                }
                wr.colour = wr.way.ElementAt(0).Color;
                w_repos.Add(wr);
                window_report.review(w_repos);

            }
        }

        public double math_average_time_loc_ant(String tp, List<Big_Location> bll, sLocation sl)
        {
            double Time = My.GetTimeDouble(DateTime.Parse(tp));
            int TypeDay = select_type_day;
            double AvgTime = 0;
            foreach (var bl in bll)
            {
                List<sLocation> bloc = new List<sLocation>();
                if (bl == null)
                {
                    bloc.Add(sl);
                }
                else
                {
                    bloc = sort_big_local(bl);
                }


                for (int i = 0; i < bloc.Count; i++)
                {
                    MyTimeLocation TempLoc = new MyTimeLocation();
                    TempLoc.IdLocation = bloc[i].Id;

                    //запрос о времени прохождения на текущие время из БЗ дня, недели, года
                    //double dtDay = 0;
                    //double dtWeek = 0;
                    double dtYear = 0;
                    double srZnach = 0;

                    dtYear = bbz_year.Where(o=>o.Location == TempLoc.IdLocation).Select(o => o.Val).FirstOrDefault();
                    //dtYear = (dtYear == 0) ? bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;

                    if (dtYear > 0)
                    {/*!!!!*/
                        var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                        srZnach = ((koeff != 0) ? koeff : 1) * dtYear;
                        //запись в лист
                        //TempLoc.TimeLocation = srZnach;

                        //определение средней скорости
                        //sLocation select_location = bslocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                        AvgTime += srZnach;
                        //AvgTime += TempLoc.TimeLocation;
                    }

                }
            }
            return AvgTime;
        }

        //отсеять тупики
        private List <Big_Location> filter_location()
        {
            var ramBiglocation = be.Big_Location.ToList();
            List<Big_Location> lbl = new List<Big_Location>();

            foreach (var b in ramBiglocation)
            {
                var listt = ramBiglocation.Where(o => o.id_koord1 == b.id_koord2).ToList();
                if (listt.Count() > 0)
                {
                    lbl.Add(b);
                }
            }

            List<Big_Location> flbl = new List<Big_Location>();
            foreach (var b in lbl)
            {
                var listt = lbl.Where(o => o.id_koord1 == b.id_koord2).ToList();
                if (listt.Count() > 0)
                {
                    flbl.Add(b);
                }
            }
            return flbl;
        }
   
        private double print_way_ant_final(sKoord_point a, sKoord_point b, ant besAnt, SolidColorBrush c, way_repos wr)
        {
            double length_way = 0;
            
            for (int i = 0; i < besAnt.locAnt.Count; i++)
            {
                if (i == 0)
                {
                    List<sLocation> d = sort_big_local(besAnt.locAnt.ElementAt(i));
                    int k = 0;
                    for (int j=0;j<d.Count();j++){
                        if (d[j].sKoord_point == a) k = j;
                    }
                    for (int j = k; j < d.Count(); j++)
                    {
                        MyLocation ml = new MyLocation();
                        ml.PointA = new MyPoint() { Latitude = d[j].sKoord_point.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point.Longitude.Value + 0.00045, id = d[j].Id_point1 };
                        ml.PointB = new MyPoint() { Latitude = d[j].sKoord_point1.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point1.Longitude.Value + 0.00045, id = d[j].Id_point2 };
                        ml.way = d.ElementAt(j).length.Value;
                        ml.Color = c;
                        ml.id_loc = d[j].Id;
                        wr.way.Add(ml);
                        map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                        length_way += ml.way;
                    }
                }
                else if (i == besAnt.locAnt.Count - 1)
                {
                    List<sLocation> d = sort_big_local(besAnt.locAnt.ElementAt(i));
                    int k = 0;
                    for (int j = 0; j < d.Count(); j++)
                    {
                        if (d[j].sKoord_point1 == b) k = j;
                    }
                    for (int j = 0; j < k+1; j++)
                    {
                        MyLocation ml = new MyLocation();
                        ml.PointA = new MyPoint() { Latitude = d[j].sKoord_point.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point.Longitude.Value + 0.00045, id = d[j].Id_point1 };
                        ml.PointB = new MyPoint() { Latitude = d[j].sKoord_point1.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point1.Longitude.Value + 0.00045, id = d[j].Id_point2 };
                        ml.way = d.ElementAt(j).length.Value;
                        ml.Color = c;
                        ml.id_loc = d[j].Id;
                        wr.way.Add(ml);
                        map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                        length_way += ml.way;
                    }
                }
                else
                {
                    List<sLocation> d = sort_big_local(besAnt.locAnt.ElementAt(i));
                    for (int j = 0; j < d.Count; j++)
                    {
                        MyLocation ml = new MyLocation();
                        ml.way = 0;
                        ml.PointA = new MyPoint() { Latitude = d[j].sKoord_point.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point.Longitude.Value + 0.00045, id = d[j].Id_point1 };
                        ml.PointB = new MyPoint() { Latitude = d[j].sKoord_point1.Latitude.Value + 0.00045, Longitude = d[j].sKoord_point1.Longitude.Value + 0.00045, id = d[j].Id_point2 };
                        if (d.ElementAt(j).length.Value!=null) ml.way = d.ElementAt(j).length.Value;
                        ml.Color = c;
                        ml.id_loc = d[j].Id;
                        wr.way.Add(ml);
                        map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                        length_way += ml.way;
                    }
                }
            }

            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;

            return length_way;
        }

        public class ant
        {
            public List<Big_Location> locAnt;
            public override string ToString()
            {
              return  locAnt.Count().ToString();
            }
        }
        public class feromon
        {
            public Big_Location big_loc;
            public double feromon_value;
            public double plot
            {
                get {return feromon_value / big_loc.length; }
            }
        }
        public class ant_point
        {
            public List<sLocation> locAnt;
            public override string ToString()
            {
                return locAnt.Count().ToString();
            }
        }
        public class feromon_s
        {
            public sLocation sloc;
            public double fer_value;
            public double? plot
            {
                get { return fer_value / sloc.length; }
            }
        }
        //поиск пути по локациям
        //не используется
        private void Button_ant_location(object sender, RoutedEventArgs e)
        {
            select = 666;
            foreach (var bl in be.Big_Location)
            {
                MyLocation ml = new MyLocation();
                ml.nom = bl.Id_big;
                ml.PointA = new MyPoint() { Latitude = bl.sKoord_point.Latitude.Value, Longitude = bl.sKoord_point.Longitude.Value };
                ml.PointB = new MyPoint() { Latitude = bl.sKoord_point1.Latitude.Value, Longitude = bl.sKoord_point1.Longitude.Value };
                map1.Children.Add(PrintLocationOnMap(ml));
            }
        }
        // поиск короткого пути по локациям
        //не используется
        private void Short_way_ant_by_location(int a, int b)
        {
            UpdateProgressBarDelegate updatepbMarshDelegate = new UpdateProgressBarDelegate(pbMarsh.SetValue);
            UpdateProgressBarDelegate updatepbStepDelegate = new UpdateProgressBarDelegate(pbStep.SetValue);
            int maxStep = 10;
            int MxAnt = 500;

            pbMarsh.Maximum = maxStep;
            pbMarsh.Value = 0;
            pbStep.Maximum = MxAnt;
            pbStep.Value = 0;
            Random rnd = new Random();
            Big_Location A = be.Big_Location.Where(o => o.Id_big == a).FirstOrDefault();
            Big_Location B = be.Big_Location.Where(o => o.Id_big == b).FirstOrDefault();
            List<feromon> ppp = new List<feromon>();
            ant besAnt = new ant();
            double p = 0.5;
            double dobav = 0.7;
            foreach (var l in be.Big_Location)
            {
                feromon tempf = new feromon();
                tempf.big_loc = l;
                tempf.feromon_value = 1;
                ppp.Add(tempf);
            }
            for (int t = 0; t < maxStep; t++)
            {

                List<ant> tant = new List<ant>();
                for (int j = 0; j < MxAnt; j++)
                {
                    ant tempant = new ant();
                    tempant.locAnt = new List<Big_Location>();
                    tempant.locAnt.Add(A);
                    tant.Add(tempant);
                }
                int nomAnt = 0;
                foreach (var q in tant)
                {
                    do
                    {
                        if (q.locAnt.Count() > 500)
                        {
                            q.locAnt.Clear();
                            break;
                        }

                        var listt = ppp.Where(o => o.big_loc.id_koord1 == q.locAnt.LastOrDefault().id_koord2).ToList();
                        if (listt.Count() == 0)
                        {
                            q.locAnt.Clear();
                            break;
                        }
                        else if (listt.Count() == 1)
                        {
                            q.locAnt.Add(listt[0].big_loc);
                        }
                        else
                        {
                            double sumplo = listt.Sum(o => o.plot);
                            double arnd = rnd.NextDouble();
                            double pp = 0;
                            int k = 0;
                            for (int j = 0; j < listt.Count; j++)
                            {
                                pp += listt[j].plot / sumplo;
                                if (arnd <= pp)
                                { k = j; break; }
                            }
                            q.locAnt.Add(listt[k].big_loc);
                        }
                    } while (q.locAnt.LastOrDefault() != B);

                    pbStep.Value = nomAnt;
                    nomAnt++;
                    Dispatcher.Invoke(updatepbStepDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbStep.Value });
                }

                foreach (var fer1 in ppp)
                {
                    fer1.feromon_value *= (1 - p);
                }

                foreach (var q2 in tant)
                {
                    foreach (var fer1 in q2.locAnt)
                    {
                        ppp.Where(o => o.big_loc.Id_big == fer1.Id_big).FirstOrDefault().feromon_value += dobav;
                    }
                }


                besAnt = tant.Where(o => o.locAnt.Count() > 0).OrderBy(o => o.locAnt.Sum(q => q.length)).FirstOrDefault();

                pbMarsh.Value = t;
                Dispatcher.Invoke(updatepbMarshDelegate, System.Windows.Threading.DispatcherPriority.Background, new object[] { ProgressBar.ValueProperty, pbMarsh.Value });

                //MessageBox.Show(tant.Where(o => o.locAnt.Count() != 0).Min(n => n.locAnt.Sum(q => q.length)).ToString());
            }
            if (besAnt == null)
                MessageBox.Show("");
            else
                foreach (var bl in besAnt.locAnt)
                {
                    MyLocation ml = new MyLocation();
                    ml.nom = bl.Id_big;
                    ml.PointA = new MyPoint() { Latitude = bl.sKoord_point.Latitude.Value, Longitude = bl.sKoord_point.Longitude.Value };
                    ml.PointB = new MyPoint() { Latitude = bl.sKoord_point1.Latitude.Value, Longitude = bl.sKoord_point1.Longitude.Value };
                    map1.Children.Add(PrintLocationOnMap(ml));
                }
            #region
            //var MyL = be.Big_Location.ToList();
            //int count = MyL.Count;
            //int[,] matr = new int[count, count]; //матрица растояний
            //int[,] fer = new int[count, count]; //матрица растояний
            ////заполняем массив веромонов еденицами
            //for (int i = 0; i > count; i++)
            //{
            //    for (int j = 0; j > count; j++)
            //    {
            //        fer[i, j] = 1;
            //    }
            //}
            ////создать колонию (колония есть список локаций)
            //List<BigLoc> colon = new List<BigLoc>();
            //foreach (var rrr in be.Big_Location)
            //{
            //    BigLoc bbll = new BigLoc();
            //    bbll.id = rrr.id_big;
            //    bbll.leng = Convert.ToInt32(rrr.length);
            //    bbll.A = new MyPoint() { Latitude = rrr.sKoord_point.Latitude.Value, Longitude = rrr.sKoord_point.Longitude.Value };
            //    bbll.B = new MyPoint() { Latitude = rrr.sKoord_point1.Latitude.Value, Longitude = rrr.sKoord_point1.Longitude.Value };
            //    colon.Add(bbll);
            //}
            //// значения для формул
            //float alf = Convert.ToInt32(0.3);
            //float bet = Convert.ToInt32(0.3);
            //float eps = Convert.ToInt32(2.7);
            //float Q = Convert.ToInt32(0.5);
            //float L = Convert.ToInt32(20000); //оптимальный путь для сравнения
            //int tMAX = 100; //время жизни колонии
            ////основной цикл
            //for (int t = 1; t > tMAX; t++)
            //{
            //    for (int k = 1; k > count; k++) //цикл по муровьям количесвто муравьев = городам
            //    {

            //    }
            //}
            #endregion
        }
        #endregion

        #region Дейкстра
        //поиск пути Дейкстра
        private void Button_dijkstra(object sender, RoutedEventArgs e)
        {
            select = 777;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_dijkstra_by_point_big_local(pp1, pp2);
                //short_way_dijkstra_by_point(pp1, pp2);
            }
        }

        private void Button_dijkstra_time(object sender, RoutedEventArgs e)
        {
            select = 778;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_dijkstra_time(pp1, pp2);
                //short_way_dijkstra_by_point(pp1, pp2);
            }
        }

        private void short_way_dijkstra_by_point_big_local(sKoord_point a, sKoord_point b)
        {
            List<dijkstra_point> point_list = new List<dijkstra_point>();
            List<dijkstra_point> point_visit = new List<dijkstra_point>();
            //найдем большие локации для начала и конца
            List<Big_Location> bl_A = new List<Big_Location>();
            List<Big_Location> bl_B = new List<Big_Location>();
            List<sLocation> sl_A = new List<sLocation>();
            List<sLocation> sl_B = new List<sLocation>();

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            Stopwatch time = new Stopwatch();

            time.Start();
            if (a.sLocation.Count != 0)
            {
                for (int i = 0; i < a.sLocation.Count; i++)
                {
                    sLocation f = a.sLocation.ElementAt(i);
                    bl_A.Add(f.Big_Location);
                }
            }
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_B.Add(f.Big_Location);
                }
            }

            foreach (var bl in bl_A)
            {
                bl.sLocation = sort_big_local(bl);
                sKoord_point f = bl.sKoord_point1;
                sl_A = ((List<sLocation>)bl.sLocation);
                sLocation i = bl.sLocation.Where(o => o.sKoord_point == a).FirstOrDefault();
                double? l = 0;
                for (int j = sl_A.IndexOf(i); j < sl_A.Count; j++) { l = l + sl_A.ElementAt(j).length; }
                dijkstra_point p = new dijkstra_point();
                dijkstra_point start = new dijkstra_point();
                start.point_in = a;
                start.length = 0;
                p.point_out = start;
                p.point_in = f;
                p.length = l;
                p.visit = false;
                point_list.Add(p);
                point_visit.Add(start);
            }

            do
            {
                dijkstra_point start = new dijkstra_point();
                double? l = Double.PositiveInfinity;
                //находим непосещенную вершину с минимальным расстоянием
                foreach (var p in point_list)
                {
                    if (p.length < l && p.visit == false) { l = p.length; start = p; }
                }

                //заполняем лист с большими локациями, начинающимися из этой точки
                List<Big_Location> BB = new List<Big_Location>();
                foreach (var bl in be.Big_Location) { if (bl.sKoord_point == start.point_in) BB.Add(bl); }

                if (BB.Where(o => o.sKoord_point == bl_B.ElementAt(0).sKoord_point).FirstOrDefault() != null) break;

                foreach (var bl in BB)
                {
                    dijkstra_point input = point_list.Where(o => o.point_in == bl.sKoord_point1).FirstOrDefault();
                    if (input != null)
                    {
                        if (input.length > l + bl.length) { input.point_out = start; input.length = l + bl.length; }
                    }
                    else
                    {
                        dijkstra_point p = new dijkstra_point();
                        p.point_out = start;
                        p.point_in = bl.sKoord_point1;
                        p.length = l + bl.length;
                        p.visit = false;
                        point_list.Add(p);
                    }
                }
                point_visit.Add(start);
                foreach (var p in point_list)
                {
                    if (p == start) { p.visit = true; }
                }

            } while (point_list.Where(o => o.point_in == bl_B.ElementAt(0).sKoord_point) != null);

            time.Stop();


            sKoord_point asas = bl_B.ElementAt(0).sKoord_point;

            List<sLocation> z = sort_big_local(bl_B.ElementAt(0));
            wr.name_algoritm = "Дейкстры";
            print_way_dijkstra(point_list, asas, wr, z);
            if (tp != null)
            {
                wr.t = tp.timeString;
                math_time_motion(wr, tp);
            }
            TimeSpan ts = time.Elapsed;
            wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
            wr.colour = wr.way.ElementAt(0).Color;
            w_repos.Add(wr);
            window_report.review(w_repos);
        }

        private void short_way_dijkstra_time(sKoord_point a, sKoord_point b)
        {
            List<dijkstra_point> point_list = new List<dijkstra_point>();
            List<dijkstra_point> point_visit = new List<dijkstra_point>();
            //найдем большие локации для начала и конца
            List<Big_Location> bl_A = new List<Big_Location>();
            List<Big_Location> bl_B = new List<Big_Location>();
            List<sLocation> sl_A = new List<sLocation>();
            List<sLocation> sl_B = new List<sLocation>();

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            Stopwatch time = new Stopwatch();

            time.Start();
            if (a.sLocation.Count != 0)
            {
                for (int i = 0; i < a.sLocation.Count; i++)
                {
                    sLocation f = a.sLocation.ElementAt(i);
                    bl_A.Add(f.Big_Location);
                }
            }
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_B.Add(f.Big_Location);
                }
            }

            foreach (var bl in bl_A)
            {
                bl.sLocation = sort_big_local(bl);
                sKoord_point f = bl.sKoord_point1;
                sl_A = ((List<sLocation>)bl.sLocation);
                sLocation i = bl.sLocation.Where(o => o.sKoord_point == a).FirstOrDefault();
                double? l = 0;
                double? t = 0;
                for (int j = sl_A.IndexOf(i); j < sl_A.Count; j++) 
                {
                    l = l + sl_A.ElementAt(j).length;
                    time.Stop();
                    t += math_average_time_loc_dijkstra(tp.timeString,null,sl_A.ElementAt(j));
                    time.Start();
                }
                dijkstra_point p = new dijkstra_point();
                dijkstra_point start = new dijkstra_point();
                start.point_in = a;
                start.length = 0;
                start.t = 0;
                p.point_out = start;
                p.point_in = f;
                p.length = l;
                p.t = t;
                p.visit = false;
                point_list.Add(p);
                point_visit.Add(start);
            }

            do
            {
                dijkstra_point start = new dijkstra_point();
                double? l = Double.PositiveInfinity;
                double? t = Double.PositiveInfinity;
                //находим непосещенную вершину с минимальным расстоянием
                foreach (var p in point_list)
                {
                    if (p.t < t && p.visit == false) { l = p.length; t = p.t; start = p; }
                }

                //заполняем лист с большими локациями, начинающимися из этой точки
                List<Big_Location> BB = new List<Big_Location>();
                foreach (var bl in be.Big_Location) { if (bl.id_koord1 == start.point_in.id) BB.Add(bl); }

                if (BB.Where(o => o.sKoord_point == bl_B.ElementAt(0).sKoord_point).FirstOrDefault() != null) break;

                foreach (var bl in BB)
                {
                    dijkstra_point input = point_list.Where(o => o.point_in.id == bl.id_koord2).FirstOrDefault();
                    time.Stop();
                    double tt = math_average_time_loc_dijkstra(tp.timeString,bl,null);
                    time.Start();
                    if (input != null)
                    {
                        if (input.t > t + tt) { input.point_out = start; input.length = l + bl.length; input.t = t + tt; }
                    }
                    else
                    {
                        dijkstra_point p = new dijkstra_point();
                        p.point_out = start;
                        p.point_in = bl.sKoord_point1;
                        p.length = l + bl.length;
                        p.t = t + tt;
                        p.visit = false;
                        point_list.Add(p);
                    }
                }
                point_visit.Add(start);
                foreach (var p in point_list)
                {
                    if (p == start) { p.visit = true; }
                }

            } while (point_list.Where(o => o.point_in == bl_B.ElementAt(0).sKoord_point) != null);

            time.Stop();

            sKoord_point asas = bl_B.ElementAt(0).sKoord_point;

            List<sLocation> z = sort_big_local(bl_B.ElementAt(0));
            wr.name_algoritm = "Дейкстры по времени";
            wr.t = tp.timeString;
            print_way_dijkstra_time(point_list, asas, wr, z);

            math_time_motion(wr, tp);
            TimeSpan ts = time.Elapsed;
            wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();

            wr.colour = wr.way.ElementAt(0).Color;
            w_repos.Add(wr);
            window_report.review(w_repos);   
        }

        public void print_way_dijkstra(List<dijkstra_point> point_list, sKoord_point asas, way_repos wr, List<sLocation> z)
        {
            double? length_way = 0;
            bool fl = false;
            do
            {
                foreach (var bl in point_list)
                {
                    if (bl.point_in == asas)
                    {
                        if (bl.point_out.point_in == wr.a)
                        {
                            List<Big_Location> bl_a = bl.point_in.Big_Location1.ToList();
                            for (int i = 0; i < bl_a.Count; i++)
                            {
                                Big_Location bla = bl_a[i];
                                List <sLocation> sla = sort_big_local(bla);
                                if (sla.Where(o => o.sKoord_point == wr.a).FirstOrDefault() != null)
                                {
                                    for (int j = sla.Count - 1; j > -1; j--)
                                    {
                                        MyLocation ml = new MyLocation();
                                        ml.PointA = new MyPoint() { Latitude = sla[j].sKoord_point.Latitude.Value + 0.0003, Longitude = sla[j].sKoord_point.Longitude.Value + 0.0003, id = sla[j].Id_point1 };
                                        ml.PointB = new MyPoint() { Latitude = sla[j].sKoord_point1.Latitude.Value + 0.0003, Longitude = sla[j].sKoord_point1.Longitude.Value + 0.0003, id = sla[j].Id_point2 };
                                        ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0097A7"));
                                        ml.id_loc = sla[j].Id;
                                        map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                        wr.way.Add(ml);
                                        if (sla[j].sKoord_point == wr.a) { 
                                            fl = true; 
                                            break; 
                                        }
                                    }
                                }
                                break;
                            }
                            break;
                        }
                        Big_Location bbl = be.Big_Location.Where(o => o.id_koord2 == bl.point_in.id && o.id_koord1 == bl.point_out.point_in.id).FirstOrDefault();
                        if (bbl != null)
                        {
                            List<sLocation> sl = sort_big_local(bbl);
                            foreach (var s in sl)
                            {
                                MyLocation ml = new MyLocation();
                                ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value + 0.0003, Longitude = s.sKoord_point.Longitude.Value + 0.0003, id = s.Id_point1 };
                                ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value + 0.0003, Longitude = s.sKoord_point1.Longitude.Value + 0.0003, id = s.Id_point2 };
                                ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0097A7"));
                                ml.id_loc = s.Id;
                                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                wr.way.Add(ml);
                            }                           
                        }
                        asas = bl.point_out.point_in;
                        if (length_way < bl.length) length_way = bl.length;
                        break;
                    }                   
                }
            } while ( fl==false);

            int k = 0;
            foreach (var f in z)
            {
                if (f.sKoord_point1 == wr.b) k = z.IndexOf(f);
            }
            for (int j = 0; j <= k; j++)
            {
                MyLocation ml = new MyLocation();
                ml.PointA = new MyPoint() { Latitude = z[j].sKoord_point.Latitude.Value + 0.0003, Longitude = z[j].sKoord_point.Longitude.Value + 0.0003, id = z[j].Id_point1 };
                ml.PointB = new MyPoint() { Latitude = z[j].sKoord_point1.Latitude.Value + 0.0003, Longitude = z[j].sKoord_point1.Longitude.Value + 0.0003, id = z[j].Id_point2 };
                ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#0097A7"));
                ml.id_loc = z[j].Id;
                length_way += (double)z[j].length;
                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                wr.way.Add(ml);
            }

            wr.length = Math.Round((double)length_way);

            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;
        }

        public void print_way_dijkstra_time(List<dijkstra_point> point_list, sKoord_point asas, way_repos wr, List<sLocation> z)
        {
            double? length_way = 0;
            double? time_way = 0;
            bool fl = false;
            do
            {
                foreach (var bl in point_list)
                {
                    if (bl.point_in == asas)
                    {
                        if (bl.point_out.point_in == wr.a)
                        {
                            List<Big_Location> bl_a = bl.point_in.Big_Location1.ToList();
                            for (int i = 0; i < bl_a.Count; i++)
                            {
                                Big_Location bla = bl_a[i];
                                List<sLocation> sla = sort_big_local(bla);
                                if (sla.Where(o => o.sKoord_point == wr.a).FirstOrDefault() != null)
                                {
                                    for (int j = sla.Count - 1; j > -1; j--)
                                    {
                                        MyLocation ml = new MyLocation();
                                        ml.PointA = new MyPoint() { Latitude = sla[j].sKoord_point.Latitude.Value + 0.0003, Longitude = sla[j].sKoord_point.Longitude.Value + 0.0003, id = sla[j].Id_point1 };
                                        ml.PointB = new MyPoint() { Latitude = sla[j].sKoord_point1.Latitude.Value + 0.0003, Longitude = sla[j].sKoord_point1.Longitude.Value + 0.0003, id = sla[j].Id_point2 };
                                        ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#C51162"));
                                        ml.id_loc = sla[j].Id;
                                        map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                        wr.way.Add(ml);
                                        if (sla[j].sKoord_point == wr.a)
                                        {
                                            fl = true;
                                            break;
                                        }
                                    }
                                    if (time_way < bl.t) { length_way = bl.length; time_way = bl.t; };
                                }
                                break;
                            }
                            break;
                        }
                        Big_Location bbl = be.Big_Location.Where(o => o.id_koord2 == bl.point_in.id && o.id_koord1 == bl.point_out.point_in.id).FirstOrDefault();
                        if (bbl != null)
                        {
                            List<sLocation> sl = sort_big_local(bbl);
                            foreach (var s in sl)
                            {
                                MyLocation ml = new MyLocation();
                                ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value + 0.0003, Longitude = s.sKoord_point.Longitude.Value + 0.0003, id = s.Id_point1 };
                                ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value + 0.0003, Longitude = s.sKoord_point1.Longitude.Value + 0.0003, id = s.Id_point2 };
                                ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#C51162"));
                                ml.id_loc = s.Id;
                                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                wr.way.Add(ml);
                            }

                        }
                        asas = bl.point_out.point_in;
                        if (time_way < bl.t) { length_way = bl.length; time_way = bl.t; };
                        break;
                    }
                }
            } while (fl == false);

            int k = 0;
            foreach (var f in z)
            {
                if (f.sKoord_point1 == wr.b) k = z.IndexOf(f);
            }
            for (int j = 0; j <= k; j++)
            {
                MyLocation ml = new MyLocation();
                ml.PointA = new MyPoint() { Latitude = z[j].sKoord_point.Latitude.Value + 0.0003, Longitude = z[j].sKoord_point.Longitude.Value + 0.0003, id = z[j].Id_point1 };
                ml.PointB = new MyPoint() { Latitude = z[j].sKoord_point1.Latitude.Value + 0.0003, Longitude = z[j].sKoord_point1.Longitude.Value + 0.0003, id = z[j].Id_point2 };
                ml.Color = (SolidColorBrush)(new BrushConverter().ConvertFrom("#C51162"));
                ml.id_loc = z[j].Id;
                length_way += (double)z[j].length;
                time_way+=math_average_time_loc_dijkstra(wr.t,null,z[j]);
                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                wr.way.Add(ml);
            }

            wr.length = Math.Round((double)length_way);

            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;

            wr.time_motion = TimeSpan.FromSeconds(Math.Floor((double)time_way)).ToString();
        }

        public double math_average_time_loc_dijkstra(String tp,Big_Location bl, sLocation sl)
        {
            double Time = My.GetTimeDouble(DateTime.Parse(tp));
            int TypeDay = select_type_day;
            double AvgTime = 0;
            List<sLocation> bloc = new List<sLocation>();
            if (bl == null)
            {
                bloc.Add(sl);
            }
            else
            {
                bloc = sort_big_local(bl);
            }
            

            for (int i = 0; i < bloc.Count; i++)
            {
                MyTimeLocation TempLoc = new MyTimeLocation();
                TempLoc.IdLocation = bloc[i].Id;
                
                //запрос о времени прохождения на текущие время из БЗ дня, недели, года
                double dtYear = 0;
                double srZnach = 0;

                dtYear = bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                dtYear = (dtYear == 0) ? bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;

                if (dtYear > 0)
                {
                    /*!!!!*/
                    var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                    srZnach = ((koeff != 0) ? koeff : 1) *  dtYear;
                    //запись в лист
                    TempLoc.TimeLocation = srZnach;

                    //сдвиг времени на значение среднего
                    //сдвигаемся на сколько то секунд и определяем номер текущего среза
                    //Time += srZnach / (60.0 * 10);
                    //определение средней скорости
                    sLocation select_location = bslocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                    AvgTime += TempLoc.TimeLocation;
                }
                else
                {
                    AvgTime += srZnach;
                    //Time += srZnach / (60.0 * 10);
                }
            }
            return AvgTime;
        }

        public class dijkstra_point
        {
            public dijkstra_point point_out;
            public sKoord_point point_in;
            public double? length;
            public double? t;
            public bool visit;

            public void set_point_out(dijkstra_point po)
            {
                this.point_out = po;
            }
        }
                     
        #endregion

        #region А*
        private void Button_algoritm_a(object sender, RoutedEventArgs e)
        {
            select = 779;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_a_by_point(pp1, pp2);
            }
        }

        private void Button_algoritm_a_time(object sender, RoutedEventArgs e)
        {
            select = 780;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_a_time(pp1, pp2);
            }
        }

        private void short_way_a_by_point(sKoord_point a, sKoord_point b)
        {
            //ожидающие рассмотрения
            List<a_point> wait_point = new List<a_point>();
            //рассмотренные
            List<a_point> check_point = new List<a_point>();

            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            Stopwatch time = new Stopwatch();
            time.Start();

            List<Big_Location> bl_B = new List<Big_Location>();
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_B.Add(f.Big_Location);
                }
            }

            a_point start = new a_point();
            start.point_this = a;
            start.path_from_start = 0;
            start.path_to_finish = math_path_vector(a, b);
            wait_point.Add(start);
            bool first = true;

            while (wait_point.Count > 0)
            {
                a_point current = wait_point.OrderBy(point => point.path_total).First();
                bool f = false;
                foreach(var loc in bl_B)
                {
                    if (loc.sKoord_point == current.point_this)
                    {
                        f = true;
                        a_point final = new a_point();
                        final.set_point_out(current);
                        final.point_this = b;
                        final.path_from_start = Math.Round((double)math_total_path(loc, b, current));
                        time.Stop();
                        TimeSpan ts = time.Elapsed;
                        wr.name_algoritm = "A*";
                        wr.length = final.path_from_start;
                        wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                        print_a(final, wr, (SolidColorBrush)(new BrushConverter().ConvertFrom("#D81B60")));
                        if (tp != null)
                        {
                            wr.t = tp.timeString;
                            math_time_motion(wr, tp);
                        }
                        wr.colour = wr.way.ElementAt(0).Color;
                        w_repos.Add(wr);
                        window_report.review(w_repos);
                        break;
                    }
                }
                if (f) break;

                wait_point.Remove(current);
                check_point.Add(current);

                foreach (var neighbour in getNeighbours(current, b, first))
                {
                    if (check_point.Count(node => node.point_this == neighbour.point_this) > 0)
                        continue;
                    var wp = wait_point.FirstOrDefault(node => node.point_this == neighbour.point_this);
                    if (wp == null) wait_point.Add(neighbour);
                    else
                    {
                        if (wp.path_total > neighbour.path_total)
                        {
                            wp.set_point_out(current);
                            wp.path_from_start = neighbour.path_from_start;
                        }
                    }

                }

                first = false;
            }

        }

        public Stopwatch time = new Stopwatch();

        private void short_way_a_time(sKoord_point a, sKoord_point b)
        {
            //ожидающие рассмотрения
            List<a_point> wait_point = new List<a_point>();
            //рассмотренные
            List<a_point> check_point = new List<a_point>();

            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            TimePoint tp = (TimePoint)cbTime.SelectedItem;

            
            time.Start();

            List<Big_Location> bl_B = new List<Big_Location>();
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_B.Add(f.Big_Location);
                }
            }

            a_point start = new a_point();
            start.point_this = a;
            start.path_from_start = 0;
            start.t = 0;
            start.path_to_finish = math_path_vector(a, b);
            wait_point.Add(start);
            bool first = true;

            while (wait_point.Count > 0)
            {
                a_point current = wait_point.OrderBy(point => point.time_total).First();
                bool f = false;
                foreach (var loc in bl_B)
                {
                    if (loc.sKoord_point == current.point_this)
                    {
                        f = true;
                        a_point final = new a_point();
                        final.set_point_out(current);
                        final.point_this = b;
                        time.Stop();
                        final.t = math_average_time_loc_a(tp.timeString,loc,null);
                        time.Start();
                        final.path_from_start = Math.Round((double)math_total_path(loc, b, current));
                        time.Stop();
                        TimeSpan ts = time.Elapsed;
                        wr.name_algoritm = "A* Время";
                        wr.length = final.path_from_start;
                        wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                        print_a(final, wr, (SolidColorBrush)(new BrushConverter().ConvertFrom("#3F51B5")));
                        if (tp != null)
                        {
                            wr.t = tp.timeString;
                            math_time_motion(wr, tp);
                        }
                        wr.colour = wr.way.ElementAt(0).Color;
                        w_repos.Add(wr);
                        window_report.review(w_repos);
                        break;
                    }
                }
                if (f) break;

                wait_point.Remove(current);
                check_point.Add(current);

                foreach (var neighbour in getNeighboursTime(current, b, first, tp.timeString))
                {
                    if (check_point.Count(node => node.point_this == neighbour.point_this) > 0)
                        continue;
                    var wp = wait_point.FirstOrDefault(node => node.point_this == neighbour.point_this);
                    if (wp == null) wait_point.Add(neighbour);
                    else
                    {
                        if (wp.time_total > neighbour.time_total)
                        {
                            wp.set_point_out(current);
                            wp.path_from_start = neighbour.path_from_start;
                            wp.t = neighbour.t;
                        }
                    }

                }

                first = false;
            }
        }

        private double math_path_vector(sKoord_point a, sKoord_point b)
        {
            return My.distance(a.Longitude.Value, a.Latitude.Value, b.Longitude.Value, b.Latitude.Value);
        }

        private List<a_point> getNeighbours(a_point current, sKoord_point b, bool first)
        {
            List<a_point> neighbours = new List<a_point>();

            foreach (var ss in current.point_this.sLocation)
            {
                a_point p = new a_point();
                p.set_point_out(current);
                p.point_this = ss.Big_Location.sKoord_point1;
                p.path_to_finish = math_path_vector(p.point_this, b);

                if (first)
                {
                    List<sLocation> sl = sort_big_local(ss.Big_Location);
                    for (int i = 0; i < sl.Count; i++)
                    {
                        if (sl[i].sKoord_point == current.point_this)
                        {
                            for (int j = i; j < sl.Count; j++)
                            {
                                p.path_from_start += sl[j].length.Value;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    p.path_from_start = current.path_from_start + ss.Big_Location.length;
                }

                neighbours.Add(p);
            }

            return neighbours;
        }

        public double math_time_to_f(a_point from, a_point to)
        {
            double avg_speed = from.path_total/from.t;
            double time_to_fin = to.path_to_finish / avg_speed;
            return time_to_fin;

        }

        private List<a_point> getNeighboursTime(a_point current, sKoord_point b, bool first, string tp)
        {
            List<a_point> neighbours=new List<a_point>();

            foreach (var ss in current.point_this.sLocation)
            {
                a_point p = new a_point();
                p.set_point_out(current);
                p.point_this = ss.Big_Location.sKoord_point1;
                p.path_to_finish = math_path_vector(p.point_this, b);
                p.t_to_finish = math_time_to_f(p.point_out, p);

                if (first)
                {
                    List<sLocation> sl = sort_big_local(ss.Big_Location);
                    for (int i = 0; i < sl.Count; i++)
                    {
                        if (sl[i].sKoord_point == current.point_this)
                        {
                            for (int j = i; j < sl.Count; j++)
                            {
                                p.path_from_start += sl[j].length.Value;
                                time.Stop();
                                p.t += math_average_time_loc_a(tp, null, sl[j]);
                                time.Start();
                            }
                            break;
                        }
                    }
                }
                else
                {
                    p.path_from_start = current.path_from_start + ss.Big_Location.length;
                    time.Stop();
                    p.t = current.t + math_average_time_loc_a(tp, ss.Big_Location, null);
                    time.Start();
                }

                neighbours.Add(p);
            }

            return neighbours;
        }

        private double math_total_path(Big_Location bl, sKoord_point b, a_point came)
        {
            double path=came.path_from_start;
            List<sLocation> sl = sort_big_local(bl);
            for (int i = 0; i < sl.Count; i++)
            {
                if (sl[i].sKoord_point1 == b)
                {
                    for (int j = 0; j < i + 1; j++)
                    {
                        path += sl[j].length.Value;
                    }
                }
            }
            return path;
        }

        public double math_average_time_loc_a(String tp, Big_Location bl, sLocation sl)
        {
            double Time = My.GetTimeDouble(DateTime.Parse(tp));
            int TypeDay = select_type_day;
            double AvgTime = 0;
            List<sLocation> bloc = new List<sLocation>();
            if (bl == null)
            {
                bloc.Add(sl);
            }
            else
            {
                bloc = sort_big_local(bl);
            }


            for (int i = 0; i < bloc.Count; i++)
            {
                MyTimeLocation TempLoc = new MyTimeLocation();
                TempLoc.IdLocation = bloc[i].Id;

                //запрос о времени прохождения на текущие время из БЗ дня, недели, года

                double dtYear = 0;
                double srZnach = 0;

                dtYear = bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                dtYear = (dtYear == 0) ? bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;

                if ( dtYear > 0)
                {
                    /*!!!!*/
                    var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                    srZnach = ((koeff != 0) ? koeff : 1) * dtYear;
                    //запись в лист
                    TempLoc.TimeLocation = srZnach;

                    //сдвиг времени на значение среднего
                    //сдвигаемся на сколько то секунд и определяем номер текущего среза
                    //Time += srZnach / (60.0 * 10);
                    //определение средней скорости
                    sLocation select_location = bslocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                    AvgTime += TempLoc.TimeLocation;
                }
                else
                {
                    AvgTime += srZnach;
                   // Time += srZnach / (60.0 * 10);
                }
            }
            return AvgTime;
        }

        private void print_a(a_point final, way_repos wr, SolidColorBrush color)
        {
            a_point f = new a_point();
            f = final;

            //для последней
            List<Big_Location> bl = be.Big_Location.Where(o => o.id_koord1 == f.point_out.point_this.id).ToList();
            foreach (var b in bl)
            {
                if (b.sLocation.Where(o => o.Id_point2 == f.point_this.id).ToList().Count > 0)
                {
                    List<sLocation> sl = sort_big_local(b);
                    foreach (var s in sl)
                    {
                        if (s.Id_point1 == f.point_this.id) break;
                        else
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value + 0.00015, Longitude = s.sKoord_point.Longitude.Value + 0.00015, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value + 0.00015, Longitude = s.sKoord_point1.Longitude.Value + 0.00015, id = s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                    f = f.point_out;
                    break;
                }
            }

            while (f.point_out != null)
            {
                if (f.point_out.point_this == wr.a)
                {
                    List<Big_Location> bl_a = f.point_this.Big_Location1.ToList();
                    for (int i = 0; i < bl_a.Count; i++)
                    {
                        Big_Location bla = bl_a[i];
                        List<sLocation> sla = sort_big_local(bla);
                        if (sla.Where(o => o.sKoord_point == wr.a).FirstOrDefault() != null)
                        {
                            for (int j = sla.Count - 1; j > -1; j--)
                            {
                                MyLocation ml = new MyLocation();
                                ml.PointA = new MyPoint() { Latitude = sla[j].sKoord_point.Latitude.Value + 0.00015, Longitude = sla[j].sKoord_point.Longitude.Value + 0.00015, id = sla[j].Id_point1 };
                                ml.PointB = new MyPoint() { Latitude = sla[j].sKoord_point1.Latitude.Value + 0.00015, Longitude = sla[j].sKoord_point1.Longitude.Value + 0.00015, id = sla[j].Id_point2 };
                                ml.Color = color;
                                ml.id_loc = sla[j].Id;
                                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                wr.way.Add(ml);
                                if (sla[j].sKoord_point == wr.a)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    break;
                }
                else
                {
                    Big_Location bbl = be.Big_Location.Where(o => o.id_koord2 == f.point_this.id && o.id_koord1 == f.point_out.point_this.id).FirstOrDefault();
                    if (bbl != null)
                    {
                        List<sLocation> sl = sort_big_local(bbl);
                        foreach (var s in sl)
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value + 0.00015, Longitude = s.sKoord_point.Longitude.Value + 0.00015, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value + 0.00015, Longitude = s.sKoord_point1.Longitude.Value + 0.00015, id = s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                }
                f = f.point_out;
            }
            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;
        }
        
        public class a_point
        {
            public sKoord_point point_this;
            public a_point point_out;
            public double path_from_start;
            public double path_to_finish;
            public double t;
            public double t_to_finish;
            public double path_total
            {
                get
                {
                    return this.path_from_start + this.path_to_finish;
                }
            }

            public double time_total
            {
                get { return this.t + this.t_to_finish; }
            }

            public void set_point_out(a_point po)
            {
                this.point_out = po;
            }
        }
        #endregion

        #region В глубину
        private void Button_algoritm_depth(object sender, RoutedEventArgs e)
        {
            select = 200;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_dfs(pp1, pp2);
            }
        }

        public void short_way_dfs(sKoord_point a, sKoord_point b)
        {
            Stopwatch time = new Stopwatch();
            time.Start();
            Stack<point_dfs> order = new Stack<point_dfs>();//ожидающие
            List<point_dfs> swing = new List<point_dfs>();//рассмотренные
            TimePoint tp = (TimePoint)cbTime.SelectedItem;
            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            point_dfs start = new point_dfs();
            start.point_this = a;
            order.Push(start);

            List<Big_Location> bl_b = new List<Big_Location>();
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_b.Add(f.Big_Location);
                }
            }


            if (a.sLocation.Count != 0)
            {
                for (int i = 0; i < a.sLocation.Count; i++)
                {
                    sLocation f = a.sLocation.ElementAt(i);
                    double l = 0;
                    List<sLocation> sort = sort_big_local(f.Big_Location);
                    for (int j = 0; j < sort.Count; j++)
                    {
                        if (sort[j].sKoord_point == a)
                        {
                            for (int k = j; k < sort.Count; k++)
                            {
                                l += sort[k].length.Value;
                            }
                            break;
                        }
                    }
                    point_dfs temp = new point_dfs();
                    temp.length = l;
                    temp.set_point_out(start);
                    temp.point_this = f.Big_Location.sKoord_point1;
                    order.Push(temp);
                }
            }

            bool flag = false;

            //пока есть в очереди
            while (order.Count > 0)
            {
                point_dfs temp = new point_dfs();
                temp = order.Pop();
                swing.Add(temp);

                //если точка является одной из возможных конечных
                var ss = bl_b.Where(o => o.sKoord_point == temp.point_this).ToList();
                if (ss.Count != 0) { flag = true; 
                    break; }
                else
                {
                    //если не тупиковая
                    if (temp.point_this.Big_Location.Count != 0)
                    {
                        //перебираем все смежные большие локации
                        for (int i = 0; i < temp.point_this.Big_Location.Count; i++)
                        {
                            //если нет в очереди и в проверенных
                            if (order.Where(o => o.point_this == temp.point_this.Big_Location.ElementAt(i).sKoord_point1).ToList().Count == 0 &&
                                swing.Where(o => o.point_this == temp.point_this.Big_Location.ElementAt(i).sKoord_point1).ToList().Count == 0)
                            {
                                //тогда создаем точку и помещаем в очередь
                                point_dfs tmp = new point_dfs();
                                tmp.set_point_out(temp);
                                tmp.point_this = temp.point_this.Big_Location.ElementAt(i).sKoord_point1;
                                tmp.length = temp.length + temp.point_this.Big_Location.ElementAt(i).length;
                                order.Push(tmp);
                            }
                        }
                    }
                }
            }

            time.Stop();
            if (flag == false)
            {
                MessageBox.Show("ничего не найдено");
            }
            else
            {
                wr.name_algoritm = "В глубину";

                TimeSpan ts = time.Elapsed;
                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                point_dfs final = new point_dfs();
                final.set_point_out(swing.Last());
                final.point_this = b;
                final.length = final.point_out.length;
                List<sLocation> sort = new List<sLocation>();
                List<Big_Location> bl = bl_b.Where(o => o.sKoord_point == final.point_out.point_this).ToList();
                sort = sort_big_local(bl[0]);

                for (int i = 0; i < sort.Count; i++)
                {
                    if (sort[i].sKoord_point1 == b)
                    {
                        for (int j = 0; j < i + 1; j++)
                        {
                            final.length += sort[j].length.Value;
                        }
                    }
                }

                wr.length = Math.Round((double)final.length);
                print_dfs_way(final, wr, (SolidColorBrush)(new BrushConverter().ConvertFrom("#00C853")));
                if (tp != null)
                {
                    wr.t = tp.timeString;
                    math_time_motion(wr, tp);
                }
                wr.colour = wr.way.ElementAt(0).Color;
                w_repos.Add(wr);
                window_report.review(w_repos);
            }
        }

        public void print_dfs_way(point_dfs final, way_repos wr, SolidColorBrush color)
        {
            point_dfs f = new point_dfs();
            f = final;

            //для последней
            List<Big_Location> bl = be.Big_Location.Where(o => o.id_koord1 == f.point_out.point_this.id).ToList();
            foreach (var b in bl)
            {
                if (b.sLocation.Where(o => o.Id_point2 == f.point_this.id).ToList().Count > 0)
                {
                    List<sLocation> sl = sort_big_local(b);
                    foreach (var s in sl)
                    {
                        if (s.Id_point1 == f.point_this.id) break;
                        else
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value, Longitude = s.sKoord_point.Longitude.Value, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value, Longitude = s.sKoord_point1.Longitude.Value, id = s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                    f = f.point_out;
                    break;
                }
            }

            while (f.point_out != null)
            {
                if (f.point_out.point_this == wr.a)
                {
                    List<Big_Location> bl_a = f.point_this.Big_Location1.ToList();
                    for (int i = 0; i < bl_a.Count; i++)
                    {
                        Big_Location bla = bl_a[i];
                        List<sLocation> sla = sort_big_local(bla);
                        if (sla.Where(o => o.sKoord_point == wr.a).FirstOrDefault() != null)
                        {
                            for (int j = sla.Count - 1; j > -1; j--)
                            {
                                MyLocation ml = new MyLocation();
                                ml.PointA = new MyPoint() { Latitude = sla[j].sKoord_point.Latitude.Value, Longitude = sla[j].sKoord_point.Longitude.Value, id = sla[j].Id_point1 };
                                ml.PointB = new MyPoint() { Latitude = sla[j].sKoord_point1.Latitude.Value, Longitude = sla[j].sKoord_point1.Longitude.Value, id = sla[j].Id_point2 };
                                ml.Color = color;
                                ml.id_loc = sla[j].Id;
                                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                wr.way.Add(ml);
                                if (sla[j].sKoord_point == wr.a)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    break;
                }
                else
                {
                    Big_Location bbl = be.Big_Location.Where(o => o.id_koord2 == f.point_this.id && o.id_koord1 == f.point_out.point_this.id).FirstOrDefault();
                    if (bbl != null)
                    {
                        List<sLocation> sl = sort_big_local(bbl);
                        foreach (var s in sl)
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value, Longitude = s.sKoord_point.Longitude.Value, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value, Longitude = s.sKoord_point1.Longitude.Value, id=s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                }
                f = f.point_out;
            }

            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;
        }
        //public point_dfs final=new point_dfs();

        /*
        private void short_way_dfs(sKoord_point a, sKoord_point b)
        {
            Stopwatch time = new Stopwatch();
            time.Start();

            way_repos wr = new way_repos();
            wr.a=a;
            wr.b=b;
            wr.name_algoritm = "В глубину";

            point_dfs start = new point_dfs();
            start.point_this = a;
            start.status = 1;

            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    blb_dfs.Add(f.Big_Location);
                }
            }

            if (a.sLocation.Count != 0)
            {
                for (int i = 0; i < a.sLocation.Count; i++)
                {
                    sLocation f = a.sLocation.ElementAt(i);
                    double l = 0;
                    List<sLocation> sort = sort_big_local(f.Big_Location);
                    for (int j = 0; j < sort.Count; j++)
                    {
                        if (sort[j].sKoord_point == a)
                        {
                            for (int k = j; k < sort.Count; k++)
                            {
                                l += sort[k].length.Value;
                            }
                            break;
                        }
                    }
                    point_dfs temp = new point_dfs();
                    temp.length = l;
                    temp.set_point_out(start);
                    temp.point_this = f.Big_Location.sKoord_point1;
                    temp.status = 0;
                    start.child_points.Enqueue(temp);
                }
            }

            point_dfs mp = new point_dfs();
            point_dfs p = new point_dfs();
            mp = start;

            while (mp.child_points.Count > 0)
            {
                dfs(mp);
            }

            time.Stop();

            if (final.length != 0)
            {
                TimeSpan ts = time.Elapsed;
                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                print_way_dfs(final, wr);
            }

        }

        public void print_way_dfs(point_dfs w, way_repos wr)
        {
            point_dfs tmp = new point_dfs();
            tmp = w;
            while (tmp.point_out != null)
            {
                MyLocation ml = new MyLocation();
                ml.PointB = new MyPoint() { Latitude = tmp.point_this.Latitude.Value, Longitude = tmp.point_this.Longitude.Value };
                ml.PointA = new MyPoint() { Latitude = tmp.point_out.point_this.Latitude.Value, Longitude = tmp.point_out.point_this.Longitude.Value };

                ml.Color = Brushes.Cyan;
                map1.Children.Add(PrintLocationOnMap(ml));
                wr.way.Add(ml);
                tmp = tmp.point_out;
            }
        }

        point_dfs tmp = new point_dfs();
        List<Big_Location> blb_dfs=new List<Big_Location>();
        public void dfs(point_dfs p_temp)
        {
            if (blb_dfs.Where(o => o.sKoord_point == p_temp.point_this).ToList().Count() != 0)
            { 
                p_temp.status = 5;
                final = p_temp;
                return; }
            if (p_temp.status == 2)
            {  
                return;  }
            if (p_temp.status == 1)
            {
                if (p_temp.child_points.Count() > 0) 
                {
                    while (p_temp.child_points.Count() > 0)
                    {
                        dfs(p_temp.child_points.Dequeue());
                    }
                }
                else { p_temp.status = 2; 
                    return; }
            }
            if (p_temp.status == 0)
            {
                //если не тупиковая
                if (p_temp.point_this.Big_Location.Count != 0)
                {
                    p_temp.status = 1;//открывем вершину
                    //перебираем все смежные большие локации и добавляем новых детей
                    for (int i = 0; i < p_temp.point_this.Big_Location.Count; i++)
                    {
                        point_dfs tmp = new point_dfs();
                        tmp.set_point_out(p_temp);
                        tmp.point_this = p_temp.point_this.Big_Location.ElementAt(i).sKoord_point1;
                        tmp.length = p_temp.length + p_temp.point_this.Big_Location.ElementAt(i).length;
                        tmp.status = 0;
                        p_temp.child_points.Enqueue(tmp);
                    }
                    while (p_temp.child_points.Count > 0)
                    {
                        dfs(p_temp.child_points.Dequeue());//назначаем текущей вершиной первую из очереди детей
                    }
                }
                 //если тупик-закрываем вершину и возвращаемся на шаг назад
                else { p_temp.status = 2;
                    return; }
            }
        }
        */


        public class point_dfs
        {
            public sKoord_point point_this;
            public double length;
            public point_dfs point_out;
            public void set_point_out(point_dfs po)
            { this.point_out = po; }
        }
        #endregion 

        #region В ширину
        private void Button_algoritm_width(object sender, RoutedEventArgs e)
        {
            select = 300;
            if (p1 != null && p2 != null)
            {
                sKoord_point pp1 = ((sKoord_point)(p1.Tag));
                sKoord_point pp2 = ((sKoord_point)(p2.Tag));
                short_way_width(pp1, pp2);
            }
        }

        private void short_way_width(sKoord_point a, sKoord_point b)
        {
            Stopwatch time = new Stopwatch();
            time.Start();            
            Queue<point_width> order = new Queue<point_width>();//ожидающие
            List<point_width> swing = new List<point_width>();//рассмотренные
            TimePoint tp = (TimePoint)cbTime.SelectedItem;
            way_repos wr = new way_repos();
            wr.a = a;
            wr.b = b;

            point_width start = new point_width();
            start.point_this = a;
            order.Enqueue(start);

            List<Big_Location> bl_b = new List<Big_Location>();
            if (b.sLocation1.Count != 0)
            {
                for (int i = 0; i < b.sLocation1.Count; i++)
                {
                    sLocation f = b.sLocation1.ElementAt(i);
                    bl_b.Add(f.Big_Location);
                }
            }


            if (a.sLocation.Count != 0)
            {       
                for (int i = 0; i<a.sLocation.Count;i++)
                {
                    sLocation f = a.sLocation.ElementAt(i);
                    double l = 0;
                    List<sLocation> sort = sort_big_local(f.Big_Location);
                    for (int j = 0; j < sort.Count; j++)
                    {
                        if (sort[j].sKoord_point == a)
                        {
                            for (int k = j; k < sort.Count; k++)
                            {
                                l += sort[k].length.Value;
                            }
                            break;
                        }
                    }
                    point_width temp = new point_width();
                    temp.length = l;
                    temp.set_point_out(start);
                    temp.point_this = f.Big_Location.sKoord_point1;
                    order.Enqueue(temp);
                }
            }

            bool flag = false;

            //пока есть в очереди
            while (order.Count > 0)
            {
                point_width temp = new point_width();
                temp = order.Dequeue();
                swing.Add(temp);

                //если точка является одной из возможных конечных
                var ss = bl_b.Where(o => o.sKoord_point == temp.point_this).ToList();
                if (ss.Count!=0) { flag = true; break; }
                else
                {
                    //если не тупиковая
                    if (temp.point_this.Big_Location.Count != 0)
                    {
                        //перебираем все смежные большие локации
                        for (int i = 0; i < temp.point_this.Big_Location.Count; i++)
                        {
                            //если нет в очереди и в проверенных
                            if (order.Where(o => o.point_this == temp.point_this.Big_Location.ElementAt(i).sKoord_point1).ToList().Count == 0 &&
                                swing.Where(o => o.point_this == temp.point_this.Big_Location.ElementAt(i).sKoord_point1).ToList().Count==0)
                            {
                                //тогда создаем точку и помещаем в очередь
                                point_width tmp = new point_width();
                                tmp.set_point_out(temp);
                                tmp.point_this = temp.point_this.Big_Location.ElementAt(i).sKoord_point1;
                                tmp.length = temp.length + temp.point_this.Big_Location.ElementAt(i).length;
                                order.Enqueue(tmp);
                            }
                        }
                    }
                }
            }

            time.Stop();
            if (flag == false)
            {
                MessageBox.Show("ничего не найдено");
            }
            else 
            {
                wr.name_algoritm = "В ширину";
                TimeSpan ts = time.Elapsed;
                wr.time_work = TimeSpan.FromSeconds(Math.Round(ts.TotalSeconds)).ToString();
                point_width final = new point_width();
                final.set_point_out(swing.Last());
                final.point_this = b;
                final.length = final.point_out.length;
                List<sLocation> sort = new List<sLocation>();
                List<Big_Location> bl = bl_b.Where(o => o.sKoord_point == final.point_out.point_this).ToList();
                sort = sort_big_local(bl[0]);

                for (int i = 0; i < sort.Count; i++)
                {
                    if (sort[i].sKoord_point1 == b)
                    {
                        for (int j = 0; j < i + 1; j++)
                        {
                            final.length += sort[j].length.Value;
                        }
                    }
                }

                wr.length = Math.Round((double)final.length);
                print_width_way(final, wr, (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFB300")));
                if (tp != null)
                {
                    wr.t = tp.timeString;
                    math_time_motion(wr, tp);
                }
                wr.colour = wr.way.ElementAt(0).Color;
                w_repos.Add(wr);
                window_report.review(w_repos);
            }
        }

        public void print_width_way(point_width final, way_repos wr, SolidColorBrush color)
        {
            point_width f = new point_width();
            f = final;

            //для последней
            List<Big_Location> bl = be.Big_Location.Where(o => o.id_koord1 == f.point_out.point_this.id).ToList();
            foreach (var b in bl)
            {
                if (b.sLocation.Where(o => o.Id_point2 == f.point_this.id).ToList().Count > 0)
                {
                    List<sLocation> sl = sort_big_local(b);
                    foreach (var s in sl)
                    {
                        if (s.Id_point1 == f.point_this.id) break;
                        else
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value - 0.00015, Longitude = s.sKoord_point.Longitude.Value - 0.00015, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value - 0.00015, Longitude = s.sKoord_point1.Longitude.Value - 0.00015, id = s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                    f = f.point_out;
                    break;
                }
            }

            while (f.point_out != null)
            {
                if (f.point_out.point_this == wr.a)
                {
                    List<Big_Location> bl_a = f.point_this.Big_Location1.ToList();
                    for (int i = 0; i < bl_a.Count; i++)
                    {
                        Big_Location bla = bl_a[i];
                        List<sLocation> sla = sort_big_local(bla);
                        if (sla.Where(o => o.sKoord_point == wr.a).FirstOrDefault() != null)
                        {
                            for (int j = sla.Count - 1; j > -1; j--)
                            {
                                MyLocation ml = new MyLocation();
                                ml.PointA = new MyPoint() { Latitude = sla[j].sKoord_point.Latitude.Value - 0.00015, Longitude = sla[j].sKoord_point.Longitude.Value - 0.00015, id = sla[j].Id_point1 };
                                ml.PointB = new MyPoint() { Latitude = sla[j].sKoord_point1.Latitude.Value - 0.00015, Longitude = sla[j].sKoord_point1.Longitude.Value - 0.00015, id = sla[j].Id_point2 };
                                ml.Color = color;
                                ml.id_loc = sla[j].Id;
                                map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                                wr.way.Add(ml);
                                if (sla[j].sKoord_point == wr.a)
                                {
                                    break;
                                }
                            }
                        }
                        break;
                    }
                    break;
                }
                else
                {
                    Big_Location bbl = be.Big_Location.Where(o => o.id_koord2 == f.point_this.id && o.id_koord1 == f.point_out.point_this.id).FirstOrDefault();
                    if (bbl != null)
                    {
                        List<sLocation> sl = sort_big_local(bbl);
                        foreach (var s in sl)
                        {
                            MyLocation ml = new MyLocation();
                            ml.PointA = new MyPoint() { Latitude = s.sKoord_point.Latitude.Value - 0.00015, Longitude = s.sKoord_point.Longitude.Value - 0.00015, id = s.Id_point1 };
                            ml.PointB = new MyPoint() { Latitude = s.sKoord_point1.Latitude.Value - 0.00015, Longitude = s.sKoord_point1.Longitude.Value - 0.00015, id = s.Id_point2 };
                            ml.Color = color;
                            ml.id_loc = s.Id;
                            map1.Children.Add(PrintWayOnMap(ml, wr.name_algoritm));
                            wr.way.Add(ml);
                        }
                    }
                }
                f = f.point_out;
            }

            List<MyLocation> lml = new List<MyLocation>();
            int id = wr.a.id;
            for (int i = 0; i < wr.way.Count; i++)
            {
                lml.Add(wr.way.Where(o => o.PointA.id == id).First());
                id = lml[lml.Count - 1].PointB.id;
            }
            wr.way.Clear();
            wr.way = lml;
        }

        public class point_width
        {
            public sKoord_point point_this;
            public point_width point_out;
            public double length;

            public void set_point_out(point_width a)
            {
                this.point_out = a;
            }
        }

        #endregion

        public void math_time_motion(way_repos wr, TimePoint tp)
        {
           
            //double AvgSpeed=0;
            double AvgTime = 0;
            

            double Time = My.GetTimeDouble(DateTime.Parse(wr.t));
            int TypeDay = select_type_day;

            for (int i = 0; i < wr.way.Count(); i++)
            {
                MyTimeLocation TempLoc = new MyTimeLocation();
                    TempLoc.IdLocation=wr.way[i].id_loc;
                #region Расчет времени прохождения локации
                double dtYear = 0;
                double srZnach = 0;
                    //dtYear = bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time).Select(o => o.Val).FirstOrDefault();
                    //dtYear = (dtYear == 0) ? bbz_year.Where(o => o.Days == TypeDay && o.Location == TempLoc.IdLocation && o.Times == Time + 1).Select(o => o.Val).FirstOrDefault() : dtYear;
                dtYear = bbz_year.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Val).FirstOrDefault();

                //вычисление среднего с учетом поправкок если есть коэффециент поправочный
                if (dtYear > 0)
                {
                    /*!!!!*/
                    var koeff = 0;// be.bz_koeff.Where(o => o.Location == TempLoc.IdLocation).Select(o => o.Koeff).FirstOrDefault();
                    srZnach = ((koeff != 0) ? koeff : 1) * dtYear;
                    //запись в лист
                    //TempLoc.TimeLocation = srZnach;

                    //сдвиг времени на значение среднего
                    //Time += srZnach / (10.0 * 60.0);//сдвигаемся на сколько то секунд и определяем номер текущего среза

                    //определение средней скорости
                    //sLocation select_location = bslocation.Where(o => o.Id == TempLoc.IdLocation).FirstOrDefault();
                    //AvgSpeed += select_location.length.Value / TempLoc.TimeLocation;
                    AvgTime += srZnach;
                }
                else
                {
                    AvgTime += srZnach;
                    //Time += srZnach / (60.0*10);
                    //break;//нет данных о локациях (мало статистики)
                }

                #endregion
            }
           // double s = AvgSpeed;
            wr.time_motion = TimeSpan.FromSeconds(Math.Floor(AvgTime)).ToString();
        }

    }

    public class way_repos
    {
        public string name_algoritm { get; set; }
        public string t { get; set; }
        public sKoord_point a { get; set; }
        public sKoord_point b { get; set; }
        public double length { get; set; }
        public string time_motion { get; set; }
        public string time_work { get; set; }
        public SolidColorBrush colour { get; set; }
        public List<MyLocation> way = new List<MyLocation>();
    }

    //локация
    public class MyLocation
    {
        public MyLocation()
        {
            Color = Brushes.Yellow;
        }
        public int nom;
        public double way;
        public MyPoint PointA;
        public MyPoint PointB;
        public int NextLocation;
        public SolidColorBrush Color;
        public int id_loc;
        public double TimeLocation;
    }

    public class TimePoint
    {
        public int timeInt { get; set; }
        public string timeString
        {
            get
            {
                return TimePointToTime(timeInt);
            }
        }
        public string TimePointToTime(int tp)
        {
            DateTime dt = new DateTime().AddMinutes(tp * 10);
            return dt.ToShortTimeString();
        }
    }
    public class TC
    {
        public int num;
        public int ID_Marh;
        /// <summary>
        /// Долгота 78
        /// </summary>
        public double Longitude;
        /// <summary>
        /// Широта 55
        /// </summary>
        public double Latitude;
        public double Ugol;
        public DateTime Date;

        public void Go()
        {
            MyLocation = My.GiveLocation(ID_Marh, Ugol, Longitude, Latitude);
            MyPercent = (MyLocation != null) ? MyPercent = My.GivePersent(MyLocation.nom, Longitude, Latitude) : 0;
        }

        public MyLocation MyLocation;
        public double MyPercent;
    }
    public class BigLoc
    {
        public int id;
        public MyPoint A;
        public MyPoint B;
        public float leng;
    }
    public class M
    {
        public MyPoint B;
    }



    //точки с координатами
    public class MyPoint
    {
        public int id;
        public double Latitude;//широта
        public double Longitude;//долгота
    }

    public class MyTimeBus
    {
        public MyMarsh Marsh { get; set; }
        public TimeSpan? AtPointA { get; set; }
        public TimeSpan? AtPointB { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public List<MyTimeLocation> Location { get; set; }
    }
    public class MyMarsh
    {
        public int id { get; set; }
        public string name { get; set; }
        public int type { get; set; }
    }
    public class MyTimeLocation
    {
        public int IdLocation { get; set; }
        public double TimeLocation { get; set; }
    }   
    public class MyBusOnMarsh
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public int Id_Activ_Location { get; set; }
        public double Pos_Activ_Location { get; set; }
        public double azimuth { get; set; }
        public DateTime dtUpdate { get; set; }
        public List<MyTimeLocation> TimeLocation { get; set; }

        public int temp1;
        public int temp2;
    }
}