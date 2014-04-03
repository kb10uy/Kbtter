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

namespace KbtterWPF
{
    /// <summary>
    /// Authorize.xaml の相互作用ロジック
    /// </summary>
    public partial class AuthorizeWindow : Window
    {

        public string PIN { get; private set; }

        public AuthorizeWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            PIN = PINCode.Text;
        }

        private void PINOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
