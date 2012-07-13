/*
 * Created by SharpDevelop.
 * User: aifdsc
 * Date: 07/30/2011
 * Time: 08:23
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace AudioChanger
{
	class Program
	{
		private static void displayDevices(AudioEndpointManager manager)
		{
			Console.WriteLine("Output Devices ({device name} on {bus name} {last used} |(default)| {device id})");
			Console.WriteLine("--------------------------------------------------------------------------------");
			
			foreach (AudioDevice device in manager.OutputDevices)
				Console.WriteLine(string.Format("{0} on {1} {4:yyyyMMdd HHmmss} {3}\t{2}", device.Name, device.Bus, device.Id, device.IsDefault ? "\t(default)" : "", device.LastDate));
			
			Console.WriteLine();
			
			Console.WriteLine("Input Devices ({device name} on {bus name} {last used} |(default)| {device id})");
			Console.WriteLine("-------------------------------------------------------------------------------");
			
			foreach (AudioDevice device in manager.InputDevices)
				Console.WriteLine(string.Format("{0} on {1} {4:yyyyMMdd HHmmss} {3}\t{2}", device.Name, device.Bus, device.Id, device.IsDefault ? "\t(default)" : "", device.LastDate));
			
			Console.WriteLine();
			Console.WriteLine();
			
			Console.WriteLine(string.Format("Default Output Device:\n{0} on {1}\t{2}", manager.DefaultOutputDevice.Name, manager.DefaultOutputDevice.Bus, manager.DefaultOutputDevice.Id));
			Console.WriteLine();
			Console.WriteLine(string.Format("Default Input Device:\n{0} on {1}\t{2}", manager.DefaultInputDevice.Name, manager.DefaultInputDevice.Bus, manager.DefaultInputDevice.Id));
			
			Console.WriteLine();
			Console.WriteLine();
			
		}
		
		private static void displayHelpMessage()
		{
			Console.WriteLine("Audio Changer written by Stephan Desmoulin AKA aifdsc");
			Console.WriteLine("For more information, please visit http://aifdsc.blogspot.com/");
			Console.WriteLine("");
			Console.WriteLine("Allows a command-line method of switching system default audio devices. Tested and found working on Windows 7 x64. Should also work on Windows 7 x86. In both cases, this program must be run as an administrator or with UAC disabled.");
			Console.WriteLine("");
			Console.WriteLine("Parameters:");
			Console.WriteLine("	/?			Brings up this help message");
			Console.WriteLine("	/list			Display a list of currently enabled audio devices and the defaults");
			Console.WriteLine("	/cro			Returns the ID of the current default output audio device");
			Console.WriteLine("	/cri			Returns the ID of the current default input audio device");
			Console.WriteLine("	/so {guid}		Sets the default output audio device based on a GUID");
			Console.WriteLine("	/so devicename busname	Sets the default output audio device based on the device");
			Console.WriteLine("				name and device bus name (like Speakers, Creative SB X-Fi)");
			Console.WriteLine("	/si {guid}		Sets the default input audio device based on a GUID");
			Console.WriteLine("	/si devicename busname	Sets the default input audio device based on the device");
			Console.WriteLine("				name and device bus name (like Speakers, Creative SB X-Fi)");
			Console.WriteLine("	/so devicename busname [devicename] [busname] ...	Switches between default output audio devices");
			Console.WriteLine("				in the list of pairs; once it reaches the last one, it cycles back to the first item");
			Console.WriteLine("	/si devicename busname [devicename] [busname] ...	Switches between default input audio devices");
			Console.WriteLine("				in the list of pairs; once it reaches the last one, it cycles back to the first item");
		}
		
		public static void Main(string[] args)
		{
			try
			{
				//Console.Clear();
				Console.SetWindowSize(120, 40);
				
				AudioEndpointManager manager = new AudioEndpointManager();
				
				if (args == null || args.Length == 0)
				{
					displayDevices(manager);
					return;
				}
				
				switch (args[0]) {
					case "/list":
						displayDevices(manager);
						break;
						
					case "/?":
					case "/help":
						displayHelpMessage();
						break;
						
					case "/cro":
						Console.Write(string.Format("{0} {1} {2}", manager.DefaultOutputDevice.Id, manager.DefaultOutputDevice.Name, manager.DefaultOutputDevice.Bus));
						break;
						
					case "/cri":
						Console.Write(string.Format("{0} {1} {2}", manager.DefaultInputDevice.Id, manager.DefaultInputDevice.Name, manager.DefaultInputDevice.Bus));
						break;
						
					case "/so":
						if (args.Length == 2)
							manager.SetDefaultOutputDevice(args[1]);
						if (args.Length == 3)
							manager.SetDefaultOutputDevice(args[1], args[2]);
						if (args.Length > 4)
							manager.SetNextDefaultOutputDevice(args, 1);
						break;
						
					case "/si":
						if (args.Length == 2)
							manager.SetDefaultInputDevice(args[1]);
						if (args.Length == 3)
							manager.SetDefaultInputDevice(args[1], args[2]);
						if (args.Length > 4)
							manager.SetNextDefaultInputDevice(args, 1);
						break;
						
					default:
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			
			/*
			AudioEndpoint audio = new AudioEndpoint();
			try
			{
				audio.GetAudioInputDevices();
			} catch (Exception e) {
				
				Console.WriteLine(e.ToString());
			}
			try
			{
				audio.GetAudioOutputDevices();
			} catch (Exception e) {
				
				Console.WriteLine(e.ToString());
			}
			
			Console.WriteLine("Output devices...");
			foreach (KeyValuePair<String, String> device in audio.OutputDevicesPeriheral)
				Console.WriteLine(device.Key + "		(" + device.Value + ") " + audio.OutputDevices[device.Key]);

			Console.WriteLine();
			
			Console.WriteLine("Input devices...");
			foreach (KeyValuePair<String, String> device in audio.InputDevicesPeriheral)
				Console.WriteLine(device.Key + "		(" + device.Value + ") " + audio.InputDevices[device.Key]);
			
			Console.WriteLine();
			Console.WriteLine("Default output device:		" + audio.DefaultOutputDevice);
			Console.WriteLine("Default input device:		" + audio.DefaultInputDevice);
			*/
			
			/*
			Console.WriteLine();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			*/
		}
	}
}
