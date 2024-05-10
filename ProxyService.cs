using System;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace HSRProxy
{
    internal class ProxyService
    {
        private const string QueryGatewayRequestString = "query_gateway";

        private const string ConfFile = "HSRProxy.conf";
        private static string[] s_redirectDomains =
        {
            ".bhsr.com",
            ".starrails.com",
            ".hoyoverse.com",
            ".mihoyo.com"
        };
        private readonly string _targetRedirectUrl= "http://127.5.6.1:17888";
        private readonly string _targetRedirectHost = "";

        private readonly ProxyServer _webProxyServer;

        public ProxyService()
        {
            _webProxyServer = new ProxyServer();
            _webProxyServer.CertificateManager.EnsureRootCertificate();

            _webProxyServer.BeforeRequest += BeforeRequest;
            _webProxyServer.ServerCertificateValidationCallback += OnCertValidation;

            try
            {
                // read
                string[] text = File.ReadAllText(ConfFile, Encoding.UTF8).Split(" => ");
                if(text.Length == 2)
                {
                    string[] aSrcHost = text[0].Split(";");
                    if(aSrcHost.Length > 0)
                    {
                        string sTargetUrl = text[1];
                        string[] aTargetUrl = sTargetUrl.Split("/");
                        if(aTargetUrl.Length == 3)
                        {
                            string[] aDstHost = aTargetUrl[2].Split(":");
                            if(aDstHost.Length > 0)
                            {
                                s_redirectDomains = aSrcHost;
                                _targetRedirectUrl = sTargetUrl;
                                _targetRedirectHost = aDstHost[0];
                            }
                        }
                    }
                }
                // write
                File.WriteAllText(ConfFile, string.Format("{0} => {1}", string.Join(";", s_redirectDomains), _targetRedirectUrl), Encoding.UTF8);
            }
            catch(Exception){}

            SetEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, 8080, true));
            Console.WriteLine(File.ReadAllText(ConfFile, Encoding.UTF8));
        }

        private void SetEndPoint(ExplicitProxyEndPoint explicitEP)
        {
            explicitEP.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;

            _webProxyServer.AddEndPoint(explicitEP);
            _webProxyServer.Start();

            _webProxyServer.SetAsSystemHttpProxy(explicitEP);
            _webProxyServer.SetAsSystemHttpsProxy(explicitEP);
        }

        public void Shutdown()
        {
            _webProxyServer.Stop();
            _webProxyServer.Dispose();
        }

        private Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            // Console.WriteLine(hostname);
            args.DecryptSsl = ShouldRedirect(hostname);

            return Task.CompletedTask;
        }

        private Task OnCertValidation(object sender, CertificateValidationEventArgs args)
        {
            if (args.SslPolicyErrors == SslPolicyErrors.None)
                args.IsValid = true;

            return Task.CompletedTask;
        }

        private Task BeforeRequest(object sender, SessionEventArgs args)
        {
            string hostname = args.HttpClient.Request.RequestUri.Host;
            if (ShouldRedirect(hostname) || (hostname == _targetRedirectHost && args.HttpClient.Request.RequestUri.AbsolutePath.Contains(QueryGatewayRequestString)))
            {
                string requestUrl = args.HttpClient.Request.Url;
                Uri local = new Uri(_targetRedirectUrl);

                string replacedUrl = new UriBuilder(requestUrl)
                {
                    Scheme = local.Scheme,
                    Host = local.Host,
                    Port = local.Port
                }.Uri.ToString();

                Console.WriteLine(hostname + " => " + replacedUrl);
                args.HttpClient.Request.Url = replacedUrl;
            }

            return Task.CompletedTask;
        }

        private static bool ShouldRedirect(string hostname)
        {
            foreach (string domain in s_redirectDomains)
            {
                if (hostname.EndsWith(domain))
                    return true;
            }

            return false;
        }
    }
}
