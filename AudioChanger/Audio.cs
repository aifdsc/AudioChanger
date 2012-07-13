/*
 * Created by SharpDevelop.
 * User: aifdsc
 * Date: 07/30/2011
 * Time: 08:24
 * Source Information: http://social.microsoft.com/Forums/en/Offtopic/thread/9ebd7ad6-a460-4a28-9de9-2af63fd4a13e
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using Microsoft.Win32;

public class AudioDevice
{
	
	public string Id { get; set; }
	public string Name { get; set; }
	public string Bus { get; set; }
	public bool IsDefault { get; set; }
	public DateTime LastDate { get; set; }
	
	public AudioDevice(string id, string name, string bus)
	{
		Id			=	id;
		Name		=	name;
		Bus			=	bus;
	}
	
	public AudioDevice(string id, string name, string bus, DateTime lastDate)
	{
		Id			=	id;
		Name		=	name;
		Bus			=	bus;
		LastDate	=	lastDate;
	}

	public AudioDevice(string id, string name, string bus, bool isDefault)
	{
		Id			=	id;
		Name		=	name;
		Bus			=	bus;
		IsDefault	=	isDefault;
	}
	
}

public class AudioEndpointManager
{

	private static string __outputDevicesRegistry = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
	private static string __inputDevicesRegistry = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture";
	
	private static string __outputDeviceIdPrefix = @"{0.0.0.00000000}.";
	private static string __inputDeviceIdPrefix = @"{0.0.1.00000000}.";
	
	public List<AudioDevice> InputDevices { get; set; }
	public List<AudioDevice> OutputDevices { get; set; }
	
	public AudioDevice DefaultInputDevice { get; set; }
	public AudioDevice DefaultOutputDevice { get; set; }

	public static DateTime BytesToDate(Byte[] bytes)
	{
		int year = bytes[1] * 0x100 + bytes[0];
		int month = bytes[2];
		int day = bytes[6];
		// int dayOfWeek = strByte[4]
		int hours = bytes[8];
		int minutes = bytes[10];
		int seconds = bytes[12];
		return new DateTime(year, month, day, hours, minutes, seconds);
	}

	public static Byte[] DateToBytes(DateTime date)
	{
		Byte[] bytes = new Byte[16];
		bytes[1] = (byte)(date.Year / 0x100);
		bytes[0] = (byte)(date.Year - bytes[1] * 0x100);
		bytes[2] = (byte)date.Month;
		bytes[3] = 0; bytes[5] = 0; bytes[7] = 0; bytes[9] = 0; bytes[11] = 0; bytes[13] = 0;
		bytes[15] = 0;
		bytes[4] = (byte)date.DayOfWeek;
		bytes[6] = (byte)date.Day;
		bytes[8] = (byte)date.Hour;
		bytes[10] = (byte)date.Minute;
		bytes[12] = (byte)date.Second;
		return bytes;
	}
	
	public AudioEndpointManager()
	{
		RefreshAvailableAudioDevices();
	}
	
	private static AudioDevice getAudioDevice(List<AudioDevice> list, string deviceName, string busName)
	{
		foreach (AudioDevice device in list)
			if (device.Name.Equals(deviceName, StringComparison.InvariantCultureIgnoreCase) && device.Bus.Equals(busName, StringComparison.InvariantCultureIgnoreCase))
				return device;
		return null;
	}
	
	private static List<AudioDevice> getAvailableAudioDevices(string regKey, bool includeNonEnabledStates)
	{
		List<AudioDevice> list = new List<AudioDevice>();
		AudioDevice defaultAudioDevice = null;
		try
		{
			DateTime lastDate = new DateTime(1970, 1, 1);
			using (RegistryKey devicesKey = Registry.LocalMachine.OpenSubKey(regKey))
			{
				if (devicesKey != null)
				{
					string[] devices = devicesKey.GetSubKeyNames();
					for (int i = 0; i < devices.Length; i++)
					{
						RegistryKey deviceKey = devicesKey.OpenSubKey(devices[i]);
						if (includeNonEnabledStates || (int)deviceKey.GetValue("DeviceState", 0) == 1)
						{
							string deviceName = "Unknown device name";
							string deviceBus = "Unknown device bus";
							try
							{
								RegistryKey devicePropertiesKey = deviceKey.OpenSubKey("Properties");
								deviceName = (string)devicePropertiesKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2", "Unknown device name");
								deviceBus = (string)devicePropertiesKey.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6", "Unknown device bus");
							}
							catch (Exception)
							{
							}
							Byte[] strByte = (System.Byte[])deviceKey.GetValue("Role:0");
							DateTime date = strByte == null ? new DateTime(1970, 1, 1) : BytesToDate(strByte);
							list.Add(new AudioDevice(devices[i], deviceName, deviceBus, date));
							if (date.CompareTo(lastDate) > 0)
							{
								lastDate = date;
								defaultAudioDevice = list[list.Count - 1];
							}
						}
					}
					defaultAudioDevice.IsDefault = true;
				}
			}
		}
		catch (Exception e)
		{
			throw;
		}
		return list;
	}
	
	public void RefreshAvailableAudioDevices()
	{
		InputDevices = getAvailableAudioDevices(__inputDevicesRegistry, false);
		OutputDevices = getAvailableAudioDevices(__outputDevicesRegistry, false);
		foreach (AudioDevice device in InputDevices)
			if (device.IsDefault)
				DefaultInputDevice = device;
		foreach (AudioDevice device in OutputDevices)
			if (device.IsDefault)
				DefaultOutputDevice = device;
	}
	
	private static bool setDefaultAudioDevice(string regKey, string deviceId, string deviceIdPrefix)
	{
		try
		{
			using (RegistryKey deviceKey = Registry.LocalMachine.OpenSubKey(regKey + @"\" + deviceId, true))
			{
				Byte[] timestamp = DateToBytes(DateTime.Now.ToUniversalTime());
				deviceKey.SetValue("Role:0", timestamp);
				deviceKey.SetValue("Role:1", timestamp);
				deviceKey.SetValue("Role:2", timestamp);
			}
		}
		catch (Exception e)
		{
			throw;
		}
		//After changing the registry, use IPolicyConfig->SetDefaultEndPoint to ensure that notifications go out to all running applications
		CPolicyConfigClient client = new CPolicyConfigClient();
		client.SetDefaultDevice(deviceIdPrefix + deviceId);
		CPolicyConfigVistaClient vclient = new CPolicyConfigVistaClient();
		vclient.SetDefaultDevice(deviceIdPrefix + deviceId);
		return true;
	}
	
	public bool SetDefaultInputDevice(AudioDevice device)
	{
		return SetDefaultInputDevice(device.Id);
	}
	
	public bool SetDefaultInputDevice(string deviceId)
	{
		bool result = setDefaultAudioDevice(__inputDevicesRegistry, deviceId, __inputDeviceIdPrefix);
		RefreshAvailableAudioDevices();
		return result;
	}
	
	public bool SetDefaultInputDevice(string deviceName, string busName)
	{
//		foreach (AudioDevice device in InputDevices)
//			if (device.Name.Equals(deviceName, StringComparison.InvariantCultureIgnoreCase) && device.Bus.Equals(busName, StringComparison.InvariantCultureIgnoreCase))
		return SetDefaultInputDevice(getAudioDevice(InputDevices, deviceName, busName));
//		return false;
	}
	
	public bool SetDefaultOutputDevice(AudioDevice device)
	{
		return SetDefaultOutputDevice(device.Id);
	}
	
	public bool SetDefaultOutputDevice(string deviceId)
	{
		bool result = setDefaultAudioDevice(__outputDevicesRegistry, deviceId, __outputDeviceIdPrefix);
		RefreshAvailableAudioDevices();
		return result;
	}
	
	public bool SetDefaultOutputDevice(string deviceName, string busName)
	{
		return SetDefaultInputDevice(getAudioDevice(OutputDevices, deviceName, busName));
//		foreach (AudioDevice device in OutputDevices)
//			if (device.Name.Equals(deviceName, StringComparison.InvariantCultureIgnoreCase) && device.Bus.Equals(busName, StringComparison.InvariantCultureIgnoreCase))
//				return SetDefaultOutputDevice(device.Id);
//		return false;
	}
	
	private static bool setNextDefaultAudioDevice(string regKey, string deviceIdPrefix, List<AudioDevice> devices, IList<string> deviceBusNamePairs, int startIndex)
	{
		if ((deviceBusNamePairs.Count - startIndex) % 2 != 0)
			throw new ApplicationException("Invalid number of items in the list.");
		bool foundDefaultDevice = false;
		AudioDevice device = null;
		for (int i = startIndex; i < deviceBusNamePairs.Count; i += 2)
		{
			device = getAudioDevice(devices, deviceBusNamePairs[i], deviceBusNamePairs[i + 1]);
			if (device != null)
			{
				if (foundDefaultDevice)
				{
					setDefaultAudioDevice(regKey, device.Id, deviceIdPrefix);
					return true;
				}
				if (device.IsDefault)
					foundDefaultDevice = true;
			}
		}
		//Haven't found a device yet; assume that we reached the end of the list and need to wrap around and set the first item as default
		device = getAudioDevice(devices, deviceBusNamePairs[startIndex], deviceBusNamePairs[startIndex + 1]);
		if (device != null)
		{
			setDefaultAudioDevice(regKey, device.Id, deviceIdPrefix);
			return true;
		}
		return false;
	}

	public bool SetNextDefaultInputDevice(IList<string> deviceBusNamePairs)
	{
		return SetNextDefaultInputDevice(deviceBusNamePairs, 0);
	}

	public bool SetNextDefaultInputDevice(IList<string> deviceBusNamePairs, int startIndex)
	{
		return setNextDefaultAudioDevice(__inputDevicesRegistry, __inputDeviceIdPrefix, InputDevices, deviceBusNamePairs, startIndex);
	}
	
	public bool SetNextDefaultOutputDevice(IList<string> deviceBusNamePairs)
	{
		return SetNextDefaultOutputDevice(deviceBusNamePairs, 0);
	}
	
	public bool SetNextDefaultOutputDevice(IList<string> deviceBusNamePairs, int startIndex)
	{
		return setNextDefaultAudioDevice(__outputDevicesRegistry, __outputDeviceIdPrefix, OutputDevices, deviceBusNamePairs, startIndex);
	}
	
}

public class AudioEndpointOrig
{

	private static string outputDevicesReg = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
	private static string inputDevicesReg = @"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Capture";

	public Dictionary<string, string> InputDevices { get; set; }
	public Dictionary<string, string> InputDevicesPeriheral { get; set; }
	public Dictionary<string, string> OutputDevices { get; set; }
	public Dictionary<string, string> OutputDevicesPeriheral { get; set; }
	
	public string DefaultInputDevice { get; set; }
	public string DefaultOutputDevice { get; set; }
        
	public static DateTime BytesToDate(Byte[] bytes)
	{
		int year = bytes[1] * 0x100 + bytes[0];
		int month = bytes[2];
		int day = bytes[6];
		// int dayOfWeek = strByte[4]
		int hours = bytes[8];
		int minutes = bytes[10];
		int seconds = bytes[12];
		return new DateTime(year, month, day, hours, minutes, seconds);
	}

	public static Byte[] DateToBytes(DateTime date)
	{
		Byte[] bytes = new Byte[16];
		bytes[1] = (byte)(date.Year / 0x100);
		bytes[0] = (byte)(date.Year - bytes[1]);
		bytes[3] = 0; bytes[5] = 0; bytes[7] = 0; bytes[9] = 0; bytes[11] = 0; bytes[13] = 0;
		bytes[15] = 0;
		bytes[4] = (byte)date.DayOfWeek;
		bytes[6] = (byte)date.Day;
		bytes[8] = (byte)date.Hour;
		bytes[10] = (byte)date.Minute;
		bytes[12] = (byte)date.Second;
		return bytes;
	}

	public void GetAudioInputDevices()
	{
		try
		{
			InputDevices = new Dictionary<string, string>();
			InputDevicesPeriheral = new Dictionary<string, string>();
			DateTime lastDate = new DateTime(1970, 1, 1);
			using (RegistryKey inputDevicesKey = Registry.LocalMachine.OpenSubKey(inputDevicesReg))
			{
				if (inputDevicesKey != null)
				{
					string[] devices = inputDevicesKey.GetSubKeyNames();
					for (int i = 0; i < devices.Length; i++)
					{
						RegistryKey inputDeviceKey = inputDevicesKey.OpenSubKey(devices[i]);
						if ((int)inputDeviceKey.GetValue("DeviceState", 0) == 1)
						{
							string interfaceName = "Unknown interface (" + i + ")";
							string deviceName = "Unknown device (" + i + ")";
							try
							{
								RegistryKey inputDevicePropertiesKey = inputDeviceKey.OpenSubKey("Properties");
								interfaceName = (string)inputDevicePropertiesKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2", "Unknown interface (" + i + ")");
								deviceName = (string)inputDevicePropertiesKey.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6", "Unknown device (" + i + ")");
							}
							catch (Exception) { }
							int j = 2;
							if (InputDevices.ContainsKey(interfaceName))
							{
								while (InputDevices.ContainsKey(interfaceName + " (" + j + ")"))
									j++;
								interfaceName += " (" + j + ")";
							}
							
							InputDevices[interfaceName] = devices[i];
							InputDevicesPeriheral[interfaceName] = deviceName;
							
							Byte[] strByte = (System.Byte[])inputDeviceKey.GetValue("Role:0");
							DateTime date = strByte == null ? new DateTime(1970, 1, 1) : BytesToDate(strByte);
							if (date.CompareTo(lastDate) > 0)
							{
								lastDate = date;
								DefaultInputDevice = interfaceName;
							}
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			throw;
			//logger.Log(Level.Error, "Error while reading audio devices list", e);
		}
	}

	public void GetAudioOutputDevices()
	{
		try
		{
			OutputDevices = new Dictionary<string, string>();
			OutputDevicesPeriheral = new Dictionary<string, string>();
			DateTime lastDate = new DateTime(1970, 1, 1);
			using (RegistryKey outputDevicesKey = Registry.LocalMachine.OpenSubKey(outputDevicesReg))
			{
				if (outputDevicesKey != null)
				{
					string[] devices = outputDevicesKey.GetSubKeyNames();
					for (int i = 0; i < devices.Length; i++)
					{
						RegistryKey outputDeviceKey = outputDevicesKey.OpenSubKey(devices[i]);
						if ((int)outputDeviceKey.GetValue("DeviceState", 0) == 1)
						{
							string interfaceName = "Unknown interface (" + i + ")";
							string deviceName = "Unknown device (" + i + ")";
							try
							{
								RegistryKey outputDevicePropertiesKey = outputDeviceKey.OpenSubKey("Properties");
								interfaceName = (string)outputDevicePropertiesKey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2", "Unknown interface (" + i + ")");
								deviceName = (string)outputDevicePropertiesKey.GetValue("{b3f8fa53-0004-438e-9003-51a46e139bfc},6", "Unknown device (" + i + ")");
							}
							catch (Exception) { }
							int j = 2;
							if (OutputDevices.ContainsKey(interfaceName))
							{
								while (OutputDevices.ContainsKey(interfaceName + " (" + j + ")"))
									j++;
								interfaceName += " (" + j + ")";
							}
							
							OutputDevices[interfaceName] = devices[i];
							OutputDevicesPeriheral[interfaceName] = deviceName;
							
							Byte[] strByte = (System.Byte[])outputDeviceKey.GetValue("Role:0");
							DateTime date = strByte == null ? new DateTime(1970, 1, 1) : BytesToDate(strByte);
							if (date.CompareTo(lastDate) > 0)
							{
								lastDate = date;
								DefaultOutputDevice = interfaceName;
							}
						}
					}
				}
			}
		}
		catch (Exception e)
		{
			throw;
			//logger.Log(Level.Error, "Error while reading audio devices list", e);
		}
	}

	public void SetAudioInputDevice(string interfaceName)
	{
		try
		{
			using (RegistryKey inputDeviceKey = Registry.LocalMachine.OpenSubKey(inputDevicesReg + @"\" + InputDevices[interfaceName], true))
			{
				Byte[] timestamp = DateToBytes(DateTime.Now);
				inputDeviceKey.SetValue("Role:0", timestamp);
				inputDeviceKey.SetValue("Role:1", timestamp);
				inputDeviceKey.SetValue("Role:2", timestamp);
				DefaultInputDevice = interfaceName;
			}
		}
		catch (Exception e)
		{
			throw;
		}
	}

	public void SetAudioOutputDevice(string interfaceName)
	{
		try
		{
			using (RegistryKey outputDeviceKey = Registry.LocalMachine.OpenSubKey(outputDevicesReg + @"\" + OutputDevices[interfaceName], true))
			{
				Byte[] timestamp = DateToBytes(DateTime.Now);
				outputDeviceKey.SetValue("Role:0", timestamp);
				outputDeviceKey.SetValue("Role:1", timestamp);
				outputDeviceKey.SetValue("Role:2", timestamp);
				DefaultOutputDevice = interfaceName;
			}
		}
		catch (Exception e)
		{
			throw;
		}
	}

}
