using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
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
    /// DirectMessage.xaml の相互作用ロジック
    /// </summary>
    public partial class DirectMessage : Window
    {
        MainWindow main;
        TwitterService svc;
        HashSet<TwitterUser> users;
        ConcurrentDictionary<TwitterUser, List<TwitterDirectMessage>> dmlist;
        TwitterUser TargetUser;
        TwitterUser CurrentUser;
        object sync = new object();

        public DirectMessage(MainWindow mw)
        {
            InitializeComponent();
            main = mw;
            svc = main.Service;
            CurrentUser = main.CurrentUser;
            users = new HashSet<TwitterUser>();
            dmlist = new ConcurrentDictionary<TwitterUser, List<TwitterDirectMessage>>();
        }

        private void DMSendText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            svc.SendDirectMessage(new SendDirectMessageOptions { UserId = TargetUser.Id, Text = DMSendText.Text }, async (d, r) =>
            {
                await Reset();
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Reset();
        }

        public Task RefreshDMList()
        {
            return Task.Run(() =>
            {
                dmlist = new ConcurrentDictionary<TwitterUser, List<TwitterDirectMessage>>();
                users = new HashSet<TwitterUser>();
                var iedm = svc.ListDirectMessagesReceived(new ListDirectMessagesReceivedOptions { IncludeEntities = true, Count = 400 });
                foreach (var i in iedm)
                {
                    if (!users.Contains(i.Sender))
                    {
                        lock (sync)
                        {
                            users.Add(i.Sender);
                            dmlist[i.Sender] = new List<TwitterDirectMessage>();
                            dmlist[i.Sender].Add(i);
                        }
                    }
                    else
                    {
                        lock (sync) dmlist[i.Sender].Add(i);
                    }
                }

                iedm = svc.ListDirectMessagesSent(new ListDirectMessagesSentOptions { Count = 400, IncludeEntities = true });

                foreach (var i in iedm)
                {

                    if (!users.Contains(i.Recipient))
                    {
                        lock (sync)
                        {
                            users.Add(i.Recipient);
                            dmlist[i.Recipient] = new List<TwitterDirectMessage>();
                            dmlist[i.Recipient].Add(i);
                        }
                    }
                    else
                    {
                        lock (sync) dmlist[i.Recipient].Add(i);
                    }
                }
                foreach (var i in dmlist)
                {
                    i.Value.Sort((x, y) => x.CreatedDate.CompareTo(y.CreatedDate));
                }
            });

        }

        private void DMUserList_Selected(object sender, RoutedEventArgs e)
        {
            if (DMUserList.SelectedIndex == -1) return;
            var us = ((DMUserList.Items[DMUserList.SelectedIndex]) as StackPanel).Tag as TwitterUser;
            TargetUser = us;
            BuildDMLine();
        }

        private void BuildDMLine()
        {
            DMLine.Items.Clear();
            var tgl = dmlist[TargetUser];
            foreach (var i in tgl)
            {
                DMLine.Items.Add(CreateDMPanel(i, i.Sender == CurrentUser));
            }
        }

        private UIElement CreateDMPanel(TwitterDirectMessage dm, bool isme)
        {
            ListViewItem lvi = new ListViewItem();
            lvi.HorizontalContentAlignment = HorizontalAlignment.Right;
            lvi.Margin = new Thickness(2);
            var s = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            var so = new StackPanel();
            if (isme)
            {
                s.HorizontalAlignment = HorizontalAlignment.Right;
                lvi.HorizontalContentAlignment = HorizontalAlignment.Right;
                lvi.Background = Brushes.LightCyan;
                var tb = new TextBlock();
                tb.Text = dm.Text;
                tb.TextWrapping = TextWrapping.Wrap;
                Image im2 = new Image();
                BitmapImage bi = new BitmapImage(new Uri(CurrentUser.ProfileImageUrlHttps));
                im2.Source = bi;
                im2.Width = 36;
                im2.Height = 36;
                s.Children.Add(tb);
                s.Children.Add(im2);
                var l = new TextBlock { FontSize = 10 };
                l.Text = dm.CreatedDate.AddHours(9).ToString();
                l.TextAlignment = TextAlignment.Right;
                so.Children.Add(s);
                so.Children.Add(l);
            }
            else
            {
                s.HorizontalAlignment = HorizontalAlignment.Left;
                lvi.HorizontalContentAlignment = HorizontalAlignment.Left;
                lvi.Background = Brushes.Honeydew;
                var tb = new TextBlock();
                tb.Text = dm.Text;
                tb.TextWrapping = TextWrapping.Wrap;
                Image im2 = new Image();
                BitmapImage bi = new BitmapImage(new Uri(TargetUser.ProfileImageUrlHttps));
                im2.Source = bi;
                im2.Width = 36;
                im2.Height = 36;
                s.Children.Add(im2);
                s.Children.Add(tb);
                var l = new TextBlock { FontSize = 10 };
                l.TextAlignment = TextAlignment.Left;
                l.Text = dm.CreatedDate.AddHours(9).ToString();
                so.Children.Add(s);
                so.Children.Add(l);
            }
            lvi.Content = so;
            return lvi;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await Reset();
        }

        private async Task Reset()
        {
            await RefreshDMList();
            DMLine.Dispatch(() =>
            {
                DMLine.Items.Clear();
                DMLine.SelectedIndex = -1;
            });
            DMUserList.Dispatch(() => {
                DMUserList.Items.Clear();
                DMUserList.SelectedIndex = -1;

                foreach (var u in users)
                {
                    DMUserList.Items.Add(main.CreateUserPanel(u));
                }
            });

        }

        private void CreateDM_Click(object sender, RoutedEventArgs e)
        {
            svc.GetUserProfileFor(new GetUserProfileForOptions { ScreenName = NewUserName.Text },
                (u, r) =>
                {
                    if (u == null)
                    {
                        MessageBox.Show("そのようなユーザーは存在しません", "ユーザー取得失敗");
                        return;
                    }
                    if (users.Contains(u)) return;
                    DMUserList.Dispatch(() =>
                    {
                        DMUserList.Items.Add(main.CreateUserPanel(u));
                        dmlist[u] = new List<TwitterDirectMessage>();
                    });
                });
        }

    }
}
