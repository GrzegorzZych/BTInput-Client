﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using Windows.UI.Input.Preview.Injection;

namespace BTKeyboardClient
{
    class VirtualMouse
    {
        private GattCharacteristic DeltaXCharacteristic, DeltaYCharacteristic, MouseOptionsCharacteristic;
        private List<InjectedInputMouseInfo> mouseInfoList;
        private InjectedInputMouseInfo mouseInfo;
        private InputInjector inputInjector;
        private const int SENSITIVITY_THRESHOLD = 10;
        private const int SMOOTHING_THRESHOLD = 3;

        public VirtualMouse(Dictionary<Guid, GattCharacteristic> characteristicsDictionary)
        {
            DeltaXCharacteristic = characteristicsDictionary[Guid.Parse("c1463681-fa4b-4c89-978d-cac6ee299dd9")];
            DeltaYCharacteristic = characteristicsDictionary[Guid.Parse("8994394d-2e98-4b9f-9638-8d9d8f7bc2d7")];
            MouseOptionsCharacteristic = characteristicsDictionary[Guid.Parse("a73e4e39-a963-4523-bd2d-3d74ec4a0a08")];
            setupMouse();
        }

        private async void setupMouse()
        {
            mouseInfoList = new List<InjectedInputMouseInfo>();
            inputInjector = InputInjector.TryCreate();
            mouseInfo = new InjectedInputMouseInfo()
            {
                DeltaY = 0,
                DeltaX = 0
            };



            DeltaXCharacteristic.ValueChanged += DeltaXCharacteristic_Value_Changed;
            await DeltaXCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            DeltaYCharacteristic.ValueChanged += DeltaYCharacteristic_Value_Changed;
            await DeltaYCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            MouseOptionsCharacteristic.ValueChanged += MouseOptionsCharacteristic_Value_Changed;
            await MouseOptionsCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);
        }

        private void MouseOptionsCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            inputInjector.InjectMouseInput(
                new InjectedInputMouseInfo[] { new InjectedInputMouseInfo()
                {
                    MouseOptions = (InjectedInputMouseOptions)int.Parse(readValue(args))
                } });
        }

        private void DeltaYCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var delta = int.Parse(readValue(args));
            if (delta != 0)
            {
                if (delta > SENSITIVITY_THRESHOLD)
                {
                    delta = SENSITIVITY_THRESHOLD;
                }
                else if (delta < -SENSITIVITY_THRESHOLD)
                {
                    delta = -SENSITIVITY_THRESHOLD;
                }

                inputInjector.InjectMouseInput(
                new InjectedInputMouseInfo[] { new InjectedInputMouseInfo(){
                        DeltaY = delta
                    }
                });
            }
        }

        private void DeltaXCharacteristic_Value_Changed(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var delta = int.Parse(readValue(args));
            if (delta != 0)
            {
                if (delta > SENSITIVITY_THRESHOLD)
                {
                    delta = SENSITIVITY_THRESHOLD;
                }
                else if (delta < -SENSITIVITY_THRESHOLD)
                {
                    delta = -SENSITIVITY_THRESHOLD;
                }

                inputInjector.InjectMouseInput(
                new InjectedInputMouseInfo[] { new InjectedInputMouseInfo(){
                        DeltaX = delta
                    }
                });
            }

        }

        private String readValue(GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] input = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(input);
            String value = System.Text.Encoding.UTF8.GetString(input);
            return value;

        }

    }
}
