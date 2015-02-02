using System.Configuration;

namespace GeoServer
{
	public class ServersConfiguration : ConfigurationSection
	{
		private static string sConfigurationSectionConst = "ServersConfiguration";

		/// <summary>
		/// Returns an shiConfiguration instance
		/// </summary>
		public static ServersConfiguration GetConfig()
		{
			return (ServersConfiguration)ConfigurationManager.GetSection(sConfigurationSectionConst) ?? new ServersConfiguration();
		}

		[ConfigurationProperty("serversSettings")]
		public ServerSettingCollection ServersSettings
		{
			get
			{
				return (ServerSettingCollection)this["serversSettings"] ?? new ServerSettingCollection();
			}
		}
	}
}