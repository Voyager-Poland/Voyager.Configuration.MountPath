namespace Voyager.Configuration.MountPath
{
	public class SettingsProvider
	{
		public virtual Settings GetSettings(string filename = "appsettings")
		{
			return new Settings()
			{
				FileName = filename,
			};
		}

		internal static Settings PrepareDefault()
		{

			var prov = new SettingsProvider();
			return prov.GetSettings();
		}
	}
}
