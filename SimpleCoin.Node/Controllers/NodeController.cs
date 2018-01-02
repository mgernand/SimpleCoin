namespace SimpleCoin.Node.Controllers
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.Extensions.Logging;
	using Newtonsoft.Json;
	using PeerToPeer;

	/// <summary>
	/// The controller which holds the nodes REST endpoints.
	/// </summary>
	public class NodeController : Controller
	{
		private readonly ILogger<NodeController> logger;
		private readonly WebSocketManager webSocketManager;

		public NodeController(ILogger<NodeController> logger, WebSocketManager webSocketManager)
		{
			this.logger = logger;
			this.webSocketManager = webSocketManager;
		}

		/// <summary>
		/// Check if the node is alive.
		/// </summary>
		/// <returns></returns>
		[HttpGet("ping")]
		public IActionResult Ping()
		{
			return this.Ok();
		}

		/// <summary>
		/// Gets the connected peers of the node.
		/// </summary>
		/// <returns></returns>
		[HttpGet("peers")]
		public IActionResult GetPeers()
		{
			IList<string> peers = this.webSocketManager.GetPeerUrls();
			return this.Ok(peers);
		}

		/// <summary>
		/// Adds a new peer to the node.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[HttpPost("addPeer")]
		public IActionResult AddPeer([FromBody] IDictionary<string, string> data)
		{
			if (!data.ContainsKey("peer"))
			{
				return this.BadRequest("Missing peer url.");
			}

			string peerAddress = data["peer"];
			this.webSocketManager.ConnectToPeer(peerAddress);

			return this.Ok();
		}

		[HttpGet("hello")]
		public async Task<IActionResult> SendToPeers()
		{
			await this.webSocketManager.BroadcastMessage(new Message
			{
				Type = MessageType.Test,
				Data = JsonConvert.SerializeObject(new {text = "Hello, World!"})
			});

			return this.Ok();
		}
	}
}
