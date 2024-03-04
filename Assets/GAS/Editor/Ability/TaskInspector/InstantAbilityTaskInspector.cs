﻿using GAS.Runtime.Ability;

namespace GAS.Editor.Ability
{
    public abstract class InstantAbilityTaskInspector:AbilityTaskInspector 
    {
        protected InstantAbilityTaskInspector(AbilityTaskBase taskBase) : base(taskBase)
        {
        }
    }
    
    public abstract class InstantAbilityTaskInspector<T>:InstantAbilityTaskInspector where T:InstantAbilityTask
    {
        protected T _task;
        protected InstantAbilityTaskInspector(AbilityTaskBase taskBase) : base(taskBase)
        {
            _task = _taskBase as T;
        }
    }
}