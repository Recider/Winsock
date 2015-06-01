using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace Winsock
{
	class Server
	{
		public enum Mode
		{
			STRUCT, JSON
		}

		struct SConfig
		{
			public String listenHost;
			public int listenPort;
			public int maxClients;
			
			// modes:
			// 0 - Struct->Marshal mode
			// 1 - JSON mode
			public Mode serverMode;
		}

		SConfig ServerConfig;
		Byte[] buffer;
		Socket ServerSocket;
		

		public Server(Mode mode = Mode.STRUCT, String listenHost = "127.0.0.1", int listenPort = 27015, int maxClients = 10)
		{
			ServerConfig.listenHost = listenHost;
			ServerConfig.listenPort = listenPort;
			ServerConfig.maxClients = maxClients;
			ServerConfig.serverMode = mode;

			buffer = new byte[1024];

			// ask for permission to bind TCP socket on set port
			// depends on platform/system settings?
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();

			// initializing Socket component
			// TCP
			ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			IPHostEntry ipHost = Dns.GetHostEntry(ServerConfig.listenHost);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, ServerConfig.listenPort);

			// bind dat ass
			ServerSocket.Bind(ipEnd);
		}

		// that function will allow socket to listen to incoming clients
		public void Listen()
		{
			// runs listening on ServerSocket and allows [MAX_CLIENTS] clients to handle (not total!)
			ServerSocket.Listen(ServerConfig.maxClients);

			// create asynchronous callback to AcceptCallback function
			AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

			// Begins an asynchronous operation to accept an incoming connection attempt from 
			// a specified socket and receives the first block of data sent by the client application
			ServerSocket.BeginAccept(aCallback, ServerSocket);
			Console.WriteLine("Server bound and listening...");
		}

		// handles clients connection event asynchronously
		private void AcceptCallback(IAsyncResult ar)
		{
			// who's listening?
			Socket listener = null;
			// who's calling a server?
			Socket handler = null;

			Console.WriteLine("A client has connected.");

			try
			{
				// prepare to data transmission
				byte[] buffer = new byte[1024];
				listener = (Socket)ar.AsyncState;

				// accept connection
				handler = listener.EndAccept(ar);

				// using Nagle algo (like whatever)
				handler.NoDelay = false;

				object[] obj = new object[2];
				obj[0] = buffer;
				obj[1] = handler;

				// Begins to asynchronously receive data  
				handler.BeginReceive(
					buffer,								// An array of type Byte for received data  
					0,									// The zero-based position in the buffer   
					buffer.Length,						// The number of bytes to receive  
					SocketFlags.None,					// Specifies send and receive behaviors  
					new AsyncCallback(ReceiveCallback),	// An AsyncCallback delegate  
					obj									// Specifies infomation for receive operation  
				);

				// Begin a client handling
				AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
				listener.BeginAccept(aCallback, listener);
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
			}
		}

		// Begins an asynchronous operation of handling client incoming data
		private void ReceiveCallback(IAsyncResult ar)
		{
			// who's calling a server?
			Socket handler = null;
			Console.WriteLine("Received a message from the client: ");
			try
			{
				// Fetch a user-defined object that contains information  
				object[] obj = new object[2];
				obj = (object[])ar.AsyncState;

				// Received byte array  
				byte[] buffer = (byte[])obj[0];

				// A Socket to handle remote host communication.  
				handler = (Socket)obj[1];

				// The number of bytes received (very important).  
				int bytesRead = handler.EndReceive(ar);

				if (bytesRead > 0)
				{
					// byte-to-string conversion would be like this
					// String content = Encoding.Unicode.GetString(buffer, 0, bytesRead);

					switch (ServerConfig.serverMode)
					{
						case Mode.STRUCT:
							serverStructParse(Packets.ByteArrayToStructure(buffer));
							break;

						case Mode.JSON:
							serverJSONParse(Encoding.Unicode.GetString(buffer, 0, bytesRead));
							break;

						default:
							break;
					}

					//re-run listening
					byte[] buffernew = new byte[1024];
					obj[0] = buffernew;
					obj[1] = handler;
					handler.BeginReceive(buffernew, 0, buffernew.Length,
							SocketFlags.None,
							new AsyncCallback(ReceiveCallback), obj);
				}
				else
					Console.WriteLine("Received bytes read = 0 (Disconnected?)");

			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
			}
		}

		private void serverStructParse(IPacket IRecv)
		{
			SPacket recv = (SPacket)IRecv; // explicit conversion, careful
			// byte-to-struct conversion
			if ((int)EPacketType.BLANK == recv.PACKET_TYPE)
				Console.WriteLine("Received packet is corrupted.");
			else
			{
				Console.WriteLine(String.Format("PACKET_TYPE: {0} ({1})", recv.PACKET_TYPE, (EPacketType)recv.PACKET_TYPE));
				Console.WriteLine("TEXT_MESSAGE: " + recv.TEXT_MESSAGE);
				Console.WriteLine("DECIMAL_VALUE: " + recv.DECIMAL_VALUE);
				Console.WriteLine("COUNTER: " + recv.COUNTER);
				Console.WriteLine("TIMESTAMP: " + recv.TIMESTAMP);
				Console.WriteLine("Timestamp diff (ping in ms): " + (Packets.timeInMilis() - recv.TIMESTAMP));
				Console.WriteLine();
			}
		}
		private void serverJSONParse(String recv)
		{
			try
			{
				SPacket SRecv = JsonConvert.DeserializeObject<SPacket>(recv);
				serverStructParse(SRecv);
			}
			catch (Exception)
			{
				Console.WriteLine("Packet malformed / unsupported packet format.");
			}
		}

	}
}
