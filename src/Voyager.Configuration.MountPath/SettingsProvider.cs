namespace Voyager.Configuration.MountPath
{
	public class SettingsProvider
	{
		public virtual Settings GetSettings()
		{
			return new Settings();
		}

		internal static Settings PrepareDefault()
		{

			var prov = new SettingsProvider();
			return prov.GetSettings();
		}
	}
}
