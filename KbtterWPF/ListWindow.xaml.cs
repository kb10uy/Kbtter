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
using Kb10uy.Extension;
using TweetSharp;
using Kbtter;

namespace KbtterWPF
{
    /// <summary>
    /// ListWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ListWindow : Window
    {
        MainWindow main;
        TwitterService svc;
        TwitterUser me, tg;

        public ListWindow(MainWindow mw, TwitterUser ta)
        {
            main = mw;
            svc = main.Service;
            me = main.CurrentUser;
            tg = ta;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TargetName.Text = tg.ScreenName;
            RefreshLists();
        }

        private void RefreshLists()
        {
            ListsList.Items.Clear();
            svc.ListListsFor(new ListListsForOptions { UserId = me.Id }, (ietl, res) =>
            {
                svc.ListListMembershipsFor(new ListListMembershipsForOptions { UserId = tg.Id, FilterToOwnedLists = true }, (tctl, res2) =>
                {
                    foreach (var i in ietl)
                    {
                        ListsList.Dispatch(() =>
                        {
                            ListsList.Items.Add(CreateListPanel(i, tctl.Any(p => p.Id == i.Id)));
                        });
                    }
                });

            });
        }

        private void ListCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ListOK_Click(object sender, RoutedEventArgs e)
        {
            foreach (var i in ListsList.Items)
            {
                var ls = (i as StackPanel).Tag as TwitterList;
                var ch = ((i as StackPanel).Children[0] as CheckBox);
                var om = (bool)ch.Tag;
                if (ch.IsChecked != om)
                {
                    if (om)
                    {
                        svc.RemoveListMember(new RemoveListMemberOptions { ListId = ls.Id, UserId = tg.Id }, (tu, res) => { });
                    }
                    else
                    {
                        svc.AddListMember(new AddListMemberOptions { ListId = ls.Id, UserId = tg.Id }, (tu, res) => { });
                    }
                }

            }
            Close();
        }

        public UIElement CreateListPanel(TwitterList list, bool ic)
        {
            var s = new StackPanel();
            var si = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            var ch = new CheckBox();
            ch.Margin = new Thickness(16);
            ch.IsChecked = ic;
            ch.Tag = ic;
            s.Children.Add(ch);

            var ti = new TextBlock { FontSize = 20 };
            ti.Text = (list.Mode == "private" ? "*" : "") + list.Name;
            ti.TextWrapping = TextWrapping.Wrap;
            var de = new TextBlock { Foreground = Brushes.Gray };
            de.Text = list.Description;
            de.TextWrapping = TextWrapping.Wrap;
            si.Children.Add(ti);
            si.Children.Add(de);
            s.Children.Add(si);
            s.Tag = list;
            return s;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (ListsList.SelectedIndex == -1) return;
            var ls = (ListsList.Items[ListsList.SelectedIndex] as StackPanel).Tag as TwitterList;
            svc.DeleteList(new DeleteListOptions { ListId = ls.Id }, (lsx, re) =>
            {
                if (re.Error != null) return;
                ListsList.Dispatch(() => {
                    ListsList.Items.RemoveAt(ListsList.SelectedIndex);
                    ListsList.SelectedIndex = -1;
                });
            });
        }

        private void NewListCreate_Click(object sender, RoutedEventArgs e)
        {
            var md = NewListPrivate.IsChecked.Value;
            svc.CreateList(
                new CreateListOptions
                {
                    
                    Name = NewListName.Text == "" ? "無題" : NewListName.Text,
                    Description = NewListDesc.Text == "" ? " " : NewListDesc.Text,
                    Mode = md ? TwitterListMode.Private : TwitterListMode.Public
                }, (ls2, res) =>
                {
                    if (ls2 == null || res.Error != null) return;
                    ListsList.Dispatch(() => ListsList.Items.Add(CreateListPanel(ls2, false)));
                    NewListExp.Dispatch(() => NewListExp.IsExpanded = false);
                });
        }
    }
}
