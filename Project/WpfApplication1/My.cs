using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data;

namespace WpfApplication1
{
    public static class My
    {
        //public static SqlConnection sqlCon = new SqlConnection("Data Source=46.181.137.131;Initial Catalog=marshrutsTsKem;Persist Security Info=True;User ID=navi;Password=transnavi");
        //public static SqlConnection sqlCon = new SqlConnection("Data Source=192.168.0.154;Initial Catalog=bdBus;Integrated Security=True");
        //public static SqlConnection sqlCon = new SqlConnection("Data Source=HOME-PC\\SQLEXPRESS;Initial Catalog=bdBus;Integrated Security=True");
        public static SqlConnection sqlCon = new SqlConnection("Data Source=doroganovv.asuscomm.com;Initial Catalog=bdBus;Persist Security Info=True;User ID=sa;Password=q2w3e4r");
        /// <summary>
        /// Определение локации по номеру маршрута, углу направления и координатам
        /// </summary>
        /// <param name="marsh">Номер маршрута</param>
        /// <param name="ugol">Угол направления</param>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <returns>Найденная локация</returns>
        public static MyLocation GiveLocation(int marsh, double azimuth, double Longitude, double Latitude)
        {
            //double s = ((y1 - y2) * x + (x1 - x2) * y + (x1 * y2 - x2 * y1)) / Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            //можно попробовать заменить растояние на хранимую процедуру

            //надо проверить работоспособность этого при нахождении в условии, если не получиться, перенести в SELECT
            string SQL = @"SELECT slocation.id, slocation.length, 
                               ABS
                                (
                                    (
                                        (#Latitude - kord_point1.Latitude) * (kord_point2.Longitude - kord_point1.Longitude) - 
										(#Longitude - kord_point1.Longitude) * (kord_point2.Latitude - kord_point1.Latitude)
                                    ) /
                                    SQRT
                                    (
                                        (kord_point2.Longitude - kord_point1.Longitude)*(kord_point2.Longitude - kord_point1.Longitude) 
                                      + (kord_point2.Latitude - kord_point1.Latitude)*(kord_point2.Latitude - kord_point1.Latitude)
                                    ) 
                                ) as len

                          FROM sKoord_point as kord_point1, sKoord_point as kord_point2, sLocation, sMarhList as sMarshList
                          WHERE 
                                sMarshList.Id_marsh=#marsh   
                            AND sLocation.id=sMarshList.Id_location
                            AND ((abs(sLocation.azimuth-(#azimuth)) < 40) or (abs(sLocation.azimuth-(#azimuth)+360)< 40))
                            
                            AND slocation.Id_point1=kord_point1.id 
                            AND slocation.Id_point2=kord_point2.id 
                            AND 
                            (
                                 (#Longitude-kord_point1.Longitude)*(kord_point2.Longitude-kord_point1.Longitude) +
                                 (#Latitude-kord_point1.Latitude)*(kord_point2.Latitude-kord_point1.Latitude)
                            ) >= 0
                            AND
                            (
                                (#Longitude-kord_point2.Longitude)*(kord_point1.Longitude-kord_point2.Longitude) +
                                (#Latitude-kord_point2.Latitude)*(kord_point1.Latitude-kord_point2.Latitude)
                            ) >= 0 
                            
                            Order by len desc";
            //AND sLocation.id=MarshList.Id_location
            SQL = SQL.Replace("#Longitude", Longitude.ToString().Replace(',', '.'));
            SQL = SQL.Replace("#Latitude", Latitude.ToString().Replace(',', '.'));
            SQL = SQL.Replace("#azimuth", azimuth.ToString().Replace(',', '.'));
            SQL = SQL.Replace("#marsh", marsh.ToString());
            //lock (sqlCon)
            {
                SqlDataAdapter sdaMain = new SqlDataAdapter(SQL, sqlCon);
                DataTable dtMain = new DataTable();
                sdaMain.Fill(dtMain);
                if (dtMain.Rows.Count != 0)
                {
                    MyLocation l = new MyLocation();
                    l.nom = (int)dtMain.Rows[0]["ID"];
                    l.way = (double)dtMain.Rows[0]["length"];
                    //MessageBox.Show(marsh+"  "+dtMain.Rows[0]["len"].ToString());
                    if ((double)dtMain.Rows[0]["len"] > 0.01) return null;
                    return l;
                }
            }
            return null;
        }
        /// <summary>
        /// Определение % прохождениея локации
        /// </summary>
        /// <param name="MyLocation">Номер локации</param>
        /// <param name="x">Координата X</param>
        /// <param name="y">Координата Y</param>
        /// <returns>Прохождение локации в интервале от 0 до 1</returns>
        public static double GivePersent(int MyLocation, double Longitude, double Latitude)//
        {
            string SQL = @"SELECT kp1.Longitude as xA, kp1.Latitude as yA, kp2.Longitude as xB, kp2.Latitude as yB
                            FROM  sLocation, sKoord_point as kp1, sKoord_point as kp2
                            WHERE slocation.id =#l 
                              AND kp1.id=Id_Point1
                              AND kp2.id=Id_Point2";
            SQL = SQL.Replace("#l", MyLocation.ToString());

            SqlDataAdapter sdaMain = new SqlDataAdapter(SQL, sqlCon);
            DataTable dtMain = new DataTable();
            sdaMain.Fill(dtMain);
            if (dtMain.Rows.Count > 0)
            {
                double xA = (double)dtMain.Rows[0]["xA"];
                double yA = (double)dtMain.Rows[0]["yA"];
                double xB = (double)dtMain.Rows[0]["xB"];
                double yB = (double)dtMain.Rows[0]["yB"];

                //уравнение прямой: y=(x * yB - x * yA - xA * yB + yA * xA)/(xB - xA) - yA
                double ygol_loc = (yB - yA) / (xB - xA);
                double sdvig_loc = xA * (yA - yB) / (xB - xA) + yA;

                //уравнение прямой, перпендикулярная данной. т.к. прямая перпендикулярна, угловой коэффециент обратная величина
                double ygol_point = -1 / ygol_loc;
                //остаётся посчитать сдвиг прямой, зная её точку и угловой коэффециент
                double sdvig_point = -ygol_point * Longitude + Latitude;

                //решаем систему уравнений и находим точку пересечения
                double xM = (sdvig_point - sdvig_loc) / (ygol_loc - ygol_point);
                double yM = ygol_point * xM + sdvig_point;

                double len = Math.Sqrt(Math.Pow(xA - xB, 2) + Math.Pow(yA - yB, 2));
                double lenPoint = Math.Sqrt(Math.Pow(xA - xM, 2) + Math.Pow(yA - yM, 2));//считаем растояния
                double procent = lenPoint / len;//считаем % прохождения локации
                return procent;
            }
            //определяем координаты проекции, затем расчитываем растояния от начала до точки и делим на длину локации
            return 0;
        }
        /// <summary>
        /// Определение пройденных локаций
        /// </summary>
        /// <param name="Location1">Начальная локация</param>
        /// <param name="Location2">Конечная локация</param>
        /// <param name="marsh">Маршрут</param>
        /// <returns>Список локаций</returns>

        public static List<MyLocation> GiveBetweenLocation(int Location1, int Location2, int marsh)
        {
            List<MyLocation> coll = new List<MyLocation>();
            string SQL = @"SELECT sLocation.id, sLocation.length, sLocation.Id_point1, sLocation.Id_point2
                            FROM  sMarhList as sMarshList, sLocation
                            WHERE sMarshList.Id_Location = sLocation.id 
                            AND   (sMarshList.Id_marsh= #m)";
            SQL = SQL.Replace("#m", marsh.ToString());

            SqlDataAdapter sdaMain = new SqlDataAdapter(SQL, sqlCon);
            DataTable dtMain = new DataTable();
            sdaMain.Fill(dtMain);
            foreach (DataRow dr in dtMain.Rows)
            {
                MyLocation temp = new MyLocation();
                temp.nom = (int)dr["id"];
                temp.way = (double)dr["length"];
                //temp.PointA = (int)dr["Id_point1"];
                //temp.PointB = (int)dr["Id_point2"];
                coll.Add(temp);
            }
            List<MyLocation> Coll_marsh = new List<MyLocation>();
            MyLocation x1 = coll.Where(o => o.nom == Location1).FirstOrDefault();
            if (x1 != null)
            {
                bool flag = true;
                while (flag)
                {
                    //List<MyLocation> temp = coll.Where(o => o.nom == x1[0].NextLocation).ToList();
                    MyLocation temp = coll.Where(o => o.PointA == x1.PointB).FirstOrDefault();
                    if (temp != null)
                    {
                        if (temp.nom == Location2) break;//и дойдя до последней, её тоже не включаем, просто прекращаем цикл

                        if (temp.PointA == temp.PointB) return null;
                        //if (temp[0].nom == x1[0].NextLocation) return null;
                        if (Coll_marsh.Count > 100) return null;
                        Coll_marsh.Add(temp);
                        x1 = temp;
                    }
                    else
                        return null;
                }
            }
            else
                return null;

            return Coll_marsh;
        }
        public static int GetTime(DateTime dt)//получить номер текущего времянного среза
        {
            //int BeginTime = 240;//начало 4 утра в минутах            
            int BeginTime = 0;//начало в 00:00
            int NowTime = dt.Hour * 60 + dt.Minute;
            int srez = (NowTime - BeginTime) / 10;//важно чтобы остаток отбросился!
            return srez;
        }
        //надо брать из базы данных!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        public static double GetTimeDouble(DateTime dt)//получить номер текущего времянного среза
        {
            //int BeginTime = 240;//начало 4 утра в минутах            
            int BeginTime = 0;//начало в 00:00
            double NowTime = dt.Hour * 60 + dt.Minute + (dt.Second / 60.0);
            double srez = (NowTime - BeginTime) / 10;//важно чтобы остаток отбросился!
            return srez;
        }
        public static int GetTypeDay()//получить текущий тип дня
        {
            //если текущая дата празднечная то вернуть 2, если предпразднечная - 1, будни - 0
            DateTime dt = DateTime.Today;

            //            //если текущий день принудительно празднечный или будничный
            //            string SQL = @"SELECT TypeDay FROM  sHolidays
            //                    WHERE (TypeDay = 2 or TypeDay = 0) and (DateHolidays= '#m')
            //                    UNION 
            //                        SELECT 1 as TypeDay FROM  sHolidays
            //                    WHERE (TypeDay = 2) and (DateHolidays= '#m1')";
            //            SQL = SQL.Replace("#m1", dt.AddDays(1).ToShortDateString());
            //            SQL = SQL.Replace("#m", dt.ToShortDateString());
            //            SqlDataAdapter sdaMain = new SqlDataAdapter(SQL, sqlCon);
            //            DataTable dtMain = new DataTable();
            //            sdaMain.Fill(dtMain);
            //            if (dtMain.Rows.Count > 0)
            //                return (int)dtMain.Rows[0]["TypeDay"];//возвращаем

            //иначе просто проверяем день недели
            if (dt.DayOfWeek == DayOfWeek.Sunday || dt.DayOfWeek == DayOfWeek.Saturday) return 2;
            if (dt.DayOfWeek == DayOfWeek.Friday) return 1;

            return 0;
        }

        public static double distance(double startLongitude, double startLatitude, double endLongitude, double endLatitude)
        {
            double d2r = Math.PI / 180;           // Множитель для перевода градусов в радианы
            double major = 6378137.0;            // Большая полуось
            double minor = 6356752.314245;   // Малая полуось
            double e2 = 0.006739496742337;  // Площадь эксцентриситета эллипсоида 
            double flat = 0.003352810664747; // Свед`ение эллипсоида
            // Получаем разницы между широтами-долготами
            double lambda = (startLongitude - endLongitude) * d2r;          // Разность долгот
            double phi = (startLatitude - endLatitude) * d2r;                     // Разность широт
            double phiMean = ((startLatitude + endLatitude) / 2.0) * d2r;  // Средняя широта
            // Расчет мередианального и траверсного радиусов кривизны в средних широтах 
            double temp = 1 - e2 * (Math.Pow(Math.Sin(phiMean), 2.0)); // Временная переменная
            double rho = (major * (1 - e2)) / Math.Pow(temp, 1.5);             // Меридиональный радиус кривизны
            double nu = major / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(phiMean), 2.0));  // Поперечный РК
            // Расчет углового расстояния
            double z = Math.Sqrt(Math.Pow(Math.Sin(phi / 2.0), 2.0) + Math.Cos(endLatitude * d2r) * Math.Cos(startLatitude * d2r) * Math.Pow(Math.Sin(lambda / 2.0), 2.0));
            z = 2 * Math.Asin(z);          // Угловое расстояние в центре сфероида
            // Расчет азимута
            double alpha = Math.Cos(endLatitude * d2r) * Math.Sin(lambda) * 1 / Math.Sin(z);
            alpha = Math.Asin(alpha);   // Азимут
            // Используем Теорему Эйлера для определения радиуса сферической Земли
            double r = rho * nu / (rho * Math.Pow(Math.Sin(alpha), 2.0) + nu * Math.Pow(Math.Cos(alpha), 2.0));
            // Устанавливаем азимут и расстояние
            return z * r; // Дистанция
        }
        public static double azimuth(double d1, double s1, double d2, double s2)
        {
            if (s1 == s2 && d1 == d2)
            {
                return 0;
            }
            else
            {
                // return Math.Atan(((d1 - d2) / (s1 - s2)) * Math.Cos((s1 - s2) / 2)) * 180 / Math.PI;
                double u = ((Math.Atan2(Math.Sin(d1 - d2) * Math.Cos(s2),
                Math.Cos(s1) * Math.Sin(s2) - Math.Sin(s1) * Math.Cos(s2) * Math.Cos(d1 - d2))) % (2 * Math.PI)) * 180 / Math.PI;
                if (u < 0) u += 360;
                return u;
            }

        }
    }

}
