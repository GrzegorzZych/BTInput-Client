using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Gaming.Input;
using Windows.Storage.Streams;
using Windows.UI.Input.Preview.Injection;

namespace BTKeyboardClient
{
    class VirtualGamepad
    {
        private GattCharacteristic ButtonsCharacteristic, LeftThumbstickXCharacteristic,
            LeftThumbstickYCharacteristic, LeftTriggerCharacteristic, RightThumbstickXCharacteristic,
            RightThumbstickYCharacteristic, RightTriggerCharacteristic;

        //private GamepadReading gamepadReading;
        private InjectedInputGamepadInfo gamepadInfo;
        private InputInjector inputInjector;
        private bool isPolling = false;
        //private const int POLLING_DELAY = 1;

        public VirtualGamepad(Dictionary<Guid, GattCharacteristic> characteristicsDictionary)
        {
            
            ButtonsCharacteristic = characteristicsDictionary[Guid.Parse("5906134f-0a14-4eb8-871f-b67885d2733a")];
            LeftThumbstickXCharacteristic = characteristicsDictionary[Guid.Parse("5ed38605-4a0b-4039-bc64-2d0bd7e28274")];
            LeftThumbstickYCharacteristic = characteristicsDictionary[Guid.Parse("b61e1d2f-61fb-4b32-b693-439c7dc94c65")];
            LeftTriggerCharacteristic = characteristicsDictionary[Guid.Parse("389b8d8a-fad6-4be6-9a9a-8962a42d95eb")];
            RightThumbstickXCharacteristic = characteristicsDictionary[Guid.Parse("2f1ea192-4c39-4a2c-9371-63c6e12cac14")];
            RightThumbstickYCharacteristic = characteristicsDictionary[Guid.Parse("9db40e09-f3a8-4eb7-9e85-4aab96d2db14")];
            RightTriggerCharacteristic = characteristicsDictionary[Guid.Parse("1b6be6a0-d72c-4eb6-b1b1-053359043cf3")];
            setupKeyboard();
        }

        private async void setupKeyboard()
        {
            //gamepadReading = new GamepadReading();
            gamepadInfo = new InjectedInputGamepadInfo();
            inputInjector = InputInjector.TryCreate();
            inputInjector.InitializeGamepadInjection();

            ButtonsCharacteristic.ValueChanged += ButtonsCharacteristic_Value_Changed;
            await ButtonsCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            LeftThumbstickXCharacteristic.ValueChanged += LeftThumbstickXCharacteristic_Value_Changed;
            await LeftThumbstickXCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            LeftThumbstickYCharacteristic.ValueChanged += LeftThumbstickYCharacteristic_Value_Changed;
            await LeftThumbstickYCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            LeftTriggerCharacteristic.ValueChanged += LeftTriggerCharacteristic_Value_Changed;
            await LeftTriggerCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            RightThumbstickXCharacteristic.ValueChanged += RightThumbstickXCharacteristic_Value_Changed;
            await RightThumbstickXCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            RightThumbstickYCharacteristic.ValueChanged += RightThumbstickYCharacteristic_Value_Changed;
            await RightThumbstickYCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            RightTriggerCharacteristic.ValueChanged += RightTriggerCharacteristic_Value_Changed;
            await RightTriggerCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);


        }

        private void RightTriggerCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.RightTrigger =  float.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void RightThumbstickYCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.RightThumbstickY = float.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void RightThumbstickXCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.RightThumbstickX = float.Parse(readValue(args));
           // inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void LeftTriggerCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.LeftTrigger = float.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void LeftThumbstickYCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.LeftThumbstickY = float.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void LeftThumbstickXCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.LeftThumbstickX = float.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private void ButtonsCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            gamepadInfo.Buttons = (GamepadButtons)Int32.Parse(readValue(args));
            //inputInjector.InjectGamepadInput(gamepadInfo);
        }

        private String readValue(GattValueChangedEventArgs args)
        {
            
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);
            String value = System.Text.Encoding.UTF8.GetString(input);
            return value;

        }

        public void beginPolling()
        {
            isPolling = true;
            pollingTask();
        }

        private async void pollingTask()
        {
            while (isPolling)
            {
                inputInjector.InjectGamepadInput(gamepadInfo);
            }
        }

        internal void stopPolling()
        {
            isPolling = false;
        }
    }
}
