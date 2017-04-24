using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using TEdit.MvvmLight.Threading;
using DispatcherHelper = GalaSoft.MvvmLight.Threading.DispatcherHelper;

namespace TEditXna
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            GalaSoft.MvvmLight.Threading.DispatcherHelper.Initialize();
        }

        public static FileVersionInfo Version { get; set; }
		
		public static Version CnVersion { get; } = new Version(1, 6, 0, 0);

        protected override void OnStartup(StartupEventArgs e)
        {
            ErrorLogging.Initialize();
            ErrorLogging.Log(string.Format("TEdit版本 {0} (CN: {1})", ErrorLogging.Version, CnVersion.ToString(3)));
            ErrorLogging.Log(string.Format("OS: {0}", Environment.OSVersion));

            Assembly asm = Assembly.GetExecutingAssembly();
            Version = FileVersionInfo.GetVersionInfo(asm.Location);

            try
            {
                int directxMajorVersion = DependencyChecker.GetDirectxMajorVersion();
                if (directxMajorVersion < 11)
                {
                    ErrorLogging.Log(string.Format("DirectX {0} 过旧. 需要 DirectX 11 及更高版本.", directxMajorVersion));
                }
            }
            catch (Exception ex)
            {
                ErrorLogging.Log("无法验证DirectX版本. TEdit 可能无法正确运行.");
                ErrorLogging.LogException(ex);
            }

            try
            {
                DependencyChecker.CheckPaths();
            }
            catch (Exception ex)
            {
                ErrorLogging.Log("寻找 Terraria 材质路径失败. TEdit 可能无法正确运行.");
                ErrorLogging.LogException(ex);
            }


            try
            {
                if (!DependencyChecker.VerifyTerraria())
                {
                    ErrorLogging.Log("寻找 Terraria 目录失败. 没有可用的材质.");
                }
                else
                {
                    ErrorLogging.Log(string.Format("Terraria v{0}", DependencyChecker.GetTerrariaVersion() ?? "not found"));
                    ErrorLogging.Log(string.Format("Terraria Data Path: {0}", DependencyChecker.PathToContent));
                } // 日志文件里的内容, 暂时跳过汉化
            }
            catch (Exception ex)
            {
                ErrorLogging.Log("Failed to verify Terraria Paths. No texture data will be available.");
                ErrorLogging.LogException(ex);
            }


            if (e.Args != null && e.Args.Count() > 0)
            {
                ErrorLogging.Log(string.Format("Command Line Open: {0}", e.Args[0]));
                this.Properties["OpenFile"] = e.Args[0];
            }

            if (AppDomain.CurrentDomain.SetupInformation.ActivationArguments != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData != null &&
                AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData.Length > 0)
            {
                string fname = "No filename given";
                try
                {
                    fname = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData[0];

                    // It comes in as a URI; this helps to convert it to a path.
                    var uri = new Uri(fname);
                    fname = uri.LocalPath;

                    this.Properties["OpenFile"] = fname;
                }
                catch (Exception ex)
                {
                    // For some reason, this couldn't be read as a URI.
                    // Do what you must...
                    ErrorLogging.LogException(ex);
                }
            }

            DispatcherHelper.Initialize();
            TaskFactoryHelper.Initialize();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }


        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
#if DEBUG
            throw (Exception)e.ExceptionObject;
#else
            ErrorLogging.LogException(e.ExceptionObject);
            MessageBox.Show(string.Format("UNHANDLED EXCEPTION OCCURRED: 请将这里的错误报告文件 \r\n{0}\r\n 通过 Github Issue 报告. (帮助 -> 反馈问题).\r\n程序现在将退出.", ErrorLogging.LogFilePath), "未处理的异常");
            Current.Shutdown();
#endif
        }
    }
}
