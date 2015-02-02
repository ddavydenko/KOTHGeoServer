using System;
using System.Collections.Generic;

namespace GeoServer
{
	public class KothServerIpProvider
	{
		private readonly Dictionary<string, string> countryToIpMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static KothServerIpProvider instance;
		
		public static KothServerIpProvider Instance
		{
			get { return instance ?? (instance = new KothServerIpProvider()); }
		}

		private KothServerIpProvider()
		{
			ServersConfiguration sc = ServersConfiguration.GetConfig();
			foreach (ServerSettingElements e in sc.ServersSettings)
			{
				foreach (string country in e.Countries.Split(','))
				{
					this.countryToIpMap.Add(country.Trim(), e.Ip);
				}
			}
		}

		public string GetServerIpByCountry(string country)
		{
			string ip;
			return this.countryToIpMap.TryGetValue(country, out ip) ? ip : null;
		}
	}
}