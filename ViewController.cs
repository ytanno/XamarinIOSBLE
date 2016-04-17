using System;
using System.Timers;
using System.Collections;
using System.Text.RegularExpressions;

using UIKit;
using CoreBluetooth;
using Foundation;
using CoreFoundation;

using System.IO;

//usefule url
//https://developer.xamarin.com/guides/ios/getting_started/installation/device_provisioning/free-provisioning/
//https://developer.xamarin.com/api/namespace/CoreBluetooth/

namespace XamarinTest
{
	public partial class ViewController : UIViewController
	{
		public ViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.
			var myDel = new MySimpleCBCentralManagerDelegate(NotifyTextF1);
			var myMgr = new CBCentralManager(myDel, DispatchQueue.CurrentQueue);
		}

		public override void DidReceiveMemoryWarning()
		{
			base.DidReceiveMemoryWarning();
			// Release any cached data, images, etc that aren't in use.
		}

		partial void UIButton8_TouchUpInside(UIButton sender)
		{
			label1.Text = "On";
			
		}
	}


	public class MySimpleCBCentralManagerDelegate : CBCentralManagerDelegate
	{
		CBCentralManager _mgr;
		ArrayList _activePeripherals = new ArrayList();
		UITextView _notifyText;

		public MySimpleCBCentralManagerDelegate(UITextView notifyTextUI)
		{
			_notifyText = notifyTextUI;
		}


		override public void UpdatedState(CBCentralManager mgr)
		{
			if (mgr.State == CBCentralManagerState.PoweredOn)
			{
				_mgr = mgr;
				//Passing in null scans for all peripherals. Peripherals can be targeted by using CBUIIDs
				CBUUID[] cbuuids = null;
				mgr.ScanForPeripherals(cbuuids); //Initiates async calls of DiscoveredPeripheral
												 //Timeout after 30 seconds
				var timer = new Timer(30 * 1000);
				timer.Elapsed += (sender, e) => mgr.StopScan();
			}
			else
			{
				//Invalid state -- Bluetooth powered down, unavailable, etc.
				System.Console.WriteLine("Bluetooth is not available");
			}
		}

		public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
		{
			if (peripheral.Name == "YourPeripheralName")
			{
				Console.WriteLine("Discovered {0}, data {1}, RSSI {2}", peripheral.Name, advertisementData, RSSI);
				_mgr.ConnectPeripheral(peripheral);
			}
			//Console.WriteLine("Discovered {0}, data {1}, RSSI {2}", peripheral.Name, advertisementData, RSSI);
		}

		public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
		{
		
			Console.WriteLine("Connected to " + peripheral.Name);
			if (peripheral.Delegate == null)
			{
				peripheral.Delegate = new SimplePeripheralDelegate(_notifyText);
				
				//Begins asynchronous discovery of services
				peripheral.DiscoverServices();

			
			}
			_activePeripherals.Add(peripheral);
			//base.ConnectedPeripheral(central, peripheral);
		}

	}


	public class SimplePeripheralDelegate : CBPeripheralDelegate
	{
		UITextView _notifyTextFiled;
		int _textLineNumber = 0;

		string _saveDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		string _filePath;


		public SimplePeripheralDelegate(UITextView notifyTextUI)
		{
			_notifyTextFiled = notifyTextUI;
			_filePath = Path.Combine(_saveDir, "Write.txt");
		}
		
		public override void DiscoveredService(CBPeripheral peripheral, NSError error)
		{
			Console.WriteLine("Discovered a service");
			foreach (var service in peripheral.Services)
			{
				Console.WriteLine(service.ToString());
				peripheral.DiscoverCharacteristics(service);
			}
		}

		public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error)
		{
			Console.WriteLine("Discovered characteristics of " + peripheral);
			foreach (var c in service.Characteristics)
			{
				Console.WriteLine(c.ToString());
				peripheral.ReadValue(c);
			}

			peripheral.SetNotifyValue(true, service.Characteristics[0]);
		}

		public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error)
		{
			Console.WriteLine("Value of characteristic " + descriptor.Characteristic + " is " + descriptor.Value);
		}

		public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error)
		{
			string result = parseValue(characteristic.ToString());
			Console.WriteLine("Value of characteristic " + characteristic.ToString() + " is " + result);
			if (result.Length > 0)
			{
				if (_textLineNumber > 10)
				{
					_textLineNumber = 0;
					_notifyTextFiled.Text = "";
				}

				var saveData = "UUID = " + characteristic.UUID.ToString().Substring(0, 8) + "  Value = " + result + Environment.NewLine;
				_notifyTextFiled.Text += saveData;
				//File.WriteAllText(_filePath, saveData);
				_textLineNumber++;
			}
		}

		public string parseValue(string c)
		{
			var result = "";
			var sp = c.Split(',');
			if (sp.Length > 4)
			{
				result = sp[3].Replace("value = <", "").Replace(">", "").Trim();
			}
			return result;
		}
	}
}