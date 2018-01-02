namespace SimpleCoin.Node.PeerToPeer
{
	using System;
	using System.Collections.Generic;
	using System.Net.WebSockets;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Newtonsoft.Json;

	[UsedImplicitly]
	public class MessageHandler
	{
		public async Task Handle(WebSocket socket, Message message)
		{
			IDictionary<string, object> data = JsonConvert.DeserializeObject<IDictionary<string, object>>(message.Data);

			switch (message.Type)
			{
				case MessageType.Test:
					await this.Test(data);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Task Test(IDictionary<string, object> data)
		{
			Console.WriteLine(data["text"]);

			return Task.CompletedTask;

			//await socket.SendMessage(new Message()
			//{
			//	Type = MessageType.Test,
			//	Data = JsonConvert.SerializeObject(data)
			//});
		}
	}
}