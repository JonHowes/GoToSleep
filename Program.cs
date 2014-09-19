using System;
using System.Windows.Forms;

using System.Net.NetworkInformation;
using System.Diagnostics;

namespace GoToSleep
{

	class Program
	{

		static int secsBetweenTests = 60;
		static TimeSpan sleepAfterTimeSpan = new TimeSpan(0, 15, 0);

		static void Main(string[] args)
		{
			// Set a different time between tests - primarily for debugging
			int newSecs;
			if (args.Length > 0 && Int32.TryParse(args[0], out newSecs))
			{
				secsBetweenTests = newSecs;
			}

			DateTime startInUseTest = DateTime.Now;
			for ( ; ; )
			{
				DateTime now = DateTime.Now;
				// Test for an active link to a client machine on the same IP range
				if ( IsWindowsMediaPlayerRunning() || (NetworkInterface.GetIsNetworkAvailable() && LocalConnectionsExist()) )
				{
					// This machine has active clients on the local network.
					// Pump up the start time.
					startInUseTest = now;
					Console.WriteLine("Count down reset. {0} to go", sleepAfterTimeSpan);
				}
				else if ((now - startInUseTest) > sleepAfterTimeSpan)
				{
					// This machine has had no active clients for the configured time span.
					// Break out of the loop and continue to put he machine to sleep.
					break;
				}
				else
				{
					Console.WriteLine("Counting down to sleep. {0} to go",  startInUseTest + sleepAfterTimeSpan - now);
				}
				// Wait for the prescribed time before making another test for connected machines
				System.Threading.Thread.Sleep(secsBetweenTests * 1000);
				Console.WriteLine("");
			}
			// If get here, time to put the computer to sleep and exit
			Application.SetSuspendState(PowerState.Suspend, true, false);
		}

		public static bool LocalConnectionsExist()
		{
			int count = 0;
			IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
			TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections();
			foreach (TcpConnectionInformation tcpInfo in tcpConnections)
			{
				string ip1 = tcpInfo.LocalEndPoint.Address.ToString();
				string ip2 = tcpInfo.RemoteEndPoint.Address.ToString();
				if (ip1 == ip2)
				{	// Exclude self connections
					continue;
				}
				if (tcpInfo.State == TcpState.Established && HasSameIpRoot(ip1, ip2))
				{
					string remoteName = null;
					try { remoteName = System.Net.Dns.GetHostEntry(tcpInfo.RemoteEndPoint.Address.ToString()).HostName; }
					catch { }
					count++;
					Console.WriteLine("Connected to   {0}   - ({1})", ip2, remoteName ?? "???");
				}
			}
			return count > 0;
		}

		public static bool HasSameIpRoot(string ip1, string ip2)
		{
			string[] ip1S = ip1.Split(new char[] { '.' });
			string[] ip2S = ip2.Split(new char[] { '.' });
			if (ip1S.Length != ip2S.Length || ip1S.Length != 4)
			{
				return false;
			}
			for (int i = 0; i < 3; i++)
			{
				int ip1Int, ip2Int;
				if (!Int32.TryParse(ip1S[i], out ip1Int))
				{
					return false;
				}
				if (!Int32.TryParse(ip2S[i], out ip2Int))
				{
					return false;
				}
				if (ip1Int != ip2Int)
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsWindowsMediaPlayerRunning()
		{
			foreach (Process clsProcess in Process.GetProcesses())
			{
				if (clsProcess.ProcessName.Contains("wmplayer"))
				{
					Console.WriteLine("Windows media player is running");
					return true;
				}
			}
			return false;
		}

	}
}

