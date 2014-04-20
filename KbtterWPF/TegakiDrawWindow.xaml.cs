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
using ColorDialog = System.Windows.Forms.ColorDialog;
using TweetSharp;
using System.Windows.Ink;
using Kb10uy;
using System.IO;
namespace KbtterWPF
{
    /// <summary>
    /// TegakiDrawWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TegakiDrawWindow : Window
    {
        MainWindow main;
        TwitterService svc;
        TwitterUser me;
        DateTime dt;
        StrokeCollection undobuf, redobuf;
        string fn;
        public TegakiDrawWindow(MainWindow mw, DateTime d)
        {
            InitializeComponent();
            MainCanvas.Strokes.StrokesChanged += Strokes_StrokesChanged;
            main = mw;
            svc = main.Service;
            me = main.CurrentUser;
            dt = d;
            fn = "tegaki" + String.Format("{0:D4}{1:D2}{2:D2}-{3:D2}{4:D2}{5:D2}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second) + ".png";
            ImageFileName.Text = fn;
        }

        public TegakiDrawWindow(MainWindow mw, DateTime d, TwitterStatus st)
            : this(mw, d)
        {
            IsReply.IsChecked = true;
            TweetDesc.Text += "@" + st.User.ScreenName + " ";
            ReplyID.Text = st.Id.ToString();
            foreach (var m in st.Entities.Mentions)
            {
                TweetDesc.Text += "@" + m.ScreenName + " ";
            }
        }

        void Strokes_StrokesChanged(object sender, System.Windows.Ink.StrokeCollectionChangedEventArgs e)
        {
            undobuf = e.Added;
            redobuf = e.Removed;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var b = new Rect { Width = 640, Height = 480 };
            var bound = MainCanvas.Strokes.GetBounds();
            bound = b;
            var dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            dc.PushTransform(new TranslateTransform(-bound.X, -bound.Y));
            dc.DrawRectangle(MainCanvas.Background, null, bound);
            MainCanvas.Strokes.Draw(dc);
            dc.Close();
            var rtb = new RenderTargetBitmap((int)bound.Width, (int)bound.Height, 96, 96, PixelFormats.Default);
            rtb.Render(dv);
            var enc = new PngBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(rtb));
            using (Stream s = File.Create("tegaki/" + fn))
            {
                enc.Save(s);
            }

            var d = new Dictionary<string, Stream>();
            d.Add("image", new FileStream("tegaki/" + fn, FileMode.Open));
            var stx = TweetDesc.Text + " " + (AddHash.IsChecked == true ? "#tdt_kbtter" : "");
            if (IsReply.IsChecked == true)
            {
                long id = long.Parse(ReplyID.Text);
                svc.SendTweetWithMedia(new SendTweetWithMediaOptions { InReplyToStatusId = id, Status = stx, Images = d });
            }
            else
            {
                svc.SendTweetWithMedia(new SendTweetWithMediaOptions { Status = stx, Images = d });
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AllClear_Click(object sender, RoutedEventArgs e)
        {
            MainCanvas.Strokes.Clear();
        }

        private void ColorRect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var cd = new ColorDialog();
            if (cd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MainCanvas.DefaultDrawingAttributes.Color = Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B);
                ColorRect.Fill = new SolidColorBrush(MainCanvas.DefaultDrawingAttributes.Color);
            }
        }

        private void PenThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainCanvas.DefaultDrawingAttributes.Width = PenThickness.Value;
            MainCanvas.DefaultDrawingAttributes.Height = PenThickness.Value;
        }

        private void PenEraser_Checked(object sender, RoutedEventArgs e)
        {
            MainCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
        }

        private void PenPen_Checked(object sender, RoutedEventArgs e)
        {
            MainCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }

        private void PenEraserSt_Checked(object sender, RoutedEventArgs e)
        {
            MainCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            MainCanvas.Strokes.Remove(undobuf);
            //MainCanvas.Strokes.Add(redobuf);
        }
    }
}
