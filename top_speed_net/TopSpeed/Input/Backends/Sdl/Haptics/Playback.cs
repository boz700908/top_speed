using System;
using System.Collections.Generic;
using TopSpeed.Input.Devices.Vibration;
using TS.Sdl.Input;

namespace TopSpeed.Input.Backends.Sdl
{
    internal sealed partial class Device
    {
        private const uint Infinite = 0xFFFFFFFFu;
        private readonly Dictionary<VibrationEffectType, FeedbackState> _feedback = new Dictionary<VibrationEffectType, FeedbackState>();
        private readonly Dictionary<VibrationEffectType, HapticEffectHandle> _handles = new Dictionary<VibrationEffectType, HapticEffectHandle>();
        private readonly HashSet<VibrationEffectType> _unsupportedEffects = new HashSet<VibrationEffectType>();

        public void PlayEffect(VibrationEffectType type, int intensity = 10000)
        {
            var state = CreateFeedback(type, intensity);
            state.RunPending = true;
            _feedback[type] = state;
            ApplyFeedback();
        }

        public void StopEffect(VibrationEffectType type)
        {
            if (_feedback.Remove(type))
            {
                StopHapticEffect(type);
                ApplyFeedback();
            }
        }

        public void Gain(VibrationEffectType type, int value)
        {
            if (!_feedback.TryGetValue(type, out var state))
                return;

            state.Gain = value;
            _feedback[type] = state;
            ApplyFeedback();
        }

        private void StopAllFeedback()
        {
            _feedback.Clear();

            foreach (var handle in _handles.Values)
                handle.Dispose();

            _handles.Clear();
            _unsupportedEffects.Clear();

            if (_gamepad != null)
            {
                _gamepad.Rumble(0, 0, 0);
                _gamepad.RumbleTriggers(0, 0, 0);
            }

            if (_haptic != null)
            {
                _haptic.StopRumble();
                _haptic.StopAllEffects();
                if ((_haptic.Features & HapticFeatures.AutoCenter) != 0)
                    _haptic.SetAutoCenter(0);
            }
        }

        private void ApplyFeedback()
        {
            var low = 0f;
            var high = 0f;
            var leftTrigger = 0f;
            var rightTrigger = 0f;
            var activeTypes = new List<VibrationEffectType>(_feedback.Keys);
            for (var i = 0; i < activeTypes.Count; i++)
            {
                var type = activeTypes[i];
                var state = _feedback[type];
                var handledByHaptics = TryApplyHapticEffect(type, ref state);
                _feedback[type] = state;
                if (handledByHaptics)
                    continue;

                var gain = Clamp01(state.Gain / 10000f);
                low = Math.Max(low, state.Low * gain);
                high = Math.Max(high, state.High * gain);
                leftTrigger = Math.Max(leftTrigger, state.LeftTrigger * gain);
                rightTrigger = Math.Max(rightTrigger, state.RightTrigger * gain);
            }

            ApplyRumbleFallback(low, high, leftTrigger, rightTrigger);
        }

        private void ApplyRumbleFallback(float low, float high, float leftTrigger, float rightTrigger)
        {
            if (_gamepad != null)
            {
                _gamepad.Rumble(ToMotor(low), ToMotor(high), 1000);
                _gamepad.RumbleTriggers(ToMotor(leftTrigger), ToMotor(rightTrigger), 1000);
            }

            if (_haptic == null)
                return;

            if ((_haptic.Features & HapticFeatures.AutoCenter) != 0)
            {
                var autoCenter = (int)Math.Round(Clamp01((leftTrigger + rightTrigger) * 0.5f) * 100.0f);
                _haptic.SetAutoCenter(autoCenter);
            }

            if (_haptic.SupportsRumble())
            {
                var strength = Clamp01(Math.Max(Math.Max(low, high), Math.Max(leftTrigger, rightTrigger)));
                if (strength > 0f)
                    _haptic.Rumble(strength, 1000);
                else
                    _haptic.StopRumble();
            }
        }

        private bool TryApplyHapticEffect(VibrationEffectType type, ref FeedbackState state)
        {
            if (_haptic == null || _unsupportedEffects.Contains(type))
                return false;

            if (!TryBuildEffect(type, state, out var effect, out var mode))
            {
                _unsupportedEffects.Add(type);
                return false;
            }

            if (!_handles.TryGetValue(type, out var handle))
            {
                if (!_haptic.SupportsEffect(effect))
                {
                    _unsupportedEffects.Add(type);
                    return false;
                }

                handle = _haptic.CreateEffect(effect);
                if (handle == null)
                {
                    _unsupportedEffects.Add(type);
                    return false;
                }

                _handles[type] = handle;
            }
            else
            {
                _haptic.UpdateEffect(handle, effect);
            }

            if (state.RunPending)
            {
                _haptic.RunEffect(handle, 1);
                state.RunPending = false;
                return true;
            }

            if (mode == HapticPlaybackMode.Periodic || mode == HapticPlaybackMode.Condition)
            {
                if (!_haptic.IsEffectPlaying(handle))
                    _haptic.RunEffect(handle, 1);

                return true;
            }

            return true;
        }

        private void StopHapticEffect(VibrationEffectType type)
        {
            if (_haptic == null)
                return;

            if (_handles.TryGetValue(type, out var handle))
                _haptic.StopEffect(handle);
        }

