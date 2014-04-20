using IronPython;
using IronPython.Hosting;
using Kb10uy.MultiMedia;
using Kb10uy.Scripting;
using Kbtter;
using Microsoft.Scripting.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;
using Icon = System.Drawing.Icon;
using TweetSharp;
using System.Media;
using Kb10uy.Extension;

namespace KbtterWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private void MainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            //OAuth認証とか
            Service = new TwitterService(ConsumerKey, ConsumerSecret);
            ACSelect = new AccountSelect(this);
            var atk = ACSelect.ShowDialogWith();
            if (atk == null)
            {
                Application.Current.Shutdown(1);
            }
            else
            {
                Service.AuthenticateWith(atk.Token, atk.TokenSecret);
                Service.GetUserProfile(new GetUserProfileOptions(), (tu, res) =>
                {
                    //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                    CurrentUser = tu;
                    SetDefaultUser();
                    LoadPlugins();
                    foreach (var p in InitializePlugins)
                    {
                        p.OnInitialize(Service, CurrentUser);
                    }
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        foreach (var p in WindowPlugins)
                        {
                            var n = p.GetPluginName();
                            var l = p.GetMenuList();
                            var pnp = new MenuItem { Header = n };
                            foreach (var s in l)
                            {
                                var m = new MenuItem();
                                m.Header = s.MenuTitle;
                                m.Click += m_Click;
                                m.Tag = s;
                                pnp.Items.Add(m);
                            }
                            MenuPlugin.Items.Add(pnp);
                        }
                    }));


                    Service.ListTweetsOnHomeTimeline(new ListTweetsOnHomeTimelineOptions { Count = PreloadTimelineCount }, (ie, re) =>
                    {
                        MainTimeline.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (ie == null) return;
                            foreach (var i in ie)
                            {
                                //なんか公式RTめんどい
                                if (i.RetweetedStatus != null)
                                {
                                    if (i.User == CurrentUser) continue;
                                    //公式RTが飛んできた

                                    var rto = i.RetweetedStatus;
                                    MainTimeline.Items.Add(CreateRetweetPanel(rto, i));
                                }
                                else
                                {
                                    MainTimeline.Items.Add(CreateTweetPanel(i));
                                }
                            }
                        }));
                    });
                    Service.CancelStreaming();

                    Stream = Service.StreamUser();

                    Stream.OfType<TwitterUserStreamStatus>().Subscribe(StatusAction);

                    Stream.OfType<TwitterUserStreamEvent>().Subscribe(EventAction);

                    Stream.OfType<TwitterUserStreamDirectMessage>().Subscribe(DMAction);

                });
            }
        }



        void m_Click(object sender, RoutedEventArgs e)
        {
            var s = (sender as MenuItem).Tag as KbtterPluginMenu;
            s.MenuAction();
        }


        void morefw_Click(object sender, RoutedEventArgs e)
        {
            var sd = (sender as Button).Tag as FFInfo;
            sd.TargetListBox.Items.Clear();
            Service.ListFollowers(new ListFollowersOptions { UserId = sd.User.Id, Cursor = sd.Cursor, Count = (byte)ListFollowersCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                sd.TargetListBox.Dispatch(() =>
                {
                    if (tl.PreviousCursor == 0) return;
                    Button prevfw = new Button();
                    prevfw.Content = "前のユーザー";
                    prevfw.Click += morefw_Click;
                    prevfw.Tag = new FFInfo { TargetListBox = sd.TargetListBox, Cursor = tl.PreviousCursor, User = sd.User };
                    sd.TargetListBox.Items.Add(prevfw);
                });

                foreach (var u in tl)
                {
                    sd.TargetListBox.Dispatch(() =>
                    {
                        sd.TargetListBox.Items.Add(CreateUserPanel(u));
                    });
                }


                sd.TargetListBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefw = new Button();
                    morefw.Content = "さらに表示";
                    morefw.Click += morefw_Click;
                    morefw.Tag = new FFInfo { Cursor = tl.NextCursor, User = sd.User, TargetListBox = sd.TargetListBox };
                    sd.TargetListBox.Items.Add(morefw);
                }));
            });
        }

        void morefr_Click(object sender, RoutedEventArgs e)
        {
            var sd = (sender as Button).Tag as FFInfo;
            sd.TargetListBox.Items.Clear();
            Service.ListFriends(new ListFriendsOptions { UserId = sd.User.Id, Cursor = sd.Cursor, Count = (byte)ListFollowersCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                sd.TargetListBox.Dispatch(() =>
                {
                    if (tl.PreviousCursor == 0) return;
                    Button prevfw = new Button();
                    prevfw.Content = "前のユーザー";
                    prevfw.Click += morefr_Click;
                    prevfw.Tag = new FFInfo { TargetListBox = sd.TargetListBox, Cursor = tl.PreviousCursor, User = sd.User };
                    sd.TargetListBox.Items.Add(prevfw);
                });

                foreach (var u in tl)
                {
                    sd.TargetListBox.Dispatch(() =>
                    {
                        sd.TargetListBox.Items.Add(CreateUserPanel(u));
                    });
                }


                sd.TargetListBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefw = new Button();
                    morefw.Content = "さらに表示";
                    morefw.Click += morefr_Click;
                    morefw.Tag = new FFInfo { Cursor = tl.NextCursor, User = sd.User, TargetListBox = sd.TargetListBox };
                    sd.TargetListBox.Items.Add(morefw);
                }));
            });
        }



        private async void Search(string text)
        {
            await Task.Run(() =>
            {
                this.Dispatch(async () =>
                    {
                        TabItem ti = new TabItem();

                        StackPanel h = new StackPanel();
                        h.Orientation = Orientation.Horizontal;
                        TextBlock tx = new TextBlock { Text = String.Format(MessageHeaderSearchResultFormat, text) };
                        Button cl = new Button();
                        cl.Margin = new Thickness(2);
                        cl.Content = new TextBlock { FontFamily = new FontFamily("Marlett"), FontSize = 7, Text = "r" };
                        cl.Tag = ti;
                        cl.Click += cl_Click;
                        cl.Template = GetTemplate("FlatButton");
                        cl.Background = Brushes.LightGray;
                        h.Children.Add(tx);
                        h.Children.Add(cl);
                        ti.Header = h;

                        ListBox lb = new ListBox();
                        lb.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                        lb.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                        lb.SetValue(ScrollViewer.CanContentScrollProperty, false);
                        ti.Content = lb;
                        MainTab.Items.Add(ti);
                        MainTab.SelectedItem = ti;
                        foreach (var s in await SearchTweet(text, SearchCount))
                        {
                            lb.Dispatch(() =>
                            {
                                if (s.RetweetedStatus != null)
                                {
                                    //公式RTが飛んできた
                                    var rto = s.RetweetedStatus;
                                    lb.Items.Add(CreateRetweetPanel(rto, s));
                                }
                                else
                                {
                                    lb.Items.Add(CreateTweetPanel(s));
                                }
                            });

                        }

                    });
            });
        }



        private void ButtonMini_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void ButtonState_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (WindowState)
            {
                case WindowState.Maximized:
                    RootBorder.Margin = new Thickness(6);
                    StateChangeButton.Content = "2";
                    break;
                case WindowState.Normal:
                    RootBorder.Margin = new Thickness(0);
                    StateChangeButton.Content = "1";
                    break;
                default:
                    break;
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TasktrayIcon.Visible = false;
            TasktrayIcon.Dispose();
            Service.CancelStreaming();
            Application.Current.Shutdown();
        }

        private void MenuInfo_Click(object sender, RoutedEventArgs e)
        {
            (new VersionWindow()).ShowDialog();
        }

        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EasyTweetText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Service.Tweet(EasyTweetText.Text, (ts, res) =>
                {
                    SetStatusBarWithSendTweet(res, ts);
                });
                EasyTweetText.Text = "";
            }
        }
        private void EasyTweetText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EasyTweetText.Text.Length > 140)
            {
                EasyTweetText.Background = Brushes.Red;
            }
            else
            {
                EasyTweetText.Background = Brushes.White;
            }
        }


        private void SearchText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Search(SearchText.Text);
                SearchText.Text = "";
            }
        }

        private void TextBoxTweet_TextChanged(object sender, TextChangedEventArgs e)
        {
            TweetRest.Text = (140 - TextBoxTweet.Text.Length).ToString() + "　　";
        }

        private void Tweet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (UploadImagePath.Count == 0)
            {
                TweetWithButtonChanger(TextBoxTweet.Text, true);
            }
            else
            {
                MediaTweetWithButtonChanger(TextBoxTweet.Text, true);

            }
        }

        private void FollowButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (FollowType)
            {
                case UserStateKind.Me:
                case UserStateKind.Blocking:
                case UserStateKind.Blocked:
                    break;
                case UserStateKind.Following:
                case UserStateKind.FFed:
                    Service.UnfollowUser(new UnfollowUserOptions { UserId = ViewingUser.Id }, (t, r) =>
                    {
                        //TwitterState.Content = r.ToString();
                    });
                    SetUserStates();
                    break;
                case UserStateKind.Followed:
                case UserStateKind.None:
                    Service.FollowUser(new FollowUserOptions { UserId = ViewingUser.Id }, (t, r) =>
                    {
                        //TwitterState.Content = r.ToString();
                    });
                    SetUserStates();
                    break;
                default:
                    break;
            }
        }

        private void UserUri_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(UserUri.NavigateUri.ToString());
        }

        private void UserTweets_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Dispatch(() =>
            {
                TabItem ti = new TabItem();

                StackPanel h = new StackPanel();
                h.Orientation = Orientation.Horizontal;
                TextBlock tx = new TextBlock { Text = String.Format(MessageHeaderUserTweetsFormat, ViewingUser.ScreenName) };
                Button cl = new Button();
                cl.Margin = new Thickness(2);
                cl.Content = new TextBlock { FontFamily = new FontFamily("Marlett"), FontSize = 7, Text = "r" };
                cl.Tag = ti;
                cl.Click += cl_Click;
                cl.Template = GetTemplate("FlatButton");
                cl.Background = Brushes.LightGray;
                h.Children.Add(tx);
                h.Children.Add(cl);
                ti.Header = h;

                ListBox lb = new ListBox();
                lb.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                lb.SetValue(ScrollViewer.CanContentScrollProperty, false);
                lb.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                ti.Content = lb;
                Service.ListTweetsOnUserTimeline(new ListTweetsOnUserTimelineOptions { UserId = ViewingUser.Id, Count = ListTweetCount }, (tsl, res) =>
                {
                    //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                    lb.Dispatch(() =>
                    {
                        foreach (var p in tsl)
                        {
                            //なんか公式RTめんどい
                            if (p.RetweetedStatus != null)
                            {
                                //公式RTが飛んできた
                                var rto = p.RetweetedStatus;
                                lb.Items.Add(CreateRetweetPanel(rto, p));
                            }
                            else
                            {
                                lb.Items.Add(CreateTweetPanel(p));
                            }
                        }
                    });
                });
                MainTab.Items.Add(ti);
                MainTab.SelectedItem = ti;
            });
        }

        void cl_Click(object sender, RoutedEventArgs e)
        {
            MainTab.Items.Remove((sender as Button).Tag as TabItem);
        }


        private void UserFavorites_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.Dispatch(() =>
            {
                TabItem ti = new TabItem();

                StackPanel h = new StackPanel();
                h.Orientation = Orientation.Horizontal;
                TextBlock tx = new TextBlock { Text = String.Format(MessageHeaderUserFavoritesFormat, ViewingUser.ScreenName) };
                Button cl = new Button();
                cl.Margin = new Thickness(2);
                cl.Content = new TextBlock { FontFamily = new FontFamily("Marlett"), FontSize = 7, Text = "r" };
                cl.Tag = ti;
                cl.Click += cl_Click;
                cl.Template = GetTemplate("FlatButton");
                cl.Background = Brushes.LightGray;
                h.Children.Add(tx);
                h.Children.Add(cl);
                ti.Header = h;

                ListBox lb = new ListBox();
                lb.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                lb.SetValue(ScrollViewer.CanContentScrollProperty, false);
                lb.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                ti.Content = lb;
                Service.ListFavoriteTweets(new ListFavoriteTweetsOptions { UserId = ViewingUser.Id, Count = ListFavoriteCount }, (tsl, res) =>
                {
                    //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                    lb.Dispatch(() =>
                    {
                        foreach (var p in tsl)
                        {
                            //なんか公式RTめんどい
                            if (p.RetweetedStatus != null)
                            {
                                //公式RTが飛んできた
                                var rto = p.RetweetedStatus;
                                lb.Items.Add(CreateRetweetPanel(rto, p));
                            }
                            else
                            {
                                lb.Items.Add(CreateTweetPanel(p));
                            }
                        }
                    });
                });
                MainTab.Items.Add(ti);
                MainTab.SelectedItem = ti;
            });
            /*
            TweetsTab.Dispatcher.BeginInvoke(new Action(() =>
            {
                TweetsTab.Header = String.Format(MessageHeaderUserFavoritesFormat, ViewingUser.ScreenName, ListFavoriteCount);
                TweetsTab.IsSelected = true;
            }));
            Service.ListFavoriteTweets(new ListFavoriteTweetsOptions { UserId = ViewingUser.Id, Count = ListFavoriteCount }, (tsl, res) =>
            {
                //SetStatusBarWithFavorite()
                TweetsList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    TweetsList.Items.Clear();
                    foreach (var p in tsl)
                    {
                        //なんか公式RTめんどい
                        if (p.RetweetedStatus != null)
                        {
                            //公式RTが飛んできた
                            var rto = p.RetweetedStatus;
                            TweetsList.Items.Add(CreateRetweetPanel(rto, p));
                        }
                        else
                        {
                            TweetsList.Items.Add(CreateTweetPanel(p));
                        }
                    }
                }));
            });
            */
        }


        private void UserTweets_MouseEnter(object sender, MouseEventArgs e)
        {
            UserTweets.Foreground = Brushes.Red;
        }

        private void UserTweets_MouseLeave(object sender, MouseEventArgs e)
        {
            UserTweets.Foreground = Brushes.DarkBlue;
        }

        private void UserFavorites_MouseEnter(object sender, MouseEventArgs e)
        {
            UserFavorites.Foreground = Brushes.Red;
        }

        private void UserFavorites_MouseLeave(object sender, MouseEventArgs e)
        {
            UserFavorites.Foreground = Brushes.DarkBlue;
        }

        private void UserFollower_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListFollowers();
        }

        private void UserFollower_MouseEnter(object sender, MouseEventArgs e)
        {
            UserFollower.Foreground = Brushes.Red;
        }

        private void UserFollower_MouseLeave(object sender, MouseEventArgs e)
        {
            UserFollower.Foreground = Brushes.DarkBlue;
        }

        private void UserFollow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListFollowings();
        }

        private void UserFollow_MouseEnter(object sender, MouseEventArgs e)
        {
            UserFollow.Foreground = Brushes.Red;
        }

        private void UserFollow_MouseLeave(object sender, MouseEventArgs e)
        {
            UserFollow.Foreground = Brushes.DarkBlue;
        }

        private void M_View_ResetUserPanel_Click(object sender, RoutedEventArgs e)
        {
            ViewingUser = CurrentUser;
            RefreshUserInfo();
        }

        private void M_View_AllowWrap_Click(object sender, RoutedEventArgs e)
        {
            if (M_View_AllowWrap.IsChecked)
            {
                MainTimeline.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
                MainMention.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            }
            else
            {
                MainTimeline.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                MainMention.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            }
        }

        private void ShowDMWindow_Click(object sender, RoutedEventArgs e)
        {
            new DirectMessage(this).Show();
        }

        private void TB_RefreshUser_Click(object sender, RoutedEventArgs e)
        {
            Service.GetUserProfileFor(new GetUserProfileForOptions { IncludeEntities = true, UserId = ViewingUser.Id }, (u, r) =>
            {
                //TwitterState.Content = r.ToString();
                if (CurrentUser.Id == ViewingUser.Id) CurrentUser = u;
                ViewingUser = u;
                RefreshUserInfo();
            });
        }

        private void M_Setting_Click(object sender, RoutedEventArgs e)
        {
            new SettingWindow(this).ShowDialog();
        }



        private void UserSpamButton_Click(object sender, RoutedEventArgs e)
        {
            Service.ReportSpam(new ReportSpamOptions { UserId = ViewingUser.Id }, (u, res) =>
            {
                RefreshUserInfo();
            });
        }

        private void UserBlockButton_Click(object sender, RoutedEventArgs e)
        {
            Service.BlockUser(new BlockUserOptions { UserId = ViewingUser.Id }, (u, res) =>
            {
                Service.ListBlockedUsers(new ListBlockedUsersOptions { }, (tu, res2) =>
                {
                    //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                    BlockingUsers = tu;
                    RefreshUserInfo();
                });

            });
        }

        private void UserListButton_Click(object sender, RoutedEventArgs e)
        {
            new ListWindow(this, ViewingUser).ShowDialog();
        }

        public IEnumerable<string> GetFollowingMembersStartsWith(string st)
        {
            return new List<string>();
        }

        private void M_Lists_Click(object sender, RoutedEventArgs e)
        {
            new ExListWindow(this).Show();
        }

        private void TextBoxTweet_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                TweetWithButtonChanger(TextBoxTweet.Text, true);
            }
        }

        private void MainTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void MainTimeline_KeyDown(object sender, KeyEventArgs e)
        {
            if (MainTimeline.SelectedIndex == -1) return;
            var s = (MainTimeline.SelectedItem as StackPanel).Children;
            switch (e.Key)
            {
                case Key.R:
                    if (((MainTimeline.SelectedItem as StackPanel).Tag as TwitterStatus).User == CurrentUser) return;
                    var b = (((s[s.Count - 1] as StackPanel).Children[0]) as ToggleButton);
                    b.IsChecked = !b.IsChecked;
                    rt_Click(b, e);
                    break;
                case Key.F:
                    var b2 = (((s[s.Count - 1] as StackPanel).Children[1]) as ToggleButton);
                    b2.IsChecked = !b2.IsChecked;
                    fav_Click(b2, e);
                    break;
            }
        }

        private void AI_Draw_Click(object sender, RoutedEventArgs e)
        {
            var nt = DateTime.Now;
            new TegakiDrawWindow(this, nt).Show();
        }

        private void AI_Load_Click(object sender, RoutedEventArgs e)
        {
            if (UploadImageDialog.ShowDialog(this) == true)
            {
                var t = UploadImageDialog.FileNames;
                foreach (var i in t)
                {
                    if (!UploadImagePath.ContainsKey(i))
                    {
                        var stream = new FileStream(i, FileMode.Open);
                        UploadImagePath.Add("image", stream);
                        IncludeImageList.Children.Add(CreateUploadImageText(i, stream));
                    }
                }
            }
        }

        public UIElement CreateUploadImageText(string p, FileStream st)
        {
            StackPanel s = new StackPanel();
            s.Orientation = Orientation.Horizontal;
            s.Tag = st;
            TextBlock t = new TextBlock { Text = p, TextWrapping = TextWrapping.Wrap };
            Button b = new Button { Content = "×", Tag = s };
            b.Click += b_Click;
            s.Children.Add(b);
            s.Children.Add(t);
            return s;
        }

        void b_Click(object sender, RoutedEventArgs e)
        {
            var st = ((sender as Button).Tag as StackPanel).Tag as FileStream;
            UploadImagePath.Remove(UploadImagePath.Where(p => p.Value == st).First().Key);
            IncludeImageList.Children.Remove((sender as Button).Tag as StackPanel);
        }

        private void M_Reboot_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            Process.Start(Environment.GetCommandLineArgs()[0]);
        }

    }



    public static class Twiex
    {


        public static void SaveToFile<T>(this T obj, string name)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (FileStream fs = new FileStream(name, FileMode.Create))
            {
                dcs.WriteObject(fs, obj);
            }
        }

        public static void ReadFromFile<T>(string name, out T obj)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (FileStream fs = new FileStream(name, FileMode.Open))
            {
                obj = (T)dcs.ReadObject(fs);
            }
        }
    }
}
