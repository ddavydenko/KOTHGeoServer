using System;
using System.Configuration;
using System.Configuration.Provider;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;

namespace GeoServer
{
	public class ServerByIp : IHttpHandler
	{
		private static string urlTemplate;
		private static string xPathCountryQuery;
		private static string xPathCountryQueryNamespaces;
		private static string defaultKothServerIp;
		private static bool? enableCallLogger;

		private static string usaCountryCode;
		private static string xPathZipQuery;
		private static int? zipDiscriminator;
		private static string usaLowIp;
		private static string usaHiIp;

		private static bool EnableCallLogger
		{
			get
			{
				if (!enableCallLogger.HasValue)
				{
					enableCallLogger = (0 == String.Compare("true",
						ConfigurationManager.AppSettings["EnableCallLogger"], StringComparison.OrdinalIgnoreCase));
				}
				return enableCallLogger.Value;
			}
		}

		private static string UrlTemplate
		{
			get { return urlTemplate ?? (urlTemplate = ConfigurationManager.AppSettings["UrlTemplate"]); }
		}
		
		private static string XPathCountryQuery
		{
			get { return xPathCountryQuery ?? (xPathCountryQuery = ConfigurationManager.AppSettings["XPathCountryQuery"]); }
		}

		private static string XPathCountryQueryNamespaces
		{
			get { return xPathCountryQueryNamespaces ?? (xPathCountryQueryNamespaces = ConfigurationManager.AppSettings["XPathCountryQueryNamespaces"]); }
		}
		private static string DefaultKothServerIp
		{
			get { return defaultKothServerIp ?? (defaultKothServerIp = ConfigurationManager.AppSettings["DefaultKothServerIp"]); }
		}

		//все кто больше 64400 - на западный сервер, кто меньне - на восточный
		private static string UsaCountryCode
		{
			get { return usaCountryCode ?? (usaCountryCode = ConfigurationManager.AppSettings["UsaCountryCode"]); }
		}
		private static string XPathZipQuery
		{
			get { return xPathZipQuery ?? (xPathZipQuery = ConfigurationManager.AppSettings["XPathZipQuery"]); }
		}
		private static string UsaLowIp
		{
			get { return usaLowIp ?? (usaLowIp = ConfigurationManager.AppSettings["UsaLowIp"]); }
		}
		private static string UsaHiIp
		{
			get { return usaHiIp ?? (usaHiIp = ConfigurationManager.AppSettings["UsaHiIp"]); }
		}

		private static int ZipDiscriminator
		{
			get
			{
				return zipDiscriminator ?? (zipDiscriminator = Int32.Parse(ConfigurationManager.AppSettings["ZipDiscriminator"])).Value;
			}
		}
		//
		private static string ClientIp
		{
			get
			{
				string clientIp = HttpContext.Current.Request.QueryString["clientip"];
				return String.IsNullOrEmpty(clientIp)
					? HttpContext.Current.Request.UserHostAddress
					: clientIp;
			}
		}

		private static string RequestUrl
		{
			get
			{
				if (String.IsNullOrEmpty(ClientIp))
				{
					throw new InvalidOperationException("client ip is empty");
				}
				if (String.IsNullOrEmpty(UrlTemplate))
				{
					throw new InvalidOperationException("UrlTemplate undefined");
				}
				return String.Format(UrlTemplate, ClientIp);
			}
		}

		private static string GetStringFromXml(XmlDocument xmlDocument, XmlNamespaceManager nsmgr, string xPathQuery)
		{
			XmlNode node = xmlDocument.SelectSingleNode(xPathQuery, nsmgr);
			if (node == null)
				throw new InvalidOperationException("Cannot find "+ xPathQuery +" in xml response");

			string str = node.InnerText;
			if (String.IsNullOrWhiteSpace(str))
				throw new InvalidOperationException(xPathQuery + " result is null or empty");
			return str;
		}

		public void ProcessRequest(HttpContext context)
		{
			context.Response.ContentType = "text/plain";
			try
			{
				string sXml = SendRequest(RequestUrl);
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(sXml);

				XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDocument.NameTable);

				if (!String.IsNullOrEmpty(XPathCountryQueryNamespaces))
				{
					foreach (string ns in XPathCountryQueryNamespaces.Split(';'))
					{
						string[] nsa = ns.Split(',');
						nsmgr.AddNamespace(nsa[0], nsa[1]);
					}
				}

				string sCountry = GetStringFromXml(xmlDocument, nsmgr, XPathCountryQuery);

				string ip = null;
				if (0 == StringComparer.OrdinalIgnoreCase.Compare(UsaCountryCode, sCountry.Trim()))
				{
					string sZip = GetStringFromXml(xmlDocument, nsmgr, XPathZipQuery);
					int zip;
					if (Int32.TryParse(sZip, out zip))
					{
						ip = (zip > ZipDiscriminator) ? UsaHiIp : UsaLowIp;
					}
				}
				else
				{
					ip = KothServerIpProvider.Instance.GetServerIpByCountry(sCountry);
				}
				//if (String.IsNullOrEmpty(ip))
				//{
				//    Logger.Log("unknown country: "+ sCountry, null);
				//}

				string resultIp = String.IsNullOrEmpty(ip) ? DefaultKothServerIp : ip;
				context.Response.Write(resultIp);
				LogCall(sCountry, resultIp);
			}catch(Exception ex)
			{
				Logger.Log(null, ex);
				LogCall("??", DefaultKothServerIp);
				context.Response.Write(DefaultKothServerIp);
			}
		}

		public bool IsReusable
		{
			get
			{
				return false;
			}
		}

		private static string SendRequest(string url)
		{
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
			request.Method = "Get";
			request.ContentType = "text/xml";
			try
			{
				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					if (response.StatusCode != HttpStatusCode.OK)
					{
						throw new ProviderException("Bad responce code:" + ((int)response.StatusCode));
					}

					if (!response.ContentType.StartsWith("text/xml", StringComparison.OrdinalIgnoreCase))
						throw new ProviderException("Invalid response. url = " + url);

					using (Stream responseStream = response.GetResponseStream())
					{
						using (StreamReader sr = new StreamReader(responseStream, Encoding.UTF8))
						{
							return sr.ReadToEnd();
						}
					}
				}
			}
			catch (HttpException ex)
			{
				throw new ProviderException("Server cannot process " + url, ex);
			}
			catch (WebException ex)
			{
				HttpWebResponse r = ex.Response as HttpWebResponse;
				if (null != r)
				{
					throw new ProviderException(String.Format("Server cannot process url: {0}, description: {1}, code: {2}",
						url, r.StatusDescription, r.StatusCode));
				}
				else
				{
					throw new ProviderException("Server cannot process " + url, ex);
				}
			}
		}

		private static void LogCall(string country, string serverIp)
		{
			if (!EnableCallLogger)
				return;
			string msg = String.Format("client ip:{0}, country:{1}, server ip:{2}", ClientIp, country, serverIp);
			if (serverIp == DefaultKothServerIp)
			{
				msg += " -default";
			}
			Logger.LogCall(msg);
		}
	}
}