using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Input.Preview.Injection;

namespace BTKeyboardClient
{
    class VirtualKeyboard
    {
        private GattCharacteristic ScanCodeCharacteristic;
        private InputInjector inputInjector;
        private InjectedInputKeyboardInfo keyboardInfo;
        private List<InjectedInputKeyboardInfo> unclickList;


        public VirtualKeyboard(Dictionary<Guid, GattCharacteristic> characteristicsDictionary)
        {
            ScanCodeCharacteristic = characteristicsDictionary[Guid.Parse("f4509bd6-3c64-4a0e-b43d-2beb8bd44072")];

            setupKeyboard();
        }

        private enum SPECIAL_KEYS : ushort
        {
            CTRL = 0x1D, LSHIFT = 0x2A, RSHIFT = 0x36, ALT = 0x38
        }

        private async void setupKeyboard()
        {
            inputInjector = InputInjector.TryCreate();
            keyboardInfo = new InjectedInputKeyboardInfo()
            { KeyOptions = InjectedInputKeyOptions.ScanCode };
            unclickList = new List<InjectedInputKeyboardInfo>() {
                new InjectedInputKeyboardInfo() {
                    ScanCode=(ushort)SPECIAL_KEYS.LSHIFT,
                    KeyOptions=InjectedInputKeyOptions.KeyUp
                },
                new InjectedInputKeyboardInfo() {
                    ScanCode=(ushort)SPECIAL_KEYS.CTRL,
                    KeyOptions=InjectedInputKeyOptions.KeyUp
                },
                new InjectedInputKeyboardInfo() {
                    ScanCode=(ushort)SPECIAL_KEYS.RSHIFT,
                    KeyOptions=InjectedInputKeyOptions.KeyUp
                },
                new InjectedInputKeyboardInfo() {
                    ScanCode=(ushort)SPECIAL_KEYS.ALT,
                    KeyOptions=InjectedInputKeyOptions.KeyUp
                }
            };

            ScanCodeCharacteristic.ValueChanged += VirtualKeyCharacteristic_Value_Changed;
            await ScanCodeCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
        }

        private void VirtualKeyCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            List<InjectedInputKeyboardInfo> infoList = new List<InjectedInputKeyboardInfo>();
            var input = readValue(args);
            Debug.WriteLine(String.Format("input: {0}", input));
            foreach (ushort key in input)
            {
                Debug.WriteLine(String.Format("input: {0}", key));
                infoList.Add(new InjectedInputKeyboardInfo() { ScanCode = key });
            }
            inputInjector.InjectKeyboardInput(infoList);
            //unclick all special keys.
            inputInjector.InjectKeyboardInput(unclickList);

        }

        private short[] readValue(GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);
            short[] value = new short[input.Length / 2];
            System.Buffer.BlockCopy(input, 0, value, 0, input.Length);
            Debug.WriteLine(String.Format("input in: {0}", input));
            Debug.WriteLine(String.Format("value: {0}", value));

            return value;

        }

    }
}