using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Winsock
{

	public enum EPacketType
	{
		BLANK, DEFAULT
	}

	public enum EPacketID
	{
		PACKET
	}

	interface IPacket
	{
		int PACKET_ID { get; set; }
	}

	struct SPacket : IPacket
	{
		public int PACKET_ID { get; set; }
		public int PACKET_TYPE;
		public int COUNTER;
		// SizeConst - make it as big as needed
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
		public String TEXT_MESSAGE;
		public Decimal DECIMAL_VALUE;
		public long TIMESTAMP;
	}

	static class Packets
	{

		const int PACKET_BUFFER_LENGTH = 1024;
		
		public static object WritePacket (bool json = false, int packetType = (int)EPacketType.BLANK, String textMessage = null, 
			int counter = 0, Decimal decimalValue = 0.0M, Int32 timestamp = -1)
		{
			SPacket packet = new SPacket();

			packet.PACKET_ID = (int)EPacketID.PACKET;
			packet.PACKET_TYPE = packetType;
			packet.TEXT_MESSAGE = textMessage;
			packet.DECIMAL_VALUE = decimalValue;
			packet.COUNTER = counter;

			if (-1 == timestamp) packet.TIMESTAMP = timeInMilis();
			else packet.TIMESTAMP = timestamp;
			if (json)
			{
				String jsonPacket = JsonConvert.SerializeObject(packet);
				Console.WriteLine(jsonPacket);
				return jsonPacket;
			}
			else return packet;
		}

		public static long unixTimestamp()
		{
			return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
		}

		public static long timeInMilis()
		{
			return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
		}

		public static byte[] StructureToByteArray(IPacket obj)
		{
			try
			{
				int len = Marshal.SizeOf(obj);
				
				
				byte[] packetIDByte = new byte[1];
				byte[] packetBody = new byte[len];
				byte[] bytePacket = new byte[len + packetIDByte.Length];

				packetIDByte[0] = (byte)obj.PACKET_ID;

				// alloc memory = obj memory len
				IntPtr ptr = Marshal.AllocHGlobal(len);
				
				// copy obj to memory
				Marshal.StructureToPtr(obj, ptr, false);

				// copy Marshal ptr memory to packetBody
				// if AccessViolation exception thrown,
				// check if sizeof/length matches
				Marshal.Copy(ptr, packetBody, 0, len);
				Marshal.FreeHGlobal(ptr);

				// combine packetIDByte and packetBody to bytePacket
				// combine cannot be over PACKET_BUFFER_LENGTH len
				Buffer.BlockCopy(packetIDByte, 0, bytePacket, 0, packetIDByte.Length);
				Buffer.BlockCopy(packetBody, 0, bytePacket, 1, packetBody.Length);

				return bytePacket;
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
				return null;
			}
		}

		public static IPacket ByteArrayToStructure(byte[] bytearray)
		{
			// check length/sizeof if AccessViolation exception
			try
			{
				int len = bytearray.Length;
				IPacket recv;
				Type packetType;
				// read first byte - contains info about
				// what packet it is

				int packetID = (int)bytearray[0];
				switch (packetID)
				{
					case 0:
						recv = new SPacket();
						break;
					default:
						recv = new SPacket();
						break;
				}

				packetType = recv.GetType();

				IntPtr ptr = Marshal.AllocHGlobal(len);

				// ignore first byte
				Marshal.Copy(bytearray, 1, ptr, Marshal.SizeOf(recv) - 1);
				recv = (IPacket)Marshal.PtrToStructure(ptr, packetType);

				//SPacket recv = (SPacket)Marshal.PtrToStructure(i, typeof(SPacket));
				Marshal.FreeHGlobal(ptr);
				return recv;
			}
			catch (AccessViolationException)
			{
				Console.WriteLine("Packet corruption detected!");
				Console.WriteLine("Incoming bytearray size: " + bytearray.Length);
				return (SPacket)Packets.WritePacket(false, (int)EPacketType.BLANK);
			}
			catch (Exception x)
			{
				Console.WriteLine(x.Message + "\n" + x.StackTrace);
				return (SPacket)Packets.WritePacket(false, (int)EPacketType.BLANK);
			}
		}
	}
}
