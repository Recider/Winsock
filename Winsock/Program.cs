using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winsock
{
	class Program
	{
		static bool isServer = false;
		static bool isSetMode = false;
		static Server ServerRole = null;
		static Client ClientRole = null;

		static void Main(string[] args)
		{
			inputMode();
		}

		static void inputMode()
		{
			do
			{
				Console.WriteLine();
				Console.WriteLine("Select mode:");
				Console.WriteLine("[1] Server \t [2] Client");
				Console.Write("> ");

				String inputMode = Console.ReadLine();

				switch (inputMode)
				{
					default:
						break;
					case "1":
						BindServer();
						break;
					case "2":
						ConnectClient();
						break;
				}
			}
			while (!isSetMode);

			inputArea();
		}
		static void inputArea()
		{
			bool isConnected = true;
			// input area
			do
			{
				Console.Write("> ");
				String input = Console.ReadLine();

				if (!isServer)
				{
					if ("dc" == input)
					{
						ClientRole.Disconnect();
						isConnected = false;
					}
					else if ("quit" == input)
					{
						ClientRole.Disconnect();
						isConnected = false;
						Environment.Exit(0);
					}
					else if ("jsonDemo" == input)
						ClientRole.SendPacket((String)Packets.WritePacket(true, (int)EPacketType.DEFAULT, "I am the one who knocks!"));
					else if("structDemo" == input)
						ClientRole.SendPacket((SPacket)Packets.WritePacket(false, (int)EPacketType.DEFAULT, "I am the one who knocks!"));
				}
			}
			while (isConnected); // this is not perfect condition lel}
			inputMode();
		}
		static void BindServer()
		{
			String modeType;
			String bindHost = "";
			int bindPort = 0;
			Server.Mode Mode = Server.Mode.STRUCT;

			Console.WriteLine();
			Console.WriteLine("Server mode: ");
			Console.WriteLine("[1] Struct (default) \t [2] JSON");
			Console.Write("> ");
			modeType = Console.ReadLine();

			Console.WriteLine();
			Console.WriteLine("Bind to IP (default: localhost): ");
			Console.Write("> ");
			bindHost = Console.ReadLine();

			Console.WriteLine();
			Console.WriteLine("Bind to port (default: 27015): ");
			Console.Write("> ");
			Console.WriteLine();
			try
			{
				bindPort = Int32.Parse(Console.ReadLine());
			}
			catch (Exception)
			{
				Console.WriteLine("Incorrect port number, setting default: 27015");
				bindPort = 27015;
			}

			switch (modeType)
			{
				case "1":
					Mode = Server.Mode.STRUCT;
					break;
				case "2":
					Mode = Server.Mode.JSON;
					break;
				default:
					Mode = Server.Mode.STRUCT;
					break;
			}

			if ("" == bindHost)
				bindHost = "127.0.0.1";

			try
			{
				ServerRole = new Server(Mode, bindHost, bindPort);
				ServerRole.Listen();
				isSetMode = true;
				isServer = true;
				Console.WriteLine(String.Format("Set app to server role on {0}:{1}.", bindHost, bindPort));
				Console.WriteLine();
			}
			catch (Exception)
			{
				Console.WriteLine(String.Format("Cannot bind {0}:{1}, try again.", bindHost, bindPort));
			}
		}
		static void ConnectClient()
		{
			String host = "";
			int port = 0;

			Console.Write("IP address (default: localhost): ");
			host = Console.ReadLine();
			Console.WriteLine();
			
			Console.Write("Port (default: 27015): ");
			try
			{
				port = Int32.Parse(Console.ReadLine());
			}
			catch(Exception){
				Console.WriteLine("Incorrect port number, setting default: 27015");
				port = 27015;
			}
			
			Console.WriteLine();

			if (host.Length > 0)
				host = "127.0.0.1";
			if (0 == port)
				port = 27015;
			try
			{
				ClientRole = new Client(host, port);
				isSetMode = true;
				isServer = false;
				Console.WriteLine("Set app to client role.");
			}
			catch (Exception)
			{
				Console.WriteLine(String.Format("Cannot connect to host {0}:{1}, try again.", host, port));
			}
		}
	}
}
