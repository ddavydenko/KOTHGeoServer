using System.Configuration;

namespace GeoServer
{
	public class ServerSettingElements : ConfigurationElement
	{
		[ConfigurationProperty("ip", IsRequired = true)]
		public string Ip
		{
			get { return this["ip"] as string; }
		}

		[ConfigurationProperty("countries", IsRequired = true)]
		public string Countries
		{
			get { return this["countries"] as string; }
		}
	}
}