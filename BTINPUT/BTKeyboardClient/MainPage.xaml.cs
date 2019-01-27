using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace BTKeyboardClient
{
    public sealed partial class MainPage : Page
    {
        public const String SERVICE_GUID_STRING = "66255a21-e815-4e64-8e0e-8ae4f6603009";
        private readonly Guid serviceGuid = Guid.Parse(SERVICE_GUID_STRING);
        public const String INPUT_CHARACTERISTIC_GUID_STRING = "715524a1-b3b8-4c2d-b587-2865114a1a08";
        private readonly Guid inputCharacteristicGuid = Guid.Parse(INPUT_CHARACTERISTIC_GUID_STRING);
        public const String MODE_CHARACTERISTIC_GUID_STRING = "91109139-7195-4fa7-bb99-b187fd4c08a6";
        private readonly Guid modeCharacteristicGuid = Guid.Parse(MODE_CHARACTERISTIC_GUID_STRING);
        private Dictionary<Guid,GattCharacteristic> characteristicsDictionary;

        private DeviceWatcher deviceWatcher;
        private VirtualGamepad virtualGamepad;
        private VirtualKeyboard virtualKeyboard;
        private VirtualMouse virtualMouse;

        private GattDeviceService gattService;
        private ObservableCollection<String> _deviceIds;
        public ObservableCollection<String> deviceIds
        {
            get { return _deviceIds; }
        }

        public enum MODES :byte {KEYBOARD=1,MOUSE,GAMEPAD };


        public HashSet<DeviceInformation> knownDevices { get; private set; }



        private BluetoothAdapter bluetoothAdapter;
        private BluetoothLEDevice bluetoothLEDevice;

        private GattCharacteristic gattModeCharacteristic;
        //private GattCharacteristic gattInputCharacteristic;
        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(1206, 663);
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1206, 663));
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            

            _deviceIds = new ObservableCollection<string>();
            knownDevices = new HashSet<DeviceInformation>();
        }

        private async void loadBluetoothAdapter()
        {
            bluetoothAdapter = await BluetoothAdapter.GetDefaultAsync();
        }

        private void stopWatch()
        {
            if (deviceWatcher != null)
            {
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                deviceWatcher.Stopped -= DeviceWatcher_Stopped;
                deviceWatcher.Stop();
                deviceWatcher = null;
                this.searchButton.IsEnabled = true;
            }
        }

        private void startWatch()
        {
            try
            {
                this.logBox.Text = "";

                string[] requestedProperties = {  };
                knownDevices.Clear();
                deviceIds.Clear();
                if (deviceWatcher == null)
                {
                    deviceWatcher =
                                DeviceInformation.CreateWatcher(
                                        BluetoothLEDevice.GetDeviceSelectorFromConnectionStatus(BluetoothConnectionStatus.Disconnected),
                                        requestedProperties,
                                        DeviceInformationKind.AssociationEndpoint);

                    deviceWatcher.Added += DeviceWatcher_Added;
                    deviceWatcher.Updated += DeviceWatcher_Updated;
                    deviceWatcher.Removed += DeviceWatcher_Removed;
                    deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
                    deviceWatcher.Stopped += DeviceWatcher_Stopped;
                }
                deviceWatcher.Start();

                this.searchButton.IsEnabled = false;
            }
            catch (Exception e)
            {
                logEvent("Failed to create watcher, error: " + e.Message);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            stopWatch();
            startWatch();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (DevicesView.SelectedValue != null)
            {
                await ConnectDevice(DevicesView.SelectedValue.ToString());
            }
        }

        public async void logEvent(String message)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                lock (this)
                {
                    logBox.Text += String.Format("\n{0}", message);
                }
            });
        }

        private async void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            
        }

        private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            logEvent("Enumeration completed");
            logEvent(String.Format("Devices found: {0}", deviceIds.Count));
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    searchButton.IsEnabled = true;
                    if (deviceIds.Count < 1)
                    {
                        stopWatch();
                    }
                }
            });

        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (bluetoothLEDevice != null)
                {
                    if (deviceIds.Contains(args.Id))
                    {
                        logEvent(String.Format("Lost connection to device advertiser {0}", args.Id));
                        lock (this)
                        {
                            deviceIds.Remove(args.Id);
                        }
                    }
                }
            });
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {

        }

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            bool doesNotContain = false;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                lock (this)
                {
                    doesNotContain = knownDevices.Add(args);
                }
            });
            if (doesNotContain)
            {
                bool compatible = false;
                try
                {
                    compatible = await isDeviceCompatibleAsync(args);
                }
                catch (Exception e) {
                    logEvent(String.Format("Exception chcecking compatibility {0} for device {1}", e.Message, args.Name));
                }

                if (compatible)
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        lock (this)
                        {
                            if (!deviceIds.Contains(args.Id))
                            {
                                logEvent(String.Format("Found device {0} {1}", args.Id, args.Name));
                                _deviceIds.Add(args.Id);
                            }
                        }
                    });
                }
            }
        }

        private async Task<bool> isDeviceCompatibleAsync(DeviceInformation args)
        {
            var device = await BluetoothLEDevice.FromIdAsync(args.Id);
            if (device == null)
            {
                throw (new Exception("Cannot connect to device"));
            }

            var servicesResult = await device.GetGattServicesForUuidAsync(serviceGuid);
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                throw (new Exception(String.Format("Cannot retrive services. Status: {0}, Error: {1}", servicesResult.Status, servicesResult.ProtocolError)));
            }
            var services = servicesResult.Services;
            if (services.Count < 1)
            {
                return false;
            }
            GattDeviceService service = services.FirstOrDefault();

            var modeCharacteristicsResultTask = service.GetCharacteristicsForUuidAsync(modeCharacteristicGuid);
            var modeCharacteristicsResult = await modeCharacteristicsResultTask;
            if (modeCharacteristicsResult.Status != GattCommunicationStatus.Success)
            {
                throw (new Exception(String.Format("Cannot retrive characteristics. Status: {0}, Error: {1}",
                    modeCharacteristicsResult.Status, modeCharacteristicsResult.ProtocolError)));
            }
           
            return true;
        }


        async Task ConnectDevice(String Id)
        {
            logEvent("Attempting to connect");

            try
            {
                bluetoothLEDevice = await BluetoothLEDevice.FromIdAsync(DevicesView.SelectedValue.ToString());

                logEvent(String.Format("Connected with {0}",
                    bluetoothLEDevice.Name));
            }
            catch (Exception e)
            {
                logEvent(e.Message);

            }
            if (bluetoothLEDevice != null)
            {
                GattDeviceServicesResult result = await bluetoothLEDevice.GetGattServicesForUuidAsync(serviceGuid, BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    if (result.Services.Count < 1)
                    {
                        deviceIds.Remove(bluetoothLEDevice.DeviceId);
                        bluetoothLEDevice = null;
                        logEvent(String.Format("No supported services found. Removing device from list {0}", bluetoothLEDevice.DeviceId));
                        return;
                    }

                    gattService = result.Services.FirstOrDefault();

                    logEvent(String.Format("Getting characteristics from service {0}", gattService.Uuid));

                    var allCharacteristics = await gattService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                    if (allCharacteristics.Status != GattCommunicationStatus.Success)
                    {
                        logEvent(String.Format("Error getting characteristics {0} {1}"
                            , allCharacteristics.Status.ToString(), allCharacteristics.ProtocolError.ToString()));
                        gattService.Dispose();
                        gattService = null;
                        bluetoothLEDevice.Dispose();
                        bluetoothLEDevice = null;
                        return;
                    }
                    characteristicsDictionary = allCharacteristics.Characteristics.ToDictionary(t => t.Uuid);

                    gattModeCharacteristic = characteristicsDictionary[modeCharacteristicGuid];

                    virtualMouse = new VirtualMouse(characteristicsDictionary);
                    virtualKeyboard = new VirtualKeyboard(characteristicsDictionary);
                    virtualGamepad = new VirtualGamepad(characteristicsDictionary);

                    gattModeCharacteristic.ValueChanged += Mode_Characteristic_Value_Changed;
                    await gattModeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync
                        (GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    
                    logEvent(String.Format("Connection status {0}", bluetoothLEDevice.ConnectionStatus));
                }
                else
                {
                    logEvent(String.Format("Getting services status {0} {1}", result.Status, result.ProtocolError));
                }
            }
            else
            {
                logEvent("Attempt failed");

            }
        }

        private void Mode_Characteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);
            byte value = input[0];
            switch (value)
            {
                case (byte)MODES.GAMEPAD:
                    virtualGamepad.beginPolling();
                    break;
                case (byte)MODES.MOUSE:
                    virtualGamepad.stopPolling();
                    break;
                case (byte)MODES.KEYBOARD:
                    virtualGamepad.stopPolling();
                    break;
                default:
                    try
                    {
                        virtualGamepad.stopPolling();
                    }catch(Exception e) { }
                    break;
            }
        }
    }
}
