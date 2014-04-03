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
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using ToolTipIcon = System.Windows.Forms.ToolTipIcon;
using Icon = System.Drawing.Icon;
using TweetSharp;
using System.Media;
namespace KbtterWPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MediaPlayer Player { get; set; }

        public AuthorizeWindow Authorize { get; protected set; }
        public NotifyIcon TasktrayIcon { get; set; }
        /// <summary>
        /// Twitterサービスを保持
        /// </summary>
        public TwitterService Service { get; set; }

        public ScriptEngine IronPythonEngine { get; set; }

        public ScriptScope Scope { get; set; }

        public List<IKbtterOnTweet> TweetPlugins { get; set; }
        public List<IKbtterOnFavorite> FavoritePlugins { get; set; }
        public List<IKbtterOnUnfavorite> UnfavoritePlugins { get; set; }
        public List<IKbtterOnRetweet> RetweetPlugins { get; set; }
        public List<IKbtterOnInitialize> InitializePlugins { get; set; }
        public List<IKbtterCallable> WindowPlugins { get; set; }

        public UserStateKind FollowType { get; set; }
        public TwitterCursorList<TwitterUser> BlockingUsers { get; set; }

        public Config Settings { get; set; }

        public IObservable<TwitterStreamArtifact> Stream { get; set; }

        public Config LanguageSetting { get; set; }

        #region 定数・メッセージ

        public int TimelineMax { get; set; }
        public int MentionMax { get; set; }
        public int SearchCount { get; set; }
        public int PreloadTimelineCount { get; set; }
        public int ListTweetCount { get; set; }
        public int ListFollowingCount { get; set; }
        public int ListFollowersCount { get; set; }
        public int ListFavoriteCount { get; set; }

        public string MessageStateYou { get; set; }
        public string MessageStateFFed { get; set; }
        public string MessageStateBlocking { get; set; }
        public string MessageStateBlocked { get; set; }
        public string MessageStateFollowing { get; set; }
        public string MessageStateFollowed { get; set; }
        public string MessageStateNone { get; set; }

        public string MessageFollowButtonYou { get; set; }
        public string MessageFollowButtonFollow { get; set; }
        public string MessageFollowButtonUnfollow { get; set; }
        public string MessageFollowButtonUnblock { get; set; }
        public string MessageFollowButtonCantFollow { get; set; }

        public string MessageInfoUserFormat { get; set; }
        public string MessageInfoRetweetFormat { get; set; }
        public string MessageInfoDeleted { get; set; }
        public string MessageInfoNotifyRetweet { get; set; }
        public string MessageInfoNotifyDelete { get; set; }
        public string MessageInfoRetweetButtonText { get; set; }
        public string MessageInfoFavoriteButtonText { get; set; }
        public string MessageInfoDescriptionButtonText { get; set; }
        public string MessageInfoDeleteButtonText { get; set; }

        public string MessageHeaderSearchResultFormat { get; set; }
        public string MessageHeaderUserTweetsFormat { get; set; }
        public string MessageHeaderUserFollowingFormat { get; set; }
        public string MessageHeaderUserFollowerFormat { get; set; }
        public string MessageHeaderUserFavoritesFormat { get; set; }

        public string MessageMentionFavoritedFormat { get; set; }
        public string MessageMentionUnfavoritedFormat { get; set; }
        public string MessageMentionRetweetedFormat { get; set; }
        public string MessageDMReceivedFormat { get; set; }

        public string MessagePluginLoadingErrorText { get; set; }
        public string MessagePluginLoadingErrorCaption { get; set; }

        public string NotifySoundReply { get; set; }
        public string NotifySoundRetweeted { get; set; }
        public string NotifySoundFavorited { get; set; }
        public string NotifySoundUnfavorited { get; set; }
        public string NotifySoundDMReceived { get; set; }

        public SoundPlayer NotifySoundReplyPlayer { get; set; }
        public SoundPlayer NotifySoundRetweetedPlayer { get; set; }
        public SoundPlayer NotifySoundFavoritedPlayer { get; set; }
        public SoundPlayer NotifySoundUnfavoritedPlayer { get; set; }
        public SoundPlayer NotifySoundDMReceivedPlayer { get; set; }
        #endregion

        /// <summary>
        /// メインのユーザー
        /// </summary>
        public TwitterUser CurrentUser { get; set; }

        /// <summary>
        /// 情報欄に表示してるユーザー
        /// </summary>
        public TwitterUser ViewingUser { get; set; }

        public TwitterUser ListingUser { get; set; }


        public OAuthRequestToken RequestToken { get; protected set; }
        public OAuthAccessToken AccessToken { get; protected set; }
        public string AccessTokenPath { get; protected set; }

        private static string ConsumerKey { get { return "fV3meTB3URhtSx7WGjQ"; } }
        private static string ConsumerSecret { get { return "3AVAf20e64Al9edgrrJnJjI5a67fp2WUPxP9xtnLsY"; } }


        public MainWindow()
        {
            InitializeComponent();
            TasktrayIcon = new NotifyIcon
            {
                Text = "Kbtter",
                Visible = true,
                Icon = new Icon("trayicon.ico")
            };
            Authorize = new AuthorizeWindow();
            AccessTokenPath = "user.ac";
            Player = new MediaPlayer();
            IronPythonEngine = Python.CreateEngine();
            IronPythonEngine.Runtime.LoadAssembly(this.GetType().Assembly);
            IronPythonEngine.Runtime.LoadAssembly(Assembly.LoadFrom("TweetSharp.dll"));



            TweetPlugins = new List<IKbtterOnTweet>();
            RetweetPlugins = new List<IKbtterOnRetweet>();
            FavoritePlugins = new List<IKbtterOnFavorite>();
            UnfavoritePlugins = new List<IKbtterOnUnfavorite>();
            InitializePlugins = new List<IKbtterOnInitialize>();
            WindowPlugins = new List<IKbtterCallable>();

            Settings = new Config();
        }

        public void ShowBalloon(string title, string text, ToolTipIcon icon)
        {
            TasktrayIcon.BalloonTipIcon = icon;
            TasktrayIcon.BalloonTipText = text;
            TasktrayIcon.BalloonTipTitle = title;
            TasktrayIcon.ShowBalloonTip(10000);
        }



        private void MainPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfig();
            //OAuth認証とか
            Service = new TwitterService(ConsumerKey, ConsumerSecret);
            RequestToken = Service.GetRequestToken();

            if (!File.Exists(AccessTokenPath))
            {
                var requri = Service.GetAuthorizationUri(RequestToken);
                Process.Start(requri.ToString());
                Authorize.ShowDialog();

                AccessToken = Service.GetAccessToken(RequestToken, Authorize.PIN);
                AccessToken.SaveToFile(AccessTokenPath);
            }
            OAuthAccessToken atk;
            Twiex.ReadFromFile<OAuthAccessToken>(AccessTokenPath, out atk);
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

        void EventAction(TwitterUserStreamEvent p)
        {
            Console.WriteLine(p.ToString());
            switch (p.Event)
            {
                case "favorite":
                    var fs = p.TargetObject as TwitterStatus;
                    if (p.Source == CurrentUser) break;
                    MainMention.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainMention.Items.Insert(0, CreateFavoritePanel(fs, p.Source));
                    }));
                    NotifyFavorited(p);
                    foreach (var pl in FavoritePlugins) pl.OnFavorite(Service, p.Source, fs);
                    break;
                case "unfavorite":
                    var fs2 = p.TargetObject as TwitterStatus;
                    if (p.Source == CurrentUser) break;
                    MainMention.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainMention.Items.Insert(0, CreateUnfavoritePanel(fs2, p.Source));
                    }));
                    NotifyUnfavorited(p);
                    foreach (var pl in UnfavoritePlugins) pl.OnUnfavorite(Service, p.Source, fs2);
                    break;
                default:
                    break;
            }
            if (MainMention.Items.Count > MentionMax)
            {
                MainMention.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainMention.Items.RemoveAt(MentionMax);
                }));
            }
        }

        void DMAction(TwitterUserStreamDirectMessage p)
        {
            if (p.DirectMessage.Recipient == CurrentUser)
            {
                NotifyDirectMessage(p.DirectMessage);
            }
        }

        void StatusAction(TwitterUserStreamStatus p)
        {
            MainTimeline.Dispatcher.BeginInvoke(new Action(() =>
            {

                if (p.Status.RetweetedStatus != null)
                {
                    //自分がRTした以外のRT
                    if (p.Status.User == CurrentUser) return;
                    if (p.Status.RetweetedStatus.User == CurrentUser)
                    {
                        //自分のがRTされて飛んだきた
                        MainMention.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MainMention.Items.Insert(0, CreateRetweetedPanel(p.Status.RetweetedStatus, p.Status.User));
                        }));
                        NotifyRetweeted(p.Status.RetweetedStatus, p.Status.User);
                        foreach (var pl in RetweetPlugins) pl.OnRetweet(Service, p.Status.User, p.Status.RetweetedStatus);
                    }
                    else
                    {
                        //他人の公式RTが飛んできた
                        var rto = p.Status.RetweetedStatus;
                        MainTimeline.Items.Insert(0, CreateRetweetPanel(rto, p.Status));
                    }
                }
                else
                {
                    //普通のRT
                    MainTimeline.Items.Insert(0, CreateTweetPanel(p.Status));
                    if (p.Status.Entities.Mentions.Count != 0)
                    {

                    }
                    if (Regex.Match(p.Status.TextDecoded, "@" + CurrentUser.ScreenName).Success)
                    {
                        MainMention.Items.Insert(0, CreateTweetPanel(p.Status));
                        NotifyReplied(p.Status);
                    }
                }

                foreach (var pt in TweetPlugins)
                {
                    pt.OnTweet(Service, CurrentUser, p.Status);
                }
                if (MainTimeline.Items.Count > TimelineMax)
                {
                    MainTimeline.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainTimeline.Items.RemoveAt(TimelineMax);
                    }));
                }
                if (MainMention.Items.Count > MentionMax)
                {
                    MainMention.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainMention.Items.RemoveAt(MentionMax);
                    }));
                }

            }));
        }

        void m_Click(object sender, RoutedEventArgs e)
        {
            var s = (sender as MenuItem).Tag as KbtterPluginMenu;
            s.MenuAction();
        }

        private void LoadPlugins()
        {
            if (!Directory.Exists("plugin")) Directory.CreateDirectory("plugin");
            var fls = Directory.GetFiles("plugin/", "*.py");

            foreach (var f in fls)
            {
                try
                {
                    ScriptScope sc = IronPythonEngine.CreateScope();
                    var ss = IronPythonEngine.CreateScriptSourceFromFile(f);
                    ss.Execute(sc);
                    var t = IronPythonEngine.Execute(System.IO.Path.GetFileNameWithoutExtension(f) + "()", sc);
                    if (t is IKbtterOnTweet) TweetPlugins.Add(t);
                    if (t is IKbtterOnFavorite) FavoritePlugins.Add(t);
                    if (t is IKbtterOnUnfavorite) UnfavoritePlugins.Add(t);
                    if (t is IKbtterOnRetweet) RetweetPlugins.Add(t);
                    if (t is IKbtterOnInitialize) InitializePlugins.Add(t);
                    if (t is IKbtterCallable) WindowPlugins.Add(t);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(MessagePluginLoadingErrorText + "\n" + ex.Message, MessagePluginLoadingErrorCaption);
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Application.Current.Shutdown();
                    }));

                }
            }
        }

        private void LoadConfig()
        {
            if (!File.Exists("Kbtter.cfg"))
            {
                Settings = GetDefaultConfig();
                Settings.SaveFile("Kbtter.cfg");
            }
            Settings.LoadFile("Kbtter.cfg");

            PreloadTimelineCount = (int)Settings["Kbtter.General.PreloadTimeline"].NumberValue;
            TimelineMax = (int)Settings["Kbtter.General.TimelineMax"].NumberValue;
            MentionMax = (int)Settings["Kbtter.General.MentionMax"].NumberValue;
            ListTweetCount = (int)Settings["Kbtter.General.UserTweetsMax"].NumberValue;
            ListFollowingCount = (int)Settings["Kbtter.General.UserFollowingShow"].NumberValue;
            ListFollowersCount = (int)Settings["Kbtter.General.UserFollowersShow"].NumberValue;
            ListFavoriteCount = (int)Settings["Kbtter.General.UserFavoritesMax"].NumberValue;
            SearchCount = (int)Settings["Kbtter.General.SearchCount"].NumberValue;

            NotifySoundReply = Settings["Kbtter.Notifycation.ReplySound"].StringValue;
            NotifySoundRetweeted = Settings["Kbtter.Notifycation.RetweetedSound"].StringValue;
            NotifySoundFavorited = Settings["Kbtter.Notifycation.FavoritedSound"].StringValue;
            NotifySoundUnfavorited = Settings["Kbtter.Notifycation.UnfavoritedSound"].StringValue;
            NotifySoundDMReceived = Settings["Kbtter.Notifycation.DMReceivedSound"].StringValue;

            if (NotifySoundReply != "") NotifySoundReplyPlayer = new SoundPlayer(NotifySoundReply);
            if (NotifySoundRetweeted != "") NotifySoundRetweetedPlayer = new SoundPlayer(NotifySoundRetweeted);
            if (NotifySoundFavorited != "") NotifySoundFavoritedPlayer = new SoundPlayer(NotifySoundFavorited);
            if (NotifySoundUnfavorited != "") NotifySoundUnfavoritedPlayer = new SoundPlayer(NotifySoundUnfavorited);
            if (NotifySoundDMReceived != "") NotifySoundDMReceivedPlayer = new SoundPlayer(NotifySoundDMReceived);

            LoadMessages(Settings["Kbtter.Environment.MessageFile"].StringValue);
        }

        private void LoadMessages(string file)
        {
            LanguageSetting = new Config();
            LanguageSetting.LoadFile(file);
            MessageStateYou = LanguageSetting["Kbtter.Message.FollowState.You"].StringValue;
            MessageStateFFed = LanguageSetting["Kbtter.Message.FollowState.FFed"].StringValue;
            MessageStateBlocking = LanguageSetting["Kbtter.Message.FollowState.Blocking"].StringValue;
            MessageStateBlocked = LanguageSetting["Kbtter.Message.FollowState.Blocked"].StringValue;
            MessageStateFollowing = LanguageSetting["Kbtter.Message.FollowState.Following"].StringValue;
            MessageStateFollowed = LanguageSetting["Kbtter.Message.FollowState.Followed"].StringValue;
            MessageStateNone = LanguageSetting["Kbtter.Message.FollowState.None"].StringValue;

            MessageFollowButtonYou = LanguageSetting["Kbtter.Message.FollowButton.You"].StringValue;
            MessageFollowButtonFollow = LanguageSetting["Kbtter.Message.FollowButton.Follow"].StringValue;
            MessageFollowButtonUnfollow = LanguageSetting["Kbtter.Message.FollowButton.Unfollow"].StringValue;
            MessageFollowButtonUnblock = LanguageSetting["Kbtter.Message.FollowButton.Unblock"].StringValue;
            MessageFollowButtonCantFollow = LanguageSetting["Kbtter.Message.FollowButton.Can'tFollow"].StringValue;

            MessageInfoUserFormat = LanguageSetting["Kbtter.Message.Infomation.UserFormat"].StringValue;
            MessageInfoRetweetFormat = LanguageSetting["Kbtter.Message.Infomation.RetweetFormat"].StringValue;
            MessageInfoDeleted = LanguageSetting["Kbtter.Message.Infomation.Deleted"].StringValue;
            MessageInfoRetweetButtonText = LanguageSetting["Kbtter.Message.Infomation.Buttons.Retweet"].StringValue;
            MessageInfoFavoriteButtonText = LanguageSetting["Kbtter.Message.Infomation.Buttons.Favorite"].StringValue;
            MessageInfoDescriptionButtonText = LanguageSetting["Kbtter.Message.Infomation.Buttons.Description"].StringValue;
            MessageInfoDeleteButtonText = LanguageSetting["Kbtter.Message.Infomation.Buttons.Delete"].StringValue;
            MessageInfoNotifyRetweet = LanguageSetting["Kbtter.Message.Infomation.Notify.Retweet"].StringValue;
            MessageInfoNotifyDelete = LanguageSetting["Kbtter.Message.Infomation.Notify.Delete"].StringValue;

            MessageHeaderSearchResultFormat = LanguageSetting["Kbtter.Message.Header.SearchResultFormat"].StringValue;
            MessageHeaderUserTweetsFormat = LanguageSetting["Kbtter.Message.Header.UserTweetsFormat"].StringValue;
            MessageHeaderUserFollowingFormat = LanguageSetting["Kbtter.Message.Header.UserFollowingFormat"].StringValue;
            MessageHeaderUserFollowerFormat = LanguageSetting["Kbtter.Message.Header.UserFollowerFormat"].StringValue;
            MessageHeaderUserFavoritesFormat = LanguageSetting["Kbtter.Message.Header.UserFavoritesFormat"].StringValue;

            MessageMentionFavoritedFormat = LanguageSetting["Kbtter.Message.Mention.FavoritedFormat"].StringValue;
            MessageMentionUnfavoritedFormat = LanguageSetting["Kbtter.Message.Mention.UnfavoritedFormat"].StringValue;
            MessageMentionRetweetedFormat = LanguageSetting["Kbtter.Message.Mention.RetweetedFormat"].StringValue;

            MessageDMReceivedFormat = LanguageSetting["Kbtter.Message.DM.ReceivedFormat"].StringValue;

            MessagePluginLoadingErrorText = LanguageSetting["Kbtter.Message.Plugin.LoadingError.Text"].StringValue;
            MessagePluginLoadingErrorCaption = LanguageSetting["Kbtter.Message.Plugin.LoadingError.Caption"].StringValue;



            //UI要素


            AB_Search.Text = LanguageSetting["Kbtter.UI.AccessBar.Search"].StringValue;
            AB_EasyTweet.Text = LanguageSetting["Kbtter.UI.AccessBar.EasyTweet"].StringValue;

            UV_Tweet.Content = LanguageSetting["Kbtter.UI.UserView.Tweet"].StringValue;
            UVTweets.Content = LanguageSetting["Kbtter.UI.UserView.UserTweets"].StringValue;
            UVFollowing.Content = LanguageSetting["Kbtter.UI.UserView.UserFollowing"].StringValue;
            UVFollowed.Content = LanguageSetting["Kbtter.UI.UserView.UserFollowed"].StringValue;
            UVFavorites.Content = LanguageSetting["Kbtter.UI.UserView.UserFavorites"].StringValue;

            MT_Timeline.Header = LanguageSetting["Kbtter.UI.MainTab.Timeline"].StringValue;
            MT_Mention.Header = LanguageSetting["Kbtter.UI.MainTab.Mention"].StringValue;
            MT_Users.Header = LanguageSetting["Kbtter.UI.MainTab.Users"].StringValue;
            TweetsTab.Header = LanguageSetting["Kbtter.UI.MainTab.Tweets"].StringValue;
            SearchTab.Header = LanguageSetting["Kbtter.UI.MainTab.SearchResult"].StringValue;

        }

        private Config GetDefaultConfig()
        {
            var f = new Config();
            f["Kbtter.General.PreloadTimeline"] = 100;
            f["Kbtter.General.TimelineMax"] = 200;
            f["Kbtter.General.MentionMax"] = 200;
            f["Kbtter.General.UserTweetsMax"] = 200;
            f["Kbtter.General.UserFollowingShow"] = 50;
            f["Kbtter.General.UserFollowersShow"] = 50;
            f["Kbtter.General.UserFavoritesMax"] = 200;
            f["Kbtter.General.SearchCount"] = 100;

            f["Kbtter.Environment.MessageFile"] = "lang/Japanese.cfg";

            f["Kbtter.Notifycation.ReplySound"] = "sound/notify1.wav";
            f["Kbtter.Notifycation.DMReceivedSound"] = "sound/notify1.wav";
            f["Kbtter.Notifycation.RetweetedSound"] = "sound/notify2.wav";
            f["Kbtter.Notifycation.FavoritedSound"] = "sound/notify2.wav";
            f["Kbtter.Notifycation.UnfavoritedSound"] = "";
            return f;
        }

        private void SetDefaultUser()
        {

            if (CurrentUser == null) return;
            ViewingUser = CurrentUser;
            RefreshUserInfo();
            FollowType = UserStateKind.Me;
            Service.ListBlockedUsers(new ListBlockedUsersOptions { }, (tu, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                BlockingUsers = tu;
            });
        }

        public void NotifyFavorited(TwitterUserStreamEvent p)
        {
            var st = p.TargetObject as TwitterStatus;
            ShowBalloon(
                String.Format(MessageMentionFavoritedFormat, p.Source.Name),
                st.TextDecoded,
                ToolTipIcon.Info
                );
            if (NotifySoundFavoritedPlayer != null) NotifySoundFavoritedPlayer.Play();

        }

        public void NotifyUnfavorited(TwitterUserStreamEvent p)
        {
            var st = p.TargetObject as TwitterStatus;
            ShowBalloon(
                String.Format(MessageMentionUnfavoritedFormat, p.Source.Name),
                st.TextDecoded,
                ToolTipIcon.Info
                );
            if (NotifySoundUnfavoritedPlayer != null) NotifySoundUnfavoritedPlayer.Play();
        }

        public void NotifyRetweeted(TwitterStatus st, TwitterUser user)
        {
            ShowBalloon(
                String.Format(MessageMentionRetweetedFormat, user.Name),
                st.TextDecoded,
                ToolTipIcon.Info
                );
            if (NotifySoundRetweetedPlayer != null) NotifySoundRetweetedPlayer.Play();
        }

        public void NotifyReplied(TwitterStatus st)
        {
            ShowBalloon(
                st.User.Name,
                st.TextDecoded,
                ToolTipIcon.Info
                );
            if (NotifySoundReplyPlayer != null) NotifySoundReplyPlayer.Play();
        }

        public void NotifyDirectMessage(TwitterDirectMessage dm)
        {
            ShowBalloon(
                String.Format(MessageDMReceivedFormat, dm.Sender.Name),
                dm.Text,
                ToolTipIcon.Info
                );
            if (NotifySoundDMReceivedPlayer != null) NotifySoundDMReceivedPlayer.Play();
        }

        private void RefreshUserInfo()
        {
            UserImage.Dispatcher.BeginInvoke(new Action(() =>
            {
                BitmapImage bi = new BitmapImage(new Uri(ViewingUser.ProfileImageUrlHttps));
                UserImage.Source = bi;
            }));
            UserName.Dispatcher.BeginInvoke(new Action(() => { UserName.Content = ViewingUser.Name; }));
            UserScreenName.Dispatcher.BeginInvoke(new Action(() => { UserScreenName.Content = ViewingUser.ScreenName; }));
            UserUri.Dispatcher.BeginInvoke(new Action(() =>
            {
                UserUri.Inlines.Clear();
                if (ViewingUser.Url == null) return;
                UserUri.NavigateUri = new Uri(ViewingUser.Url);
                UserUri.Inlines.Add(ViewingUser.Url);
            }));
            UserProfile.Dispatcher.BeginInvoke(new Action(() =>
            {
                UserProfile.Text = "";
                if (ViewingUser.Description == null) return;
                UserProfile.Text = ViewingUser.Description;
            }));
            UserTweets.Dispatcher.BeginInvoke(new Action(() => { UserTweets.Content = ViewingUser.StatusesCount; }));
            UserFollow.Dispatcher.BeginInvoke(new Action(() => { UserFollow.Content = ViewingUser.FriendsCount; }));
            UserFollower.Dispatcher.BeginInvoke(new Action(() => { UserFollower.Content = ViewingUser.FollowersCount; }));
            UserFavorites.Dispatcher.BeginInvoke(new Action(() => { UserFavorites.Content = ViewingUser.FavouritesCount; }));
            SetUserStates();
            if (ViewingUser == CurrentUser)
            {
                UserSpamButton.Dispatch(() => UserSpamButton.Visibility = Visibility.Hidden);
                UserBlockButton.Dispatch(() => UserBlockButton.Visibility = Visibility.Hidden);
            }
            else
            {
                UserSpamButton.Dispatch(() => UserSpamButton.Visibility = Visibility.Visible);
                UserBlockButton.Dispatch(() => UserBlockButton.Visibility = Visibility.Visible);
            }
        }

        public void ListFollowers()
        {
            ListingUser = ViewingUser;
            UsersList.Items.Clear();
            Service.ListFollowers(new ListFollowersOptions { UserId = ListingUser.Id, Count = (byte)ListFollowersCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                foreach (var u in tl)
                {
                    UsersList.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UsersList.Items.Add(CreateUserPanel(u));
                    }));
                }


                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefw = new Button();
                    morefw.Content = "さらに表示";
                    morefw.Click += morefw_Click;
                    morefw.Tag = tl.NextCursor;
                    UsersList.Items.Add(morefw);
                }));
            });
        }

        void morefw_Click(object sender, RoutedEventArgs e)
        {
            var sd = (long)(sender as Button).Tag;
            UsersList.Items.Clear();
            Service.ListFollowers(new ListFollowersOptions { UserId = ListingUser.Id, Cursor = sd, Count = (byte)ListFollowersCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.PreviousCursor == 0) return;
                    Button prevfw = new Button();
                    prevfw.Content = "前のユーザー";
                    prevfw.Click += morefw_Click;
                    prevfw.Tag = tl.PreviousCursor;
                    UsersList.Items.Add(prevfw);
                }));

                foreach (var u in tl)
                {
                    UsersList.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UsersList.Items.Add(CreateUserPanel(u));
                    }));
                }


                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefw = new Button();
                    morefw.Content = "さらに表示";
                    morefw.Click += morefw_Click;
                    morefw.Tag = tl.NextCursor;
                    UsersList.Items.Add(morefw);
                }));
            });
        }

        public void ListFollowings()
        {
            ListingUser = ViewingUser;
            UsersList.Items.Clear();
            Service.ListFriends(new ListFriendsOptions { UserId = ListingUser.Id, Count = (byte)ListFollowingCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                foreach (var u in tl)
                {
                    UsersList.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UsersList.Items.Add(CreateUserPanel(u));
                    }));
                }


                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefr = new Button();
                    morefr.Content = "さらに表示";
                    morefr.Click += morefr_Click;
                    morefr.Tag = tl.NextCursor;
                    UsersList.Items.Add(morefr);
                }));
            });
        }

        void morefr_Click(object sender, RoutedEventArgs e)
        {
            var sd = (long)(sender as Button).Tag;
            UsersList.Items.Clear();
            Service.ListFriends(new ListFriendsOptions { UserId = ListingUser.Id, Cursor = sd, Count = (byte)ListFollowingCount }, (tl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                if (tl == null) return;
                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.PreviousCursor == 0) return;
                    Button prevfr = new Button();
                    prevfr.Content = "前のユーザー";
                    prevfr.Click += morefr_Click;
                    prevfr.Tag = tl.PreviousCursor;
                    UsersList.Items.Add(prevfr);
                }));

                foreach (var u in tl)
                {
                    UsersList.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UsersList.Items.Add(CreateUserPanel(u));
                    }));
                }


                UsersList.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (tl.NextCursor == 0) return;
                    Button morefr = new Button();
                    morefr.Content = "さらに表示";
                    morefr.Click += morefr_Click;
                    morefr.Tag = tl.NextCursor;
                    UsersList.Items.Add(morefr);
                }));
            });
        }

        public enum UserStateKind
        {
            None,
            Blocking,
            Blocked,
            Followed,
            Following,
            FFed,
            Me,
        }

        private void SetUserStates()
        {
            UserState.Dispatcher.BeginInvoke(new Action(() =>
            {
                UserState.Text = "Loading...";
            }));
            if (CurrentUser == ViewingUser)
            {
                SetUserStates(UserStateKind.Me);
            }
            else
            {
                if (BlockingUsers.Exists(p => p == ViewingUser))
                {
                    SetUserStates(UserStateKind.Blocking);
                }

                Service.GetFriendshipInfo(new GetFriendshipInfoOptions { SourceId = CurrentUser.Id.ToString(), TargetId = ViewingUser.Id.ToString() }, (tclfw, res2) =>
                {
                    var fol = tclfw.Relationship.Target;
                    //フォローしてるだけ
                    //されてるだけ
                    //FF
                    var following = fol.FollowedBy;
                    var followed = fol.Following;
                    if (following && followed)
                    {
                        //相互
                        SetUserStates(UserStateKind.FFed);
                    }
                    else if (following && !followed)
                    {
                        //フォローだけ
                        SetUserStates(UserStateKind.Following);
                    }
                    else if (!following && followed)
                    {
                        //されてるだけ
                        SetUserStates(UserStateKind.Followed);
                    }
                    else
                    {
                        //赤の他人
                        SetUserStates(UserStateKind.None);
                    }
                });
            }
        }

        private void SetUserStates(UserStateKind state)
        {
            FollowType = state;
            switch (state)
            {
                case UserStateKind.Me:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateYou;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.Gray;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonYou;
                        FollowState.Foreground = Brushes.Black;
                    }));

                    break;

                case UserStateKind.Blocking:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateBlocking;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.DarkBlue;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonUnblock;
                        FollowState.Foreground = Brushes.White;
                    }));
                    break;

                case UserStateKind.Blocked:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateBlocked;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.Red;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonCantFollow;
                        FollowState.Foreground = Brushes.White;
                    }));
                    break;

                case UserStateKind.Following:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateFollowing;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.Yellow;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonUnfollow;
                        FollowState.Foreground = Brushes.Black;
                    }));
                    break;

                case UserStateKind.Followed:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateFollowed;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.LightBlue;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonFollow;
                        FollowState.Foreground = Brushes.Black;
                    }));
                    break;

                case UserStateKind.FFed:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateFFed;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.Yellow;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonUnfollow;
                        FollowState.Foreground = Brushes.Black;
                    }));
                    break;

                case UserStateKind.None:
                    UserState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UserState.Text = MessageStateNone;
                    }));

                    FollowButton.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowButton.Background = Brushes.LightBlue;
                    }));
                    FollowState.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FollowState.Text = MessageFollowButtonFollow;
                        FollowState.Foreground = Brushes.Black;
                    }));
                    break;

                default:
                    break;
            }
        }

        public void TweetWithButtonChanger(string str, bool f)
        {
            ButtonTweet.Background = Brushes.DarkGray;
            ButtonTweet.MouseLeftButtonDown -= Tweet_MouseLeftButtonDown;
            Service.Tweet(str, (st, rs) =>
            {
                SetStatusBarWithSendTweet(rs, st);
                ButtonTweet.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ButtonTweet.Background = new SolidColorBrush { Color = Color.FromRgb(85, 172, 238) };
                    ButtonTweet.MouseLeftButtonDown += Tweet_MouseLeftButtonDown;
                    SetDefaultUser();
                }));
            });
        }
        public UIElement CreateUserPanel(TwitterUser st)
        {
            StackPanel s = new StackPanel();

            StackPanel sus = new StackPanel();
            sus.Orientation = Orientation.Horizontal;

            TextBlock us = new TextBlock();
            Image im2 = new Image();
            BitmapImage bi = new BitmapImage(new Uri(st.ProfileImageUrlHttps));
            us.Text = String.Format(MessageInfoUserFormat, st.Name, st.ScreenName);
            us.FontSize = 18;
            us.VerticalAlignment = VerticalAlignment.Center;
            im2.Source = bi;
            im2.Width = 36;
            im2.Height = 36;
            im2.MouseDown += im2_MouseDown;
            im2.Tag = s;
            s.Tag = st;

            sus.Children.Add(im2);
            sus.Children.Add(us);
            s.Children.Add(sus);

            return s;
        }

        void im2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var st = ((sender as Image).Tag as StackPanel).Tag as TwitterUser;
            ViewingUser = st;
            RefreshUserInfo();
        }

        public UIElement CreateTweetPanel(TwitterStatus st)
        {
            StackPanel s = new StackPanel();
            s.Margin = new Thickness(2);
            StackPanel sus = new StackPanel();
            sus.Orientation = Orientation.Horizontal;

            TextBlock te = new TextBlock();
            TextBlock us = new TextBlock();
            TextBlock ti = new TextBlock();
            Image im = new Image();
            BitmapImage bi = new BitmapImage(new Uri(st.User.ProfileImageUrlHttps));
            us.Text = String.Format(MessageInfoUserFormat, st.User.Name, st.User.ScreenName);
            us.FontSize = 18;
            us.VerticalAlignment = VerticalAlignment.Center;
            im.Source = bi;
            im.Width = 36;
            im.Height = 36;
            im.MouseDown += im_MouseDown;
            im.Tag = s;

            sus.Children.Add(im);
            sus.Children.Add(us);


            te.Text = st.GetUrlConvertedStatusText();
            te.TextWrapping = TextWrapping.WrapWithOverflow;

            ti.FontSize = 10;
            var dt = st.CreatedDate;
            //日本時間 UTC +09:00
            dt = dt.AddHours(9);
            ti.Text = dt.ToString();

            var urls = new StackPanel();
            foreach (var u in st.Entities.Urls)
            {
                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedValue);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                urls.Children.Add(new Label { Content = h });
            }

            foreach (var u in st.Entities.Media)
            {

                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedUrl);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                var ll = new Label { Content = h };

                UIElement i;
                if (TryGetMediaControl(u.MediaUrlHttps, out i))
                {
                    Expander e = new Expander();
                    e.HorizontalAlignment = HorizontalAlignment.Left;
                    e.Header = ll;
                    e.Content = i;
                    urls.Children.Add(e);
                }
                else
                {
                    urls.Children.Add(ll);
                }
            }

            var hashs = new StackPanel();
            foreach (var u in st.Entities.HashTags)
            {
                var t = new TextBlock();
                t.Text = "#" + u.Text;
                t.Foreground = Brushes.Red;
                t.MouseLeftButtonDown += t_MouseLeftButtonDown;
                hashs.Children.Add(t);
            }

            var fbt = GetTemplate("FlatButton");
            var ftbt = GetTemplate("FlatToggleButton");
            StackPanel bts = new StackPanel();
            bts.Orientation = Orientation.Horizontal;
            ToggleButton rt = new ToggleButton(), fav = new ToggleButton();
            Button del = new Button();
            del.Content = MessageInfoDeleteButtonText;
            del.Tag = s;
            del.Click += del_Click;
            del.Template = fbt;
            del.Background = Brushes.LightGray;

            rt.Content = MessageInfoRetweetButtonText;
            rt.Click += rt_Click;
            rt.Tag = new RetweetData { Original = st };
            rt.Template = ftbt;
            rt.Background = Brushes.LightGray;

            fav.Content = MessageInfoFavoriteButtonText;
            fav.Click += fav_Click;
            fav.Tag = s;
            fav.Template = ftbt;
            fav.Background = Brushes.LightGray;

            if (st.User == CurrentUser)
            {
                bts.Children.Add(del);
            }
            else
            {
                bts.Children.Add(rt);
            }
            bts.Children.Add(fav);

            Button descb = new Button();
            descb.Content = MessageInfoDescriptionButtonText;
            descb.Tag = st;
            descb.Click += descb_Click;
            descb.Template = fbt;
            descb.Background = Brushes.LightGray;

            bts.Children.Add(descb);

            s.Children.Add(sus);
            s.Children.Add(te);
            s.Children.Add(urls);
            s.Children.Add(hashs);
            s.Children.Add(ti);
            s.Children.Add(bts);
            s.Tag = st;
            s.Background = Brushes.LightCyan;
            s.HorizontalAlignment = HorizontalAlignment.Stretch;

            return s;
        }

        void h_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        void descb_Click(object sender, RoutedEventArgs e)
        {
            var st = (sender as Button).Tag as TwitterStatus;
            new Description(this, st).ShowDialog();
        }

        public bool TryGetMediaControl(string url, out UIElement ue)
        {
            switch (url.Split('/').Last().Split('.').Last())
            {
                case "jpg":
                case "jpeg":
                case "png":
                case "gif":
                case "bmp":
                    ue = new Image
                    {
                        Source = new BitmapImage(new Uri(url)),
                        Stretch = Stretch.Uniform,
                        Height = 300,
                        HorizontalAlignment = HorizontalAlignment.Left,
                    };
                    return true;

                default:
                    ue = null;
                    return false;
            }
        }

        public UIElement CreateRetweetPanel(TwitterStatus ost, TwitterStatus st)
        {
            StackPanel s = new StackPanel();
            s.Margin = new Thickness(2);
            StackPanel sus = new StackPanel();
            sus.Orientation = Orientation.Horizontal;

            TextBlock te = new TextBlock();
            TextBlock us = new TextBlock();
            TextBlock ti = new TextBlock();
            TextBlock inf = new TextBlock();
            inf.FontSize = 12;
            inf.Text = String.Format(MessageInfoRetweetFormat, st.User.Name);
            inf.Foreground = Brushes.DarkGreen;
            Image im = new Image();
            BitmapImage bi = new BitmapImage(new Uri(ost.User.ProfileImageUrlHttps));
            us.Text = String.Format(MessageInfoUserFormat, ost.User.Name, ost.User.ScreenName);
            us.FontSize = 18;
            us.VerticalAlignment = VerticalAlignment.Center;
            us.Foreground = Brushes.DarkGreen;
            im.Source = bi;
            im.Width = 36;
            im.Height = 36;
            im.MouseDown += im_MouseDown;
            im.Tag = s;

            sus.Children.Add(im);
            sus.Children.Add(us);

            te.Text = ost.GetUrlConvertedStatusText();
            te.TextWrapping = TextWrapping.WrapWithOverflow;
            te.Foreground = Brushes.DarkGreen;

            ti.FontSize = 10;
            ti.Foreground = Brushes.DarkGreen;
            var dt = ost.CreatedDate;
            //日本時間 UTC +09:00
            dt = dt.AddHours(9);
            ti.Text = dt.ToString();

            var urls = new StackPanel();
            foreach (var u in ost.Entities.Urls)
            {
                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedValue);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                urls.Children.Add(new Label { Content = h });
            }
            foreach (var u in st.Entities.Media)
            {

                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedUrl);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                var ll = new Label { Content = h };

                UIElement i;
                if (TryGetMediaControl(u.MediaUrlHttps, out i))
                {
                    Expander e = new Expander();
                    e.HorizontalAlignment = HorizontalAlignment.Left;
                    e.Header = ll;
                    e.Content = i;
                    urls.Children.Add(e);
                }
                else
                {
                    urls.Children.Add(ll);
                }
            }
            var hashs = new StackPanel();
            foreach (var u in ost.Entities.HashTags)
            {
                var t = new TextBlock();
                t.Text = "#" + u.Text;
                t.Foreground = Brushes.Red;
                t.MouseLeftButtonDown += t_MouseLeftButtonDown;
                hashs.Children.Add(t);
            }

            var fbt = GetTemplate("FlatButton");
            var ftbt = GetTemplate("FlatToggleButton");
            StackPanel bts = new StackPanel();
            bts.Orientation = Orientation.Horizontal;
            ToggleButton rt = new ToggleButton(), fav = new ToggleButton();

            rt.Content = MessageInfoRetweetButtonText;
            rt.Click += rt_Click;
            rt.Tag = new RetweetData { Original = ost };
            rt.Template = ftbt;
            rt.Background = Brushes.LightGray;

            fav.Content = MessageInfoFavoriteButtonText;
            fav.Click += fav_Click;
            fav.Tag = s;
            fav.Template = ftbt;
            fav.Background = Brushes.LightGray;

            Button descb = new Button();
            descb.Content = MessageInfoDescriptionButtonText;
            descb.Tag = st;
            descb.Click += descb_Click;
            descb.Template = fbt;
            descb.Background = Brushes.LightGray;

            bts.Children.Add(rt);
            bts.Children.Add(fav);
            bts.Children.Add(descb);



            s.Children.Add(inf);
            s.Children.Add(sus);
            s.Children.Add(te);
            s.Children.Add(urls);
            s.Children.Add(hashs);
            s.Children.Add(ti);
            s.Children.Add(bts);
            s.Tag = ost;
            s.Background = Brushes.Honeydew;
            s.HorizontalAlignment = HorizontalAlignment.Stretch;

            return s;
        }

        void t_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Search((sender as TextBlock).Text as string);
        }

        void im_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var st = ((sender as Image).Tag as StackPanel).Tag as TwitterStatus;
            ViewingUser = st.User;
            RefreshUserInfo();
        }

        public UIElement CreateFavoritePanel(TwitterStatus st, TwitterUser us)
        {
            StackPanel sp = new StackPanel();
            StackPanel abp = new StackPanel();
            abp.Orientation = Orientation.Horizontal;
            var ab = new TextBlock();

            ab.Text = String.Format(MessageMentionFavoritedFormat, us.Name);
            ab.FontSize = 18;
            var abim = new Image();
            abim.Width = 36;
            abim.Height = 36;
            abim.MouseDown += im_MouseDown2;
            abim.Tag = sp;//TODO : ここ
            abim.Source = new BitmapImage(new Uri(us.ProfileImageUrlHttps));//TODO : ここ
            abp.Children.Add(abim);
            abp.Children.Add(ab);

            var te = new TextBlock();
            te.FontSize = 13;
            te.Foreground = Brushes.LightGray;
            te.Text = st.TextDecoded;

            sp.Children.Add(abp);
            sp.Children.Add(te);
            sp.Tag = us;

            return sp;
        }

        private UIElement CreateUnfavoritePanel(TwitterStatus st, TwitterUser us)
        {
            StackPanel sp = new StackPanel();

            StackPanel abp = new StackPanel();
            abp.Orientation = Orientation.Horizontal;
            var ab = new TextBlock();
            ab.Text = String.Format(MessageMentionUnfavoritedFormat, us.Name);
            ab.FontSize = 18;
            var abim = new Image();
            abim.Width = 36;
            abim.Height = 36;
            abim.MouseDown += im_MouseDown2;
            abim.Tag = sp;//TODO : ここ
            abim.Source = new BitmapImage(new Uri(us.ProfileImageUrlHttps));//TODO : ここ
            abp.Children.Add(abim);
            abp.Children.Add(ab);

            var te = new TextBlock();
            te.FontSize = 13;
            te.Foreground = Brushes.LightGray;
            te.Text = st.TextDecoded;

            sp.Children.Add(abp);
            sp.Children.Add(te);
            sp.Tag = us;

            return sp;
        }

        private void im_MouseDown2(object sender, MouseButtonEventArgs e)
        {
            var st = ((sender as Image).Tag as StackPanel).Tag as TwitterUser;
            ViewingUser = st;
            RefreshUserInfo();
        }

        private UIElement CreateRetweetedPanel(TwitterStatus st, TwitterUser us)
        {
            StackPanel sp = new StackPanel();

            StackPanel abp = new StackPanel();
            abp.Orientation = Orientation.Horizontal;
            var ab = new TextBlock();

            ab.Text = String.Format(MessageMentionRetweetedFormat, us.Name);
            ab.FontSize = 18;
            var abim = new Image();
            abim.Width = 36;
            abim.Height = 36;
            abim.Source = new BitmapImage(new Uri(us.ProfileImageUrlHttps));
            abp.Children.Add(abim);
            abp.Children.Add(ab);

            var te = new TextBlock();
            te.FontSize = 13;
            te.Foreground = Brushes.LightGray;
            te.Text = st.TextDecoded;

            sp.Children.Add(abp);
            sp.Children.Add(te);

            return sp;
        }


        void fav_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as ToggleButton);
            var st = (tb.Tag as StackPanel).Tag as TwitterStatus;
            if (tb.IsChecked == true)
            {
                Service.Favorite(st, (tws, res) =>
                {
                    SetStatusBarWithFavorite(res, tws);
                });
            }
            else
            {
                Service.UnfavoriteTweet(new UnfavoriteTweetOptions { Id = st.Id }, (tws, res) =>
                {
                    SetStatusBarWithFavorite(res, tws);
                });
            }


        }

        void rt_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as ToggleButton);
            var rd = tb.Tag as RetweetData;
            var st = rd.Original;
            if (tb.IsChecked == true)
            {
                var r = MessageBox.Show(MessageInfoNotifyRetweet + "\n" + st.User.ScreenName + ":" + st.TextDecoded, "", MessageBoxButton.OKCancel);
                if (r != MessageBoxResult.OK) return;
                Service.Retweet(st, (tws, res) =>
                {
                    SetStatusBarWithRetweet(res, tws);
                    rd.Mine = tws;
                });
            }
            else
            {
                //めんどいので却下
                Service.Delete(rd.Mine, (dst, res2) => { });

            }
        }

        void del_Click(object sender, RoutedEventArgs e)
        {
            var tb = (sender as Button);
            var rd = tb.Tag as StackPanel;
            var st = rd.Tag as TwitterStatus;

            var r = MessageBox.Show(MessageInfoNotifyDelete + "\n" + st.TextDecoded, "", MessageBoxButton.OKCancel);
            if (r != MessageBoxResult.OK) return;

            Service.Delete(st, (dst, res2) => { });
            rd.Children.Clear();
            rd.Tag = null;
            rd.Children.Add(new Label { Content = MessageInfoDeleted });
        }

        public class RetweetData
        {
            public TwitterStatus Original { get; set; }
            public TwitterStatus Mine { get; set; }
        }

        private async void Search(string text)
        {
            SearchTab.Header = String.Format(MessageHeaderSearchResultFormat, text);
            SearchTab.IsSelected = true;
            SearchResult.Items.Clear();
            foreach (var s in await SearchTweet(text, SearchCount))
            {
                await SearchResult.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SearchResult.Items.Add(CreateTweetPanel(s));
                }));

            }
        }

        public Task<IEnumerable<TwitterStatus>> SearchTweet(string text, int count)
        {
            return Task<IEnumerable<TwitterStatus>>.Factory.StartNew(() =>
            {
                return Service.Search(new SearchOptions { Q = text, Count = count }).Statuses;
            });
        }

        public void UpdateStatus(string text)
        {
            TwitterState.Dispatcher.BeginInvoke(new Action(() =>
            {
                TwitterState.Content = text;
            }));
        }

        public ControlTemplate GetTemplate(string name)
        {
            return TryFindResource(name) as ControlTemplate;
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
            }
        }

        private void TextBoxTweet_TextChanged(object sender, TextChangedEventArgs e)
        {
            TweetRest.Text = (140 - TextBoxTweet.Text.Length).ToString() + "　　";
        }

        private void Tweet_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TweetWithButtonChanger(TextBoxTweet.Text, true);
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
            TweetsTab.Dispatcher.BeginInvoke(new Action(() =>
            {
                TweetsTab.Header = String.Format(MessageHeaderUserTweetsFormat, ViewingUser.ScreenName, ListTweetCount);
                TweetsTab.IsSelected = true;
            }));
            Service.ListTweetsOnUserTimeline(new ListTweetsOnUserTimelineOptions { UserId = ViewingUser.Id, Count = ListTweetCount }, (tsl, res) =>
            {
                //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
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


        }

        private void UserTweets_MouseEnter(object sender, MouseEventArgs e)
        {
            UserTweets.Foreground = Brushes.Red;
        }

        private void UserTweets_MouseLeave(object sender, MouseEventArgs e)
        {
            UserTweets.Foreground = Brushes.DarkBlue;
        }

        private void UserFavorites_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
            MT_Users.Dispatcher.BeginInvoke(new Action(() =>
            {
                MT_Users.Header = String.Format(MessageHeaderUserFollowerFormat, ViewingUser.ScreenName, ListTweetCount);
                MT_Users.IsSelected = true;
            }));
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
            MT_Users.Dispatcher.BeginInvoke(new Action(() =>
            {
                MT_Users.Header = String.Format(MessageHeaderUserFollowingFormat, ViewingUser.ScreenName, ListTweetCount);
                MT_Users.IsSelected = true;
            }));
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
                TweetsList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
                UsersList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
            }
            else
            {
                MainTimeline.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                MainMention.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                TweetsList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
                UsersList.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
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

        public void SetStatusBarWithSendTweet(TwitterResponse res, TwitterStatus st)
        {
            var text = "";
            if (res.Error != null)
            {
                //エラー
                var omsg = res.Error.Message;
                var mn = res.Error.Code.ToString();
                var msg = LanguageSetting["Kbtter.Status.Error." + mn].StringValue;
                if (msg == "")
                {
                    msg = omsg;
                }
                text = msg;
            }
            else
            {
                text = LanguageSetting["Kbtter.Status.SendTweetSucceeded"].StringValue;
                text = String.Format(text, st.TextDecoded);
            }
            UpdateStatus(text);
        }

        public void SetStatusBarWithFavorite(TwitterResponse res, TwitterStatus st)
        {
            var text = "";
            if (res.Error != null)
            {
                //エラー
                var omsg = res.Error.Message;
                var mn = res.Error.Code.ToString();
                var msg = LanguageSetting["Kbtter.Status.Error." + mn].StringValue;
                if (msg == "")
                {
                    msg = omsg;
                }
                text = msg;
            }
            else
            {
                text = LanguageSetting["Kbtter.Status.FavoriteSucceeded"].StringValue;
                text = String.Format(text, st.TextDecoded);
            }
            UpdateStatus(text);
        }

        public void SetStatusBarWithUnfavorite(TwitterResponse res, TwitterStatus st)
        {
            var text = "";
            if (res.Error != null)
            {
                //エラー
                var omsg = res.Error.Message;
                var mn = res.Error.Code.ToString();
                var msg = LanguageSetting["Kbtter.Status.Error." + mn].StringValue;
                if (msg == "")
                {
                    msg = omsg;
                }
                text = msg;
            }
            else
            {
                text = LanguageSetting["Kbtter.Status.UnfavoriteSucceeded"].StringValue;
                text = String.Format(text, st.TextDecoded);
            }
            UpdateStatus(text);
        }

        public void SetStatusBarWithRetweet(TwitterResponse res, TwitterStatus st)
        {
            var text = "";
            if (res.Error != null)
            {
                //エラー
                var omsg = res.Error.Message;
                var mn = res.Error.Code.ToString();
                var msg = LanguageSetting["Kbtter.Status.Error." + mn].StringValue;
                if (msg == "")
                {
                    msg = omsg;
                }
                text = msg;
            }
            else
            {
                text = LanguageSetting["Kbtter.Status.RetweetSucceeded"].StringValue;
                text = String.Format(text, st.TextDecoded);
            }
            UpdateStatus(text);
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
            new ListWindow(this,ViewingUser).ShowDialog();
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
