using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Voyager.Configuration.MountPath.Owin
{
	public class SimpleController : ApiController
	{
		private readonly IConfiguration _configuration;

		public SimpleController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		[Route("api")]
		public HttpResponseMessage GetApi()
		{
			return new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ObjectContent<object>(new
				{
					Ustawienie = "Zmienna1 " + _configuration["TestSetting"]
				}, Configuration.Formatters.JsonFormatter)
			};
		}
	}
}