        private bool TryBuildEffect(VibrationEffectType type, FeedbackState state, out HapticEffect effect, out HapticPlaybackMode mode)
        {
            var gain = Clamp01(state.Gain / 10000f);
            effect = default;
            mode = HapticPlaybackMode.None;

            switch (type)
            {
                case VibrationEffectType.Start:
                    effect.Constant = new HapticConstantEffect
                    {
                        Type = (ushort)HapticEffectType.Constant,
                        Direction = HapticDirection.SteeringAxis(),
                        Length = 180,
                        Level = ScaleSigned(0.55f * gain),
                        AttackLength = 10,
                        FadeLength = 60
                    };
                    mode = HapticPlaybackMode.Constant;
                    return true;

                case VibrationEffectType.Crash:
                    effect.Constant = new HapticConstantEffect
                    {
                        Type = (ushort)HapticEffectType.Constant,
                        Direction = HapticDirection.SteeringAxis(),
                        Length = 320,
                        Level = ScaleSigned(1.0f * gain),
                        FadeLength = 120
                    };
                    mode = HapticPlaybackMode.Constant;
                    return true;

                case VibrationEffectType.Engine:
                    effect.Periodic = new HapticPeriodicEffect
                    {
                        Type = (ushort)HapticEffectType.Sine,
                        Direction = HapticDirection.SteeringAxis(),
                        Length = Infinite,
                        Period = 90,
                        Magnitude = ScaleSigned(0.26f * gain)
                    };
                    mode = HapticPlaybackMode.Periodic;
                    return true;

                case VibrationEffectType.Gravel:
                    effect.Periodic = new HapticPeriodicEffect
                    {
                        Type = (ushort)HapticEffectType.Triangle,
                        Direction = HapticDirection.SteeringAxis(),
                        Length = Infinite,
                        Period = 45,
                        Magnitude = ScaleSigned(0.38f * gain),
                        FadeLength = 20
                    };
                    mode = HapticPlaybackMode.Periodic;
                    return true;

                case VibrationEffectType.Spring:
                    var spring = new HapticConditionEffect
                    {
                        Type = (ushort)HapticEffectType.Spring,
                        Direction = HapticDirection.SteeringAxis(),
                        Length = Infinite
                    };
                    var coeff = ScaleSigned(0.35f * gain);
                    spring.SetAxis(0, 0x7FFF, 0x7FFF, coeff, coeff);
                    effect.Condition = spring;
                    mode = HapticPlaybackMode.Condition;
                    return true;

                case VibrationEffectType.CurbLeft:
                case VibrationEffectType.BumpLeft:
                    effect.Constant = new HapticConstantEffect
                    {
                        Type = (ushort)HapticEffectType.Constant,
                        Direction = HapticDirection.Polar(27000),
                        Length = type == VibrationEffectType.CurbLeft ? 140u : 120u,
                        Level = ScaleSigned(0.65f * gain),
                        FadeLength = 40
                    };
                    mode = HapticPlaybackMode.Constant;
                    return true;

                case VibrationEffectType.CurbRight:
                case VibrationEffectType.BumpRight:
                    effect.Constant = new HapticConstantEffect
                    {
                        Type = (ushort)HapticEffectType.Constant,
                        Direction = HapticDirection.Polar(9000),
                        Length = type == VibrationEffectType.CurbRight ? 140u : 120u,
                        Level = ScaleSigned(0.65f * gain),
                        FadeLength = 40
                    };
                    mode = HapticPlaybackMode.Constant;
                    return true;
            }

            return false;
        }

        private static FeedbackState CreateFeedback(VibrationEffectType type, int gain)
        {
            var state = new FeedbackState { Gain = gain };
            switch (type)
            {
                case VibrationEffectType.Start:
                    state.Low = 0.55f;
                    state.High = 0.55f;
                    break;
                case VibrationEffectType.Crash:
                    state.Low = 1.0f;
                    state.High = 1.0f;
                    state.LeftTrigger = 0.8f;
                    state.RightTrigger = 0.8f;
                    break;
                case VibrationEffectType.Engine:
                    state.Low = 0.22f;
                    state.High = 0.08f;
                    break;
                case VibrationEffectType.Gravel:
                    state.Low = 0.32f;
                    state.High = 0.42f;
                    break;
                case VibrationEffectType.Spring:
                    state.LeftTrigger = 0.35f;
                    state.RightTrigger = 0.35f;
                    break;
                case VibrationEffectType.CurbLeft:
                case VibrationEffectType.BumpLeft:
                    state.Low = 0.25f;
                    state.High = 0.6f;
                    state.LeftTrigger = 0.7f;
                    break;
                case VibrationEffectType.CurbRight:
                case VibrationEffectType.BumpRight:
                    state.Low = 0.25f;
                    state.High = 0.6f;
                    state.RightTrigger = 0.7f;
                    break;
            }

            return state;
        }

        private static ushort ToMotor(float value)
        {
            return (ushort)Math.Round(Clamp01(value) * ushort.MaxValue);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        private static short ScaleSigned(float value)
        {
            return (short)Math.Round(Clamp01(value) * short.MaxValue);
        }
    }
}
