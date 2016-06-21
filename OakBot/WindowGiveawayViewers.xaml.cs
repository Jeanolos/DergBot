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

namespace OakBot
{
    /// <summary>
    /// Interaction logic for WindowGiveawayViewers.xaml
    /// </summary>
    public partial class WindowGiveawayViewers : Window
    {
        public WindowGiveawayViewers(Giveaway gw)
        {
            InitializeComponent();
            lbViewers.ItemsSource = gw.Entries;
        }

        private void lbViewers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(lbViewers.SelectedIndex != -1)
            {
                Viewer selected = MainWindow.colDatabase.First(x => x.UserName.ToLower() == lbViewers.SelectedItem.ToString().ToLower());
                WindowViewerChat vwr = new WindowViewerChat(MainWindow.instance, selected);
                vwr.Show();
            }
        }
    }
}
