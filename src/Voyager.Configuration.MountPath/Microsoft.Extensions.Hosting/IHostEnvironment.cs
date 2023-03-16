using Voyager.Configuration.MountPath;

namespace Microsoft.Extensions.Hosting
{
	public static class IHostEnvironmentExt
	{
		public static SettingsProvider GetSettingsProvider(this IHostEnvironment env)
		{
			return new HostEnvironmentSettings(env);
		}
	}
}
