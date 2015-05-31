using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
/*
 * inspiration/reference
 * http://www.codeproject.com/Articles/463947/Working-with-Sockets-in-Csharp
 * 
 */
namespace Winsock
{
	class Network
	{
		Socket ServerSocket;
		Socket ClientSocket;

		// maximum clients handled in one time by ServerSocket
		// default: 10 (lookat: public Network(int port = 27015, int maxClients = 10) )
		int MAX_CLIENTS;
		byte[] buffer;

		// run server in struct mode?
		bool structComms = false;

		// example struct for sending thru
		struct SExampleStruct
		{
			public String name;
			public int number;
			public Decimal decimalNumber;
		}

		// Network constructor for server role (any)
		public Network(int port = 27015, int maxClients = 10)
		{
			buffer = new byte[1024];

			// ask for permission to bind TCP socket on set port
			// depends on platform/system settings?
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();

			// initializing Socket component
			ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			
			// there should be ip address to listen to (default 0.0.0.0 (any) ?)
			IPHostEntry ipHost = Dns.GetHostEntry("");
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, port);

			// bind dat ass
			ServerSocket.Bind(ipEnd);
			MAX_CLIENTS = maxClients;
		}

		// Network constructor for client role
		public Network(String host = "127.0.0.1", int port = 27015)
		{
			// ask for permission to bind TCP socket on set port
			// depends on platform/system settings?
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();

			// initializing Socket component
			ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			// ipHost - ip/domain address to connect
			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, port);

			// connect to server
			ClientSocket.Connect(ipEnd);
			
		}

		// server only
		// start socket waiting to incoming connections / listening
		public void Listen(bool structComms = false)
		{
			// runs listening on ServerSocket and allows [MAX_CLIENTS] clients to handle (not total!)
			ServerSocket.Listen(MAX_CLIENTS);

			// create asynchronous callback to AcceptCallback function
			AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

			// Begins an asynchronous operation to accept an incoming connection attempt from 
			// a specified socket and receives the first block of data sent by the client application
			ServerSocket.BeginAccept(aCallback, ServerSocket);
			Console.WriteLine("Server started, listening...");
			this.structComms = structComms;
		}

		// server only
		// handles clients connection event asynchronously
		private void AcceptCallback(IAsyncResult ar) 
		{
			Socket listener = null;
			Socket handler = null;
			Console.WriteLine("Client has connected.");

			try
			{
				// prepare to data transmission
				byte[] buffer = new byte[1024];
				listener = (Socket)ar.AsyncState;

				// accept connection
				handler = listener.EndAccept(ar);

				// using Nagle algo
				handler.NoDelay = false;

				object[] obj = new object[2];
				obj[0] = buffer;
				obj[1] = handler;

				// Begins to asynchronously receive data  
				handler.BeginReceive(
					buffer,        // An array of type Byte for received data  
					0,             // The zero-based position in the buffer   
					buffer.Length, // The number of bytes to receive  
					SocketFlags.None,// Specifies send and receive behaviors  
					new AsyncCallback(ReceiveCallback),//An AsyncCallback delegate  
					obj            // Specifies infomation for receive operation  
				);

				// Begins an asynchronous operation to accept an attempt  
				AsyncCallback aCallback = new AsyncCallback(AcceptCallback);
				listener.BeginAccept(aCallback, listener); 
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
			}
		}

		// server only
		// Begins an asynchronous operation to accept an attempt
		private void ReceiveCallback(IAsyncResult ar)
		{
			Socket handler = null;
			Console.WriteLine("Received a message from client: ");
			try
			{
				// Fetch a user-defined object that contains information  
				object[] obj = new object[2];
				obj = (object[])ar.AsyncState;

				// Received byte array  
				byte[] buffer = (byte[])obj[0];

				// A Socket to handle remote host communication.  
				handler = (Socket)obj[1];

				// Received message container
				string content = string.Empty;

				// The number of bytes received.  
				int bytesRead = handler.EndReceive(ar);

				if (bytesRead > 0)
				{
					// byte-to-string conversion
					content = Encoding.Unicode.GetString(buffer, 0, bytesRead);
					if (structComms)
					{
						// byte-to-struct conversion
						SExampleStruct recv = ByteArrayToStructure(buffer);

						Console.WriteLine("Name: " + recv.name);
						Console.WriteLine("Number: " + recv.number);
						Console.WriteLine("Decimal: " + recv.decimalNumber);
					}
					else
						Console.WriteLine(content);


					//re-run listening
					byte[] buffernew = new byte[1024];
					obj[0] = buffernew;
					obj[1] = handler; 
					handler.BeginReceive(buffernew, 0, buffernew.Length,
							SocketFlags.None,
							new AsyncCallback(ReceiveCallback), obj);
				}
				else
					Console.WriteLine("Received bytes read = 0 (?)");
 
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
			}
		}

		byte[] StructureToByteArray(object obj)
		{
			int len = Marshal.SizeOf(obj);
			byte[] arr = new byte[len];
			
			try
			{
				IntPtr ptr = Marshal.AllocHGlobal(len);
				Marshal.StructureToPtr(obj, ptr, false);
				Marshal.Copy(ptr, arr, 0, len);
				Marshal.FreeHGlobal(ptr);

				return arr;
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
				return null;
			}
			
		}

		SExampleStruct ByteArrayToStructure(byte[] bytearray)
		{
			SExampleStruct Srecv =  new SExampleStruct();

			// check length/sizeof if AccessViolation exception
			int len = bytearray.Length;
			IntPtr i = Marshal.AllocHGlobal(len);
			Marshal.Copy(bytearray, 0, i, len);
			Srecv = (SExampleStruct)Marshal.PtrToStructure(i, typeof(SExampleStruct));
			Marshal.FreeHGlobal(i);

			return Srecv;
		}

		public void SendMessage(String message)
		{
			// type "pancernik" to run ExampleStruct Demo
			if (message == "pancernik") SendExampleStruct();
			else
			{
				// send string via socket
				byte[] buffer = Encoding.Unicode.GetBytes(message);
				ClientSocket.BeginSend(buffer, 0, buffer.Length, 0,
						new AsyncCallback(SendCallback), ClientSocket); 
			}
			
		}

		public void SendCallback(IAsyncResult ar)
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

		public void SendExampleStruct()
		{
			SExampleStruct Call;
			Call.name = "Endriju";
			Call.number = 123;
			Call.decimalNumber = -3.14157927M;

			byte[] ByteStruct = StructureToByteArray(Call);
			ClientSocket.BeginSend(ByteStruct, 0, ByteStruct.Length, 0,
					new AsyncCallback(SendCallback), ClientSocket); 
		}

	}
}
