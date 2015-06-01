using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Winsock
{
	class Client
	{
		Socket ClientSocket;
		struct SConfig
		{
			public String destHost;
			public int destPort;
		}

		SConfig ClientConfig;

		public Client(String host = "127.0.0.1", int port = 27015)
		{
			ClientConfig.destHost = host;
			ClientConfig.destPort = port;
			// ask for permission to bind TCP socket on set port
			// depends on platform/system settings?
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();

			// initializing Socket component
			ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			// ipHost - ip/domain address to connect
			IPHostEntry ipHost = Dns.GetHostEntry(ClientConfig.destHost);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, ClientConfig.destPort);

			// connect to server
			ClientSocket.Connect(ipEnd);
			
		}


		public void SendPacket(IPacket packet)
		{
			byte[] PacketContainer = new byte[1024];
			byte[] ByteStruct = Packets.StructureToByteArray(packet);
			Buffer.BlockCopy(ByteStruct, 0, PacketContainer, 0, ByteStruct.Length);
			ClientSocket.BeginSend(ByteStruct, 0, ByteStruct.Length, 0,
					new AsyncCallback(SendCallback), ClientSocket);
		}

		static byte[] GetBytes(string str)
		{
			byte[] bytes = new byte[str.Length * sizeof(char)];
			System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}

		public void SendPacket(String packet)
		{
			byte[] packetBytes = GetBytes(packet);
			ClientSocket.BeginSend(packetBytes, 0, packetBytes.Length, 0,
					new AsyncCallback(SendCallback), ClientSocket);
		}

		private void SendCallback(IAsyncResult ar)
		{
			try
			{
				// A Socket which has sent the data to remote host  
				Socket handler = (Socket)ar.AsyncState;

				// The number of bytes sent to the Socket  
				int bytesSend = handler.EndSend(ar);
				Console.WriteLine(
					"Sent {0} bytes to Server", bytesSend);
			}
			catch (Exception x) { Console.WriteLine(x.Message + "\n" + x.StackTrace); }
		}

		public void Disconnect()
		{
			ClientSocket.Disconnect(true);
			Console.WriteLine("Client disconnected from server.");
		}

		//public void AcceptCallback(IAsyncResult ar) { }
		//public void ReceiveCallback(IAsyncResult ar) { }
	}
}
