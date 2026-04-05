using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Controller;
using TS.Sdl.Input;

namespace TopSpeed.Input.Backends.Sdl
{
    internal static class ChoiceMap
    {
        public static List<DiscoveredDevice> Discover()
        {
            var ids = Joystick.GetIds();
            var discovered = new List<DiscoveredDevice>(ids.Length);
            for (var i = 0; i < ids.Length; i++)
            {
                var instanceId = ids[i];
                var isGamepad = Gamepad.IsGamepad(instanceId);
                var metadata = isGamepad
                    ? Gamepad.GetMetadataForId(instanceId)
                    : Joystick.GetMetadataForId(instanceId);
                var isRacingWheel = metadata.JoystickType == JoystickType.Wheel || LooksLikeWheel(metadata.Name);
                var choiceGuid = CreateChoiceGuid(instanceId, isGamepad);
                var displayName = Display.BuildChoiceLabel(metadata, isRacingWheel);
                var choice = new Choice(choiceGuid, displayName, isRacingWheel);
                discovered.Add(new DiscoveredDevice(instanceId, isGamepad, metadata, choice));
            }

            discovered.Sort((left, right) =>
                string.Compare(left.Choice.DisplayName, right.Choice.DisplayName, StringComparison.CurrentCultureIgnoreCase));
            return discovered;
        }

        private static bool LooksLikeWheel(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var value = name!.ToLowerInvariant();
            return value.Contains("wheel")
                || value.Contains("steering")
                || value.Contains("pedal")
                || value.Contains("racing");
        }

        private static Guid CreateChoiceGuid(uint instanceId, bool isGamepad)
        {
            var bytes = new byte[16];
            System.Text.Encoding.ASCII.GetBytes("TS-SDL3").CopyTo(bytes, 0);
            Array.Copy(BitConverter.GetBytes(instanceId), 0, bytes, 8, 4);
            Array.Copy(BitConverter.GetBytes(isGamepad ? 1u : 0u), 0, bytes, 12, 4);
            return new Guid(bytes);
        }
    }

    internal sealed class DiscoveredDevice
    {
        public DiscoveredDevice(uint instanceId, bool isGamepad, DeviceMetadata metadata, Choice choice)
        {
            InstanceId = instanceId;
            IsGamepad = isGamepad;
            Metadata = metadata;
            Choice = choice;
        }

        public uint InstanceId { get; }
        public bool IsGamepad { get; }
        public DeviceMetadata Metadata { get; }
        public Choice Choice { get; }

        public DiscoveredDevice WithChoice(Choice choice)
        {
            return new DiscoveredDevice(InstanceId, IsGamepad, Metadata, choice);
        }
    }
}
