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
using TweetSharp;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Kbtter;

namespace KbtterWPF
{
    /// <summary>
    /// Description.xaml の相互作用ロジック
    /// </summary>
    public partial class Description : Window
    {
        MainWindow w;
        TwitterStatus os;
        bool isrepd;
        List<TwitterStatus> l = new List<TwitterStatus>();

        public Description(MainWindow wi, TwitterStatus st)
        {
            isrepd = false;
            w = wi;
            os = st;
            InitializeComponent();
        }

        public Description(MainWindow wi, long id)
        {
            isrepd = true;
            w = wi;
            os = w.Service.GetTweet(new GetTweetOptions { Id = id, IncludeEntities = true });
            InitializeComponent();
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            w.Service.SendTweet(new SendTweetOptions { Status = ReplyText.Text, InReplyToStatusId = os.Id }, (sts, res) => { });
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (isrepd)
            {
                this.Dispatch(() => SetInfo(os));

            }
            else
            {
                w.Service.GetTweet(new GetTweetOptions { Id = os.Id, IncludeEntities = true }, (s, r) =>
                {
                    if (s == null) return;
                    this.Dispatch(() => SetInfo(s));
                });
            }

        }

        /// <summary>
        /// 情報を表示
        /// </summary>
        /// <param name="s">ステタス</param>
        void SetInfo(TwitterStatus s)
        {
            os = s;
            if (s.RetweetedStatus == null)
            {
                SetWith(s, JObject.Parse(s.RawSource));
            }
            else
            {
                SetWith(s.RetweetedStatus, JObject.Parse(s.RawSource)["retweeted_status"]);
            }

            if (s.InReplyToStatusId != null)
            {
                ButtonShowReply.Dispatch(() => ButtonShowReply.IsEnabled = true);
            }
        }

        /// <summary>
        /// 実際の表示、かなりDispatch地獄なので別メソッド
        /// </summary>
        /// <param name="s">ステ</param>
        /// <param name="raw">なまobj</param>
        private void SetWith(TwitterStatus s, dynamic raw)
        {
            UserImage.Dispatch(() => UserImage.Source = new BitmapImage(new Uri(s.User.ProfileImageUrlHttps)));
            UserName.Dispatch(() => UserName.Text = s.User.Name);
            MainText.Dispatch(() => MainText.Text = s.GetUrlConvertedStatusText());
            foreach (var u in s.Entities.Urls)
            {
                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedValue);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                Label l = new Label { Content = h };
                URLText.Dispatch(() =>
                {
                    h.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        URLText.Children.Add(l);
                    }));
                });

            }
            foreach (var u in s.Entities.Media)
            {
                Hyperlink h = new Hyperlink();
                h.NavigateUri = new Uri(u.ExpandedUrl);
                h.RequestNavigate += h_RequestNavigate;
                h.Inlines.Add(u.DisplayUrl);
                Label l = new Label { Content = h };
                URLText.Dispatch(() =>
                {
                    h.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        URLText.Children.Add(l);
                    }));
                });
            }
            ReplyText.Dispatch(() =>
            {
                ReplyText.Text = "@" + s.User.ScreenName + " ";
                foreach (var m in s.Entities.Mentions)
                {
                    ReplyText.Text += "@" + m.ScreenName + " ";
                }
            });
            //rawsourceから自前でふぁぼカウントとる
            FavCount.Dispatch(() => FavCount.Content = raw.favorite_count);
            RTCount.Dispatch(() => RTCount.Content = s.RetweetCount);
        }

        void h_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private void ButtonShowReply_Click(object sender, RoutedEventArgs e)
        {
            new Description(w, (long)os.InReplyToStatusId).ShowDialog();
        }

        private void ReplyText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                w.Service.SendTweet(new SendTweetOptions { Status = ReplyText.Text, InReplyToStatusId = os.Id }, (sts, res) => { });
                Close();
            }
        }

        private void ReplyTegaki_Click(object sender, RoutedEventArgs e)
        {
            new TegakiDrawWindow(w, DateTime.Now, os).ShowDialog();
            Close();
        }

    }
}
