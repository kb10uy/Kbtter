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
using Kb10uy.Scripting;

namespace KbtterWPF
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window
    {
        MainWindow main;
        Config cfg;
        public SettingWindow(MainWindow mw)
        {
            main = mw;
            cfg = main.Settings;
            InitializeComponent();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                cfg["Kbtter.General.PreloadTimeline"]= int.Parse(TB_Preload.Text);
                cfg["Kbtter.General.TimelineMax"] = int.Parse(TB_MaxTweet.Text);
                cfg["Kbtter.General.MentionMax"] = int.Parse(TB_MaxTweet.Text);
                cfg["Kbtter.General.UserTweetsMax"] = int.Parse(TB_UserTweets.Text);
                cfg["Kbtter.General.UserFollowingShow"] = int.Parse(TB_UserFing.Text);
                cfg["Kbtter.General.UserFollowersShow"] = int.Parse(TB_UserFed.Text);
                cfg["Kbtter.General.UserFavoritesMax"] = int.Parse(TB_UserFavs.Text);
                cfg["Kbtter.General.SearchCount"] = int.Parse(TB_Preload.Text);
                cfg["Kbtter.General.ProtectedText"] = TB_ProtectedText.Text;

                cfg["Kbtter.Environment.MessageFile"] = TB_Lang.Text;

                cfg["Kbtter.Notifycation.ReplySound"] = TB_NotifyReply.Text;
                cfg["Kbtter.Notifycation.RetweetedSound"] = TB_NotifyRetweeted.Text;
                cfg["Kbtter.Notifycation.FavoritedSound"] = TB_NotifyFav.Text;
                cfg["Kbtter.Notifycation.UnfavoritedSound"] = TB_NotifyUnfav.Text;
                cfg["Kbtter.Notifycation.DMReceivedSound"] = TB_NotifyDM.Text;
                
                cfg.SaveFile("Kbtter.cfg");
            }
            catch (Exception ex)
            {
                MessageBox.Show("値の変換に失敗しました！正しい値が入力されているか確認して下さい。\n" + ex.Message, "エラー");
                return;
            }

            
            Close();
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TB_Preload.Text = cfg["Kbtter.General.PreloadTimeline"].NumberValue.ToString();
            TB_MaxTweet.Text = cfg["Kbtter.General.TimelineMax"].NumberValue.ToString();
            TB_MaxMention.Text = cfg["Kbtter.General.MentionMax"].NumberValue.ToString();
            TB_UserTweets.Text = cfg["Kbtter.General.UserTweetsMax"].NumberValue.ToString();
            TB_UserFing.Text = cfg["Kbtter.General.UserFollowingShow"].NumberValue.ToString();
            TB_UserFed.Text = cfg["Kbtter.General.UserFollowersShow"].NumberValue.ToString();
            TB_UserFavs.Text = cfg["Kbtter.General.UserFavoritesMax"].NumberValue.ToString();
            TB_Search.Text = cfg["Kbtter.General.SearchCount"].NumberValue.ToString();
            TB_ProtectedText.Text = cfg["Kbtter.General.ProtectedText"].StringValue;

            TB_Lang.Text = cfg["Kbtter.Environment.MessageFile"].StringValue;

            TB_NotifyReply.Text = cfg["Kbtter.Notifycation.ReplySound"].StringValue;
            TB_NotifyRetweeted.Text = cfg["Kbtter.Notifycation.RetweetedSound"].StringValue;
            TB_NotifyFav.Text = cfg["Kbtter.Notifycation.FavoritedSound"].StringValue;
            TB_NotifyUnfav.Text = cfg["Kbtter.Notifycation.UnfavoritedSound"].StringValue;
            TB_NotifyDM.Text = cfg["Kbtter.Notifycation.DMReceivedSound"].StringValue;

            ListPlugins();

        }

        public void ListPlugins()
        {
            foreach (var i in main.InitializePlugins)
            {
                OnInitList.Items.Add(i.GetPluginName());
            }
            foreach (var i in main.TweetPlugins)
            {
                OnTweetList.Items.Add(i.GetPluginName());
            }
            foreach (var i in main.FavoritePlugins)
            {
                OnFavList.Items.Add(i.GetPluginName());
            }
            foreach (var i in main.UnfavoritePlugins)
            {
                OnUnfavList.Items.Add(i.GetPluginName());
            }
            foreach (var i in main.RetweetPlugins)
            {
                OnRTList.Items.Add(i.GetPluginName());
            }
        }
    }
}
