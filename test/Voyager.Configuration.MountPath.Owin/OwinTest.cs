using Microsoft.Owin.Testing;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Voyager.Configuration.MountPath.Owin
{
	[TestFixture]
	public class OwinTest
	{
		private TestServer _server;
		private HttpClient _client;

		[SetUp]
		public void Setup()
		{
			_server = TestServer.Create<Startup1>();
			_client = _server.HttpClient;

		}
		[TearDown]
		public void TearDown()
		{
			_server.Dispose();
			_client.Dispose();
		}

		[Test]
		public async Task SimpleCall()
		{
			var messsage = await _client.GetAsync("api");
			Assert.That(messsage.IsSuccessStatusCode, Is.True);
			Console.WriteLine(await messsage.Content.ReadAsStringAsync());
			Assert.Pass();
		}
	}
}