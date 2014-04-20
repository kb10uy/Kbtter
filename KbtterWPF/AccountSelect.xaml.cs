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
using System.IO;
using TweetSharp;
using Kb10uy.Scripting;
using System.Diagnostics;

namespace KbtterWPF
{
    /// <summary>
    /// AccountSelect.xaml の相互作用ロジック
    /// </summary>
    public partial class AccountSelect : Window
    {
        AccountsList al;
        MainWindow main;

        public AccountSelect(MainWindow mw)
        {
            InitializeComponent();
            main = mw;
        }

        public OAuthAccessToken ShowDialogWith()
        {
            ShowDialog();

            var si = AccountList.SelectedItem as string;
            if (si == null) return null;
            return al.Tokens[si];
        }

        private void NewAcRegist_Click(object sender, RoutedEventArgs e)
        {
            var nsn = NewAcName.Text;
            var rt = main.Service.GetRequestToken();
            Process.Start(main.Service.GetAuthorizationUri(rt).ToString());
            var aw = new AuthorizeWindow();
            aw.ShowDialog();
            if (aw.PIN == "") return;
            var at = main.Service.GetAccessToken(rt, aw.PIN);
            if (at.ScreenName == "") return;
            al.Tokens[nsn] = at;
            al.SaveToFile("accounts.cfg");
            AccountList.Items.Add(nsn);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            al = new AccountsList();
            if (File.Exists("accounts.cfg"))
            {
                Twiex.ReadFromFile<AccountsList>("accounts.cfg", out al);
            }
            foreach (var i in al.Tokens)
            {
                AccountList.Items.Add(i.Key);
            }
        }

        private void AccountList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AccountList.Items.Count != 0 && AccountList.SelectedIndex != -1) Close();
        }

        private void AccountList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Close();
        }
    }

    public class AccountsList
    {
        public Dictionary<string, OAuthAccessToken> Tokens { get; set; }

        public AccountsList()
        {
            Tokens = new Dictionary<string, OAuthAccessToken>();
        }
    }

}
