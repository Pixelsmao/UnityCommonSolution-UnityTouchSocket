using SRDebugger.Services;
using SRF.Service;
using System;
using UnityEngine;

namespace Assets.StompyRobot.SRDebugger.Scripts.Services.Implementation
{
    [Service(typeof(IConsoleFilterState))]
    public sealed class ConsoleFilterStateService : IConsoleFilterState
    {
        public event ConsoleStateChangedEventHandler FilterStateChange;

        private readonly bool[] _states;

        public ConsoleFilterStateService()
        {
            this._states = new bool[Enum.GetValues(typeof(LogType)).Length];
            for (var i = 0; i < this._states.Length; i++)
            {
                this._states[i] = true;
            }
        }

        public void SetConsoleFilterState(LogType type, bool newState)
        {
            type = GetType(type);
            if (this._states[(int)type] == newState)
            {
                return;
            }

            //Debug.Log($"FilterState changed {type} {!newState} -> {newState}");

            this._states[(int)type] = newState;
            FilterStateChange?.Invoke(type, newState);
        }

        public bool GetConsoleFilterState(LogType type)
        {
            type = GetType(type);
            return this._states[(int)type];
        }

        private static LogType GetType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return LogType.Error;
                default:
                    return type;
            }
        }
    }
}