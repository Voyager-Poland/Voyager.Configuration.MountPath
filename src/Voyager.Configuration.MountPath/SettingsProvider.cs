namespace Voyager.Configuration.MountPath
{
	public class SettingsProvider
	{
		public virtual Settings GetSettings()
		{
			return new Settings();
		}
	}
}
