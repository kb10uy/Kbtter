using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KbtterWPF
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("エラーが発生しました。\n\'****\' object has no ...という内容の場合、プラグインのエラーである可能性が高いです。\n" + e.Exception.Message);
            e.Handled = true;
            Application.Current.Shutdown();
        }
    }
}
