using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTKeyboardClient
{
    class Constants
    {
        public readonly Dictionary<String, Guid> GAMEPAD_CHARACTERISTICS_UUIDS;
        public Constants()
        {
            GAMEPAD_CHARACTERISTICS_UUIDS = new Dictionary<string, Guid>
            {
                {"Buttons",Guid.Parse("5906134f-0a14-4eb8-871f-b67885d2733a")},
                {"LeftThumbstickX",Guid.Parse("5ed38605-4a0b-4039-bc64-2d0bd7e28274")},
                {"LeftThumbstickY",Guid.Parse("b61e1d2f-61fb-4b32-b693-439c7dc94c65")},
                {"LeftTrigger",Guid.Parse("389b8d8a-fad6-4be6-9a9a-8962a42d95eb")},
                {"RightThumbstickX",Guid.Parse("2f1ea192-4c39-4a2c-9371-63c6e12cac14")},
                {"RightThumbstickY",Guid.Parse("9db40e09-f3a8-4eb7-9e85-4aab96d2db14")},
                {"RightTrigger",Guid.Parse("1b6be6a0-d72c-4eb6-b1b1-053359043cf3")},
             };

        }
    }
}
