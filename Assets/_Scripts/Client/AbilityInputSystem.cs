using System.Linq;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class AbilityInputSystem : SystemBase {
  private MobaInputActions _inputActions;

  protected override void OnCreate() {
    RequireForUpdate<GamePlayingTag>();
     _inputActions = new MobaInputActions();
   }

  protected override void OnDestroy() {
     base.OnDestroy();
  }

  private void AoeAblility_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
    var newAbilityInput = new AbilityInput();
    //if(_inputActions.GameplayMap.AoeAblility.WasCompletedThisFrame()) {
    newAbilityInput.AoeAbility.Set();
    Debug.Log("AOE Ability Key Set");
    //}
    //Play all the keypress events into the system api noting we need to perform updates elsewhere
    foreach(var abilityInput in SystemAPI.Query<RefRW<AbilityInput>>()) {
      abilityInput.ValueRW = newAbilityInput;
    }
  }

  // This is a polling approach
  protected override void OnUpdate() {
    //Check for a keypress event
    var newAbilityInput = new AbilityInput();
    if(_inputActions.GameplayMap.AoeAblility.WasPressedThisFrame()) { // && _inputActions.GameplayMap.AoeAblility.WasCompletedThisFrame()) {
      newAbilityInput.AoeAbility.Set();
    }

    #region Aimed & confirmed abilities
    if(_inputActions.GameplayMap.SkillShotAbility.WasPressedThisFrame() 
        /*&& !newAbilityInput.ChargeAttack.IsSet*/) {
      newAbilityInput.SkillShotAbility.Set();
    }

    if(_inputActions.GameplayMap.ChargeAttack.WasPressedThisFrame() 
        /*&& !newAbilityInput.SkillShotAbility.IsSet*/) {
      newAbilityInput.ChargeAttack.Set();
    }

    if(_inputActions.GameplayMap.ConfirmSkillShotAbility.WasPressedThisFrame()) {
      newAbilityInput.ConfirmChargeAttack.Set();
      newAbilityInput.ConfirmSkillShotAbility.Set();
    }
    #endregion

    //Play all the keypress events into the system api noting we need to perform updates elsewhere
    foreach(var abilityInput in SystemAPI.Query<RefRW<AbilityInput>>()) {
      abilityInput.ValueRW = newAbilityInput;
    }
  }

  //The event driven approach to input is desireble for less frequent or non-continuous input
  protected override void OnStartRunning() {
    _inputActions.Enable();
    //_inputActions.GameplayMap.AoeAblility.canceled += AoeAblility_canceled;
  }


  protected override void OnStopRunning() {
    _inputActions.Disable();
    //_inputActions.GameplayMap.AoeAblility.canceled -= AoeAblility_canceled;
  }
}