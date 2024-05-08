using System;
using System.IO;
using System.Net;
using System.Text;

namespace HSRProxy
{
    internal static class Program
    {
        private const string Title = "HSR Proxy";
        private const string ConfFile = "HSRProxy.conf";

        private static ProxyService s_proxyService;
        private static EventHandler s_processExitHandler = new EventHandler(OnProcessExit);
        
        private static void Main(string[] args)
        {
            Console.Title = Title;
            CheckProxy();

            string sTargetHost = "127.5.6.1";
            int iTargetPort = 17888;
            try
            {
                string line = File.ReadAllText(ConfFile);
                if(line != null)
                {
                    string[] aConfig = line.Split(":");
                    if(aConfig.Length == 2)
                    {
                        int iPort = Int32.Parse(aConfig[1]);
                        if(iPort > 0 && iPort < 65535)
                        {
                            sTargetHost = aConfig[0];
                            iTargetPort = iPort;
                        }
                    }
                }
            }
            catch(Exception){}
            try
            {
                File.WriteAllText(ConfFile, string.Format("{0}:{1}", sTargetHost, iTargetPort), Encoding.UTF8);
            }
            catch(Exception){}

            s_proxyService = new ProxyService(sTargetHost, iTargetPort);
            AppDomain.CurrentDomain.ProcessExit += s_processExitHandler;

            Thread.Sleep(-1);
        }

        private static void OnProcessExit(object sender, EventArgs args)
        {
            s_proxyService.Shutdown();
        }

        public static void CheckProxy()
        {
            try
            {
                string ProxyInfo = GetProxyInfo();
                if (ProxyInfo != null)
                {
                    Console.WriteLine("well... It seems you are using other proxy software(such as Clash,V2RayN,Fiddler,etc)");
                    Console.WriteLine($"You system proxy: {ProxyInfo}");
                    Console.WriteLine("You have to close all other proxy software to make sure HSRProxy can work well.");
                    Console.WriteLine("Press any key to continue if you closed other proxy software, or you think you are not using other proxy.");
                    Console.ReadKey();
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        public static string GetProxyInfo()
        {
            try
            {
                IWebProxy proxy = WebRequest.GetSystemWebProxy();
                Uri proxyUri = proxy.GetProxy(new Uri("https://www.example.com"));

                string proxyIP = proxyUri.Host;
                int proxyPort = proxyUri.Port;
                string info = proxyIP + ":" + proxyPort;
                return info;
            }
            catch
            {
                return null;
            }
        }
    }
}