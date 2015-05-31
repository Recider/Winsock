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
			if (key.Key == ConsoleKey.NumPad1 || key.Key == ConsoleKey.D1)
			{
				net = new Network(27015);
				net.Listen();
			}
			else if (key.Key == ConsoleKey.NumPad2 || key.Key == ConsoleKey.D2)
			{
				net = new Network(27015);
				net.Listen(true);
			}
			else if (key.Key == ConsoleKey.NumPad3 || key.Key == ConsoleKey.D3)
			{
				net = new Network("127.0.0.1");
				isClient = true;
			}
			else
			{
				Environment.Exit(0);
			}

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
