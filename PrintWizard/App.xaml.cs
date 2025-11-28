// App.xaml.cs 文件

using System.Windows;

public partial class App : Application
{
    public App()
    {
        this.Dispatcher.UnhandledException += App_DispatcherUnhandledException;
    }

    /// <summary>
    /// 异常捕获处理
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        // 尝试获取内部异常信息
        string errorMessage = e.Exception.Message;
        Exception innerEx = e.Exception.InnerException;

        while (innerEx != null)
        {
            // 打印或记录最深层的内部异常消息
            errorMessage = innerEx.Message;
            innerEx = innerEx.InnerException;
        }

        MessageBox.Show($"致命错误：{errorMessage}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}