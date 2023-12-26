﻿using System.Collections.Generic;
using GAS.Runtime.Ability;
using GAS.Runtime.Attribute;
using GAS.Runtime.Effects;
using GAS.Runtime.Tags;

namespace GAS.Runtime.Component
{
    public interface IAbilitySystemComponent
    {
        bool HasAllTags(GameplayTagSet tags);
        
        bool HasAnyTags(GameplayTagSet tags);
        
        void AddTags(GameplayTagSet tags);
        
        void RemoveTags(GameplayTagSet tags);

        GameplayEffectSpec ApplyGameplayEffectTo(GameplayEffect gameplayEffect,AbilitySystemComponent target);
        
        GameplayEffectSpec ApplyGameplayEffectToSelf(GameplayEffect gameplayEffect);
        
        void RemoveGameplayEffect(GameplayEffectSpec spec);
        
        void Tick();
        
        Dictionary<string,float> DataSnapshot();
        
        void GrantAbility(AbstractAbility ability);
        
        void RemoveAbility(string abilityName);
        
        AttributeBase GetAttribute(string setName,string attributeShortName);
    }
}