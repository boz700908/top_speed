using System;

namespace TS.Sdl.Input
{
    public readonly struct DeviceMetadata
    {
        public DeviceMetadata(
            uint instanceId,
            bool isGamepad,
            string? name,
            string? path,
            Guid guid,
            JoystickType joystickType,
            GamepadType gamepadType,
            int playerIndex,
            ushort vendorId,
            ushort productId,
            ushort productVersion,
            ushort firmwareVersion,
            string? serial)
        {
            InstanceId = instanceId;
            IsGamepad = isGamepad;
            Name = name ?? string.Empty;
            Path = path ?? string.Empty;
            Guid = guid;
            JoystickType = joystickType;
            GamepadType = gamepadType;
            PlayerIndex = playerIndex;
            VendorId = vendorId;
            ProductId = productId;
            ProductVersion = productVersion;
            FirmwareVersion = firmwareVersion;
            Serial = serial ?? string.Empty;
        }

        public uint InstanceId { get; }
        public bool IsGamepad { get; }
        public string Name { get; }
        public string Path { get; }
        public Guid Guid { get; }
        public JoystickType JoystickType { get; }
        public GamepadType GamepadType { get; }
        public int PlayerIndex { get; }
        public ushort VendorId { get; }
        public ushort ProductId { get; }
        public ushort ProductVersion { get; }
        public ushort FirmwareVersion { get; }
        public string Serial { get; }
    }
}
