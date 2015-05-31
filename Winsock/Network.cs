using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
/*
 * 
 * http://www.codeproject.com/Articles/463947/Working-with-Sockets-in-Csharp
 * 
 * 
 */
namespace Winsock
{
	class Network
	{
		Socket ServerSocket;
		Socket ClientSocket;
		int MAX_CLIENTS;
		byte[] buffer;
		bool structComms = false;

		struct exIOStr
		{
			public String name;
			public int number;
			public Decimal decimalNumber;
		}

		public Network(int port = 27015, int maxClients = 10)
		{
			buffer = new byte[1024];
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();
			ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			
			IPHostEntry ipHost = Dns.GetHostEntry("");
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, port);

			ServerSocket.Bind(ipEnd);
			MAX_CLIENTS = maxClients;
		}

		public Network(String host = "127.0.0.1", int port = 27015)
		{
			var permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "", SocketPermission.AllPorts);
			permission.Demand();
			ClientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);

			IPHostEntry ipHost = Dns.GetHostEntry(host);
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint ipEnd = new IPEndPoint(ipAddr, port);

			ClientSocket.Connect(ipEnd);
			
		}

		public void Listen(bool structComms = false)
		{
			ServerSocket.Listen(MAX_CLIENTS);
			AsyncCallback aCallback = new AsyncCallback(AcceptCallback);

			// Begins an asynchronous operation to accept an incoming connection attempt from 
			// a specified socket and receives the first block of data sent by the client application
			ServerSocket.BeginAccept(aCallback, ServerSocket);
			Console.WriteLine("Server started, listening...");
			this.structComms = structComms;
		}

		private void AcceptCallback(IAsyncResult ar) 
		{
			Socket listener = null;
			Socket handler = null;
			Console.WriteLine("Client has connected.");

			try
			{
				byte[] buffer = new byte[1024];
				listener = (Socket)ar.AsyncState;
				handler = listener.EndAccept(ar);

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

		private void ReceiveCallback(IAsyncResult ar)
		{
			Socket handler = null;
			Console.WriteLine("Client sent a message: ");
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
					content = Encoding.Unicode.GetString(buffer, 0, bytesRead);
					if (structComms)
					{
						exIOStr recv = ByteArrayToStructure(buffer);

						Console.WriteLine("Name: " + recv.name);
						Console.WriteLine("Number:" + recv.number);
						Console.WriteLine("Decimal:" + recv.decimalNumber);
					}
					else
						Console.WriteLine(content);

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

		exIOStr ByteArrayToStructure(byte[] bytearray)
		{
			exIOStr Srecv =  new exIOStr();

			int len = bytearray.Length;
			IntPtr i = Marshal.AllocHGlobal(len);
			Marshal.Copy(bytearray, 0, i, len);
			Srecv = (exIOStr)Marshal.PtrToStructure(i, typeof(exIOStr));
			Marshal.FreeHGlobal(i);

			return Srecv;
		}

		public void SendMessage(String message)
		{
			if (message == "pancernik") SendExampleStruct();
			else
			{
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
			exIOStr Call;
			Call.name = "Endriju";
			Call.number = 123;
			Call.decimalNumber = -3.14157927M;

			byte[] ByteStruct = StructureToByteArray(Call);
			ClientSocket.BeginSend(ByteStruct, 0, ByteStruct.Length, 0,
					new AsyncCallback(SendCallback), ClientSocket); 
		}

	}
}
