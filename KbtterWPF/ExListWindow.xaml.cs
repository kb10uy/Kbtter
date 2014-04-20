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
using Kbtter;
using TweetSharp;

namespace KbtterWPF
{
    /// <summary>
    /// ExListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ExListWindow : Window
    {
        MainWindow main;
        TwitterService svc;
        TwitterUser me;

        public ExListWindow(MainWindow mw)
        {
            InitializeComponent();
            main = mw;
            svc = main.Service;
            me = main.CurrentUser;
        }

        private void RefreshLists()
        {
            svc.ListListsFor(new ListListsForOptions { UserId = me.Id }, (ietl, res) =>
            {
                MyListsList.Dispatch(() =>
                {
                    MyListsList.Items.Clear();
                    foreach (var i in ietl)
                    {
                        MyListsList.Items.Add(CreateListPanel(i));
                    }
                });
            });
            svc.ListListMembershipsFor(new ListListMembershipsForOptions { UserId = me.Id }, (tcltl, res) =>
            {
                AddedMeList.Dispatch(() =>
                {
                    AddedMeList.Items.Clear();
                    foreach (var i in tcltl)
                    {
                        AddedMeList.Items.Add(CreateListPanel(i));
                    }
                });
            });
        }

        public UIElement CreateListPanel(TwitterList list)
        {
            var s = new StackPanel();
            var si = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            Image img = new Image { Width = 36, Height = 36 };
            BitmapImage bi = new BitmapImage(new Uri(list.User.ProfileImageUrlHttps));
            img.Source = bi;
            s.Children.Add(img);

            var ti = new TextBlock { FontSize = 20 };
            ti.Text = (list.Mode == "private" ? "*" : "") + list.Name;
            ti.TextWrapping = TextWrapping.Wrap;
            var de = new TextBlock { Foreground = Brushes.Gray };
            de.Text = list.Description;
            de.TextWrapping = TextWrapping.Wrap;
            var cn = new TextBlock();
            cn.Text = list.MemberCount.ToString() + "人が追加されています";
            si.Children.Add(ti);
            si.Children.Add(cn);
            si.Children.Add(de);
            s.Children.Add(si);
            s.Tag = list;
            return s;
        }

        private void ListUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (MyListsList.SelectedIndex == -1) return;
            var sl = (MyListsList.Items[MyListsList.SelectedIndex] as StackPanel).Tag as TwitterList;
            var md = ListPrivate.IsChecked.Value;
            svc.UpdateList(new UpdateListOptions
            {
                ListId = sl.Id,
                Name = ListName.Text,
                Description = ListDescription.Text,
                Mode = md ? TwitterListMode.Private : TwitterListMode.Public
            },
                (ls, res) =>
                {
                    RefreshLists();
                });

        }

        private void MyListsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyListsList.SelectedIndex == -1) return;
            var sl = (MyListsList.Items[MyListsList.SelectedIndex] as StackPanel).Tag as TwitterList;
            ListName.Text = sl.Name;
            ListDescription.Text = sl.Description;
            ListPrivate.IsChecked = sl.Mode == "private";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshLists();
        }

        private void NListCreate_Click(object sender, RoutedEventArgs e)
        {
            var md = NListPrivate.IsChecked.Value;
            svc.CreateList(
                new CreateListOptions
                {

                    Name = NListName.Text == "" ? "無題" : NListName.Text,
                    Description = NListDescription.Text == "" ? " " : NListDescription.Text,
                    Mode = md ? TwitterListMode.Private : TwitterListMode.Public
                }, (ls2, res) =>
                {
                    if (ls2 == null || res.Error != null) return;
                    MyListsList.Dispatch(() => MyListsList.Items.Add(CreateListPanel(ls2)));
                    NewListExp.Dispatch(() => NewListExp.IsExpanded = false);
                });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (MyListsList.SelectedIndex == -1) return;
            var ls = (MyListsList.Items[MyListsList.SelectedIndex] as StackPanel).Tag as TwitterList;
            svc.DeleteList(new DeleteListOptions { ListId = ls.Id }, (lsx, re) =>
            {
                if (re.Error != null) return;
                MyListsList.Dispatch(() =>
                {
                    MyListsList.Items.RemoveAt(MyListsList.SelectedIndex);
                    MyListsList.SelectedIndex = -1;
                });
            });
        }
    }
}
