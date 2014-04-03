using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Reactive;
using System.Reactive.Linq;
using TweetSharp;

namespace Kbtter
{

    public interface IKbtterHasName
    {
        string GetPluginName();
    }

    public interface IKbtterOnTweet : IKbtterHasName
    {
        void OnTweet(TwitterService sv, TwitterUser user, TwitterStatus st);
    }

    public interface IKbtterOnFavorite : IKbtterHasName
    {
        void OnFavorite(TwitterService sv, TwitterUser by, TwitterStatus st);
    }

    public interface IKbtterOnUnfavorite : IKbtterHasName
    {
        void OnUnfavorite(TwitterService sv, TwitterUser by, TwitterStatus st);
    }

    public interface IKbtterOnRetweet : IKbtterHasName
    {
        void OnRetweet(TwitterService sv, TwitterUser by, TwitterStatus st);
    }

    public interface IKbtterOnInitialize : IKbtterHasName
    {
        void OnInitialize(TwitterService sv, TwitterUser user);
    }

    public interface IKbtterCallable : IKbtterHasName
    {
        IEnumerable<KbtterPluginMenu> GetMenuList();
    }

    public class KbtterPluginMenu
    {
        public string MenuTitle { get; set; }
        public Action MenuAction { get; set; }
    }

    public static class TwitterEx
    {

        public static void Dispatch(this UIElement ue, Action a)
        {
            ue.Dispatcher.BeginInvoke(a);
        } 

        public static IObservable<TwitterStreamArtifact> StreamUser(this TwitterService s)
        {
            return Observable.Create<TwitterStreamArtifact>((obs) =>
            {
                s.IncludeEntities = true;
                s.StreamUser((a, r) => obs.OnNext(a));
                return s.CancelStreaming;
            });
        }

        public static string GetUrlConvertedStatusText(this TwitterStatus st)
        {
            var ls = st.TextDecoded;
            var so = new List<string>();
            so.AddRange(st.Entities.Urls.Select(p => p.Value));
            so.AddRange(st.Entities.Media.Select(p => p.Url));

            var sr = new List<string>();
            sr.AddRange(st.Entities.Urls.Select(p => p.DisplayUrl));
            sr.AddRange(st.Entities.Media.Select(p => p.DisplayUrl));

            for (int i = 0; i < so.Count; i++)
            {
                ls = ls.Replace(so[i], sr[i]);
            }
            return ls;
        }

        public static void Tweet(this TwitterService sv, string text)
        {
            sv.SendTweet(new SendTweetOptions { Status = text }, (s, r) => { });
        }

        public static void Reply(this TwitterService sv, TwitterStatus rpto, string text)
        {
            sv.SendTweet(new SendTweetOptions { Status = text, InReplyToStatusId = rpto.Id }, (s, r) => { });
        }

        public static void Favorite(this TwitterService sv, TwitterStatus favto)
        {
            sv.FavoriteTweet(new FavoriteTweetOptions { Id = favto.Id }, (s, r) => { });
        }

        public static void Retweet(this TwitterService sv, TwitterStatus rtw)
        {
            sv.Retweet(new RetweetOptions { Id = rtw.Id }, (s, r) => { });
        }

        public static void Delete(this TwitterService sv, TwitterStatus rmt)
        {
            sv.DeleteTweet(new DeleteTweetOptions { Id = rmt.Id }, (s, r) => { });
        }
        //デリゲート対応版
        public static void Tweet(this TwitterService sv, string text, Action<TwitterStatus, TwitterResponse> act)
        {
            sv.SendTweet(new SendTweetOptions { Status = text }, act);
        }

        public static void Reply(this TwitterService sv, TwitterStatus rpto, string text, Action<TwitterStatus, TwitterResponse> act)
        {
            sv.SendTweet(new SendTweetOptions { Status = text, InReplyToStatusId = rpto.Id }, act);
        }

        public static void Favorite(this TwitterService sv, TwitterStatus favto, Action<TwitterStatus, TwitterResponse> act)
        {
            sv.FavoriteTweet(new FavoriteTweetOptions { Id = favto.Id }, act);
        }

        public static void Retweet(this TwitterService sv, TwitterStatus rtw, Action<TwitterStatus, TwitterResponse> act)
        {
            sv.Retweet(new RetweetOptions { Id = rtw.Id }, act);
        }

        public static void Delete(this TwitterService sv, TwitterStatus rmt, Action<TwitterStatus, TwitterResponse> act)
        {
            sv.DeleteTweet(new DeleteTweetOptions { Id = rmt.Id }, act);
        }
    }
}
