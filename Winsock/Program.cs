using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winsock
{
	class Program
	{
		static void Main(string[] args)
		{
			bool isClient = false; 
			Network net = null;

			Console.WriteLine("[1] Server (String example)\n[2] Server (Struct example)\n[3] Client");
			ConsoleKeyInfo key = Console.ReadKey();
			Console.WriteLine();

			// press NumPad1 or D1 key (runs basic string server)
			if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
			{
				net = new Network(27015);
				net.Listen();
			}
			// press NumPad2 or D2 key (runs server in struct mode)
			else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2)
			{
				net = new Network(27015);
				net.Listen(true);
			}
			// press NumPad3 or D3 key
			else if (key.Key == ConsoleKey.NumPad3 || key.Key == ConsoleKey.D3)
			{
				net = new Network("127.0.0.1");
				isClient = true;
			}
			// executes when pressed wrong key
			else
			{
				Environment.Exit(0);
			}

			// input area
			// type 'pancernik' to send example struct (MUST HAVE SERVER IN STRUCT MODE [option2])
			do
			{
				Console.Write("> ");
				String input = Console.ReadLine();

				if (isClient)
					net.SendMessage(input);
			}
			while (true);
			
		}
	}
}
