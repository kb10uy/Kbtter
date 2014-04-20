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
            if (rd.Mine == null)
            {
                var r = MessageBox.Show(MessageInfoNotifyRetweet + "\n" + st.User.ScreenName + ":" + st.TextDecoded, "", MessageBoxButton.OKCancel);
                if (r != MessageBoxResult.OK)
                {
                    tb.IsChecked = false;
                    return;
                }
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
                rd.Mine = null;
                tb.IsChecked = false;
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


    }


}
