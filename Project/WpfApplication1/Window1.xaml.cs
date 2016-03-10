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

namespace WpfApplication1
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        List<way_repos> w_repos = new List<way_repos>();
        public Window1()
        {
            InitializeComponent();
        }


        public void review(List<way_repos> wr)
        {
            this.w_repos = wr;
            this.gridResult.ItemsSource = w_repos;        
            this.gridResult.Items.Refresh();
        }

        private void draw_way(object sender, RoutedEventArgs e)
        {
            List<way_repos> wr = new List<way_repos>();
            if (gridResult.SelectedItems.Count > 0)
            {
                for (int i = 0; i < gridResult.SelectedItems.Count; i++)
                {
                    wr.Add((way_repos)gridResult.SelectedItems[i]);
                }
            }

            MainWindow mfrm = (MainWindow)this.Owner;
            mfrm.draw_way(wr);
        }
    }
}
