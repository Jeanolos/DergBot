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

namespace OakBot
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            textBoxStreamerName.LostFocus += MainWindow.instance.textBoxStreamerName_LostFocus;
            btnStreamerConnect.Click += MainWindow.instance.btnStreamerConnect_Click;
            cbAutoConnectStreamer.Checked += MainWindow.instance.cbAutoConnectStreamer_Checked;
            cbAutoConnectStreamer.Unchecked += MainWindow.instance.cbAutoConnectStreamer_Unchecked;
            cbAutoConnectBot.Checked += MainWindow.instance.cbAutoConnectBot_Checked;
            cbAutoConnectBot.Unchecked += MainWindow.instance.cbAutoConnectBot_Unchecked;
            btnImport.Click += MainWindow.instance.btnImport_Click;
            textBoxBotName.LostFocus += MainWindow.instance.textBoxBotName_LostFocus;
            buttonBotConnect.Click += MainWindow.instance.buttonBotConnect_Click;
            btnBotConnect.Click += MainWindow.instance.btnBotConnect_Click;
            cbServerIP.SelectionChanged += MainWindow.instance.comboBox_SelectionChanged;
            cbServerPort.SelectionChanged += MainWindow.instance.comboBox_Copy_SelectionChanged;
            button.Click += MainWindow.instance.button_Click;
            btnLogin.Click += MainWindow.instance.btnLogin_Click;
            txtToken.LostFocus += MainWindow.instance.txtToken_LostFocus;
        }
    }
}
