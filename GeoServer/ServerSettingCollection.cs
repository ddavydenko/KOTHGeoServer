using System.Configuration;

namespace GeoServer
{
	public class ServerSettingCollection : ConfigurationElementCollection
	{
		public ServerSettingElements this[int index]
		{
			get { return base.BaseGet(index) as ServerSettingElements; }
			set
			{
				if (base.BaseGet(index) != null)
				{
					base.BaseRemoveAt(index);
				}
				this.BaseAdd(index, value);
			}
		}
		protected override ConfigurationElement CreateNewElement()
		{
			return new ServerSettingElements();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((ServerSettingElements)element).Ip;
		}
	}
}