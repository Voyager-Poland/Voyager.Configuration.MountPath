using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Owin;
using System.Web.Http;
using Unity;
using Unity.Interception;
using Unity.WebApi;

[assembly: OwinStartup(typeof(Voyager.Configuration.MountPath.Owin.Startup1))]

namespace Voyager.Configuration.MountPath.Owin
{
	public class Startup1
	{
		public void Configuration(IAppBuilder app)
		{

			var config = new HttpConfiguration();
			config.MapHttpAttributeRoutes();
			config.EnsureInitialized();
			config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;

			UnityContainer container = new UnityContainer();
			container.AddExtension(new Interception());
			container.AddExtension(new Diagnostic());

			ConfigurationBuilder builder = new ConfigurationBuilder();
			builder.AddMountConfiguration(new Settings());
			IConfigurationRoot configuration = builder.Build();

			container.RegisterInstance<IConfiguration>(configuration);

			config.DependencyResolver = new UnityDependencyResolver(container);


			app.UseWebApi(config);
		}
	}
}
