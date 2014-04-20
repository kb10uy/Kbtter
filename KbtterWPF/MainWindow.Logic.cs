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
    public partial class MainWindow
    {
        public MediaPlayer Player { get; set; }

        public AccountSelect ACSelect { get; set; }
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

        public Dictionary<string, Stream> UploadImagePath { get; set; }
        public OpenFileDialog UploadImageDialog { get; set; }

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
        public string MessageInfoProtectedText { get; set; }

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

            Player = new MediaPlayer();

            UploadImagePath = new Dictionary<string, Stream>();
            UploadImageDialog = new OpenFileDialog();
            UploadImageDialog.AddExtension = true;
            UploadImageDialog.Multiselect = true;
            UploadImageDialog.Title = "アップロードする画像を選択";
            UploadImageDialog.Filter = "対応している画像ファイル|*.png;*.jpg;*.jpeg;*.gif|PNGイメージ|*.png|JPEGイメージ|*.jpg;*.jpeg|GIFイメージ|*.gif";

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

        public void SetStatusBarWithSendTweet(TwitterResponse res, TwitterStatus st)
        {
            if (res == null)
            {
                return;
            }
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


        void EventAction(TwitterUserStreamEvent p)
        {
            Console.WriteLine(p.ToString());
            switch (p.Event)
            {
                case "favorite":
                    var fs = p.TargetObject as TwitterStatus;
                    if (p.Source == CurrentUser) break;
                    if (p.Target != CurrentUser) break;
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
                    if (p.Target != CurrentUser) break;
                    MainMention.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainMention.Items.Insert(0, CreateUnfavoritePanel(fs2, p.Source));
                    }));
                    NotifyUnfavorited(p);
                    foreach (var pl in UnfavoritePlugins) pl.OnUnfavorite(Service, p.Source, fs2);
                    break;
                case "user_update":
                    Console.WriteLine("やったぜ。");
                    break;
                default:
                    Console.WriteLine(p.RawSource.ToString());
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

        void FriendAction(TwitterUserStreamFriends p)
        {

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
            if (!Directory.Exists("tegaki"))
            {
                Directory.CreateDirectory("tegaki");
            }

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
            MessageInfoProtectedText = Settings["Kbtter.General.ProtectedText"].StringValue;

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
            f["Kbtter.General.ProtectedText"] = "🔓";

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
            UserName.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ViewingUser.IsProtected == true)
                {
                    UserName.Content = "🔓" + ViewingUser.Name;
                }
                else
                {
                    UserName.Content = ViewingUser.Name;
                }

            }));
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
            this.Dispatch(() =>
            {
                TabItem ti = new TabItem();

                StackPanel h = new StackPanel();
                h.Orientation = Orientation.Horizontal;
                TextBlock tx = new TextBlock { Text = String.Format(MessageHeaderUserFollowerFormat, ViewingUser.ScreenName) };
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


                Service.ListFollowers(new ListFollowersOptions { UserId = ViewingUser.Id, Count = (byte)ListFollowersCount }, (tl, res) =>
                {
                    lb.Dispatch(() =>
                    {
                        //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                        if (tl == null) return;
                        foreach (var u in tl)
                        {
                            lb.Items.Add(CreateUserPanel(u));
                        }

                        if (tl.NextCursor == 0) return;
                        Button morefw = new Button();
                        morefw.Content = "さらに表示";
                        morefw.Click += morefw_Click;
                        morefw.Tag = new FFInfo { Cursor = tl.NextCursor, User = ViewingUser, TargetListBox = lb };
                        lb.Items.Add(morefw);
                    });
                });
            });
        }
        public class FFInfo
        {
            public ListBox TargetListBox { get; set; }
            public TwitterUser User { get; set; }
            public long? Cursor { get; set; }
        }


        public void ListFollowings()
        {
            this.Dispatch(() =>
            {
                TabItem ti = new TabItem();

                StackPanel h = new StackPanel();
                h.Orientation = Orientation.Horizontal;
                TextBlock tx = new TextBlock { Text = String.Format(MessageHeaderUserFollowingFormat, ViewingUser.ScreenName) };
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
                Service.ListFriends(new ListFriendsOptions { UserId = ViewingUser.Id, Count = (byte)ListFollowingCount }, (tl, res) =>
                {
                    lb.Dispatch(() =>
                    {
                        //TwitterState.Dispatch(() => TwitterState.Content = res.ToString());
                        if (tl == null) return;
                        foreach (var u in tl)
                        {
                            lb.Items.Add(CreateUserPanel(u));
                        }

                        if (tl.NextCursor == 0) return;
                        Button morefw = new Button();
                        morefw.Content = "さらに表示";
                        morefw.Click += morefr_Click;
                        morefw.Tag = new FFInfo { Cursor = tl.NextCursor, User = ViewingUser, TargetListBox = lb };
                        lb.Items.Add(morefw);
                    });

                });
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
                TextBoxTweet.Dispatch(() => TextBoxTweet.Text = "");

            });
        }

        public void MediaTweetWithButtonChanger(string str, bool f)
        {
            ButtonTweet.Background = Brushes.DarkGray;
            ButtonTweet.MouseLeftButtonDown -= Tweet_MouseLeftButtonDown;
            ButtonTweet.Background = Brushes.DarkGray;
            var s = Service.SendTweetWithMedia(new SendTweetWithMediaOptions { Status = str, Images = UploadImagePath });
            SetStatusBarWithSendTweet(null, s);
            UploadImagePath.Clear();
            IncludeImageList.Dispatch(() => IncludeImageList.Children.Clear());
            ButtonTweet.Dispatcher.BeginInvoke(new Action(() =>
            {
                ButtonTweet.Background = new SolidColorBrush { Color = Color.FromRgb(85, 172, 238) };
                ButtonTweet.MouseLeftButtonDown += Tweet_MouseLeftButtonDown;
                SetDefaultUser();
            }));
            TextBoxTweet.Dispatch(() => TextBoxTweet.Text = "");
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



    }
}
