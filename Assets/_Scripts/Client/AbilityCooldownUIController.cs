using UnityEngine;
using UnityEngine.UI;


public class AbilityCooldownUIController : MonoBehaviour {
  public static AbilityCooldownUIController Instance;

  [SerializeField] private Image _aoeAbilityMask;
  [SerializeField] private Image _skillShotAbilityMask;
  [SerializeField] private Image _chargeAttackMask;
  [SerializeField] private Image _autoattackMask;

  private void Awake() {
    if(Instance != null) {
      Destroy(gameObject);
      return;
    }

    Instance = this;
  }

  private void Start() {
    _aoeAbilityMask.fillAmount = 0f;
    _skillShotAbilityMask.fillAmount = 0f;
    _chargeAttackMask.fillAmount = 0f;
    _autoattackMask.fillAmount = 0f;
  }

  public void UpdateAoeMask(float fillAmount) {
    _aoeAbilityMask.fillAmount = fillAmount;
  }

  public void UpdateSkillShotMask(float fillAmount) {
    _skillShotAbilityMask.fillAmount = fillAmount;
  }

  public void UpdateChargeMask(float fillAmount) {
    _chargeAttackMask.fillAmount = fillAmount;
  }

  public void UpdateAutoMask(float fillAmount) {
    _autoattackMask.fillAmount = fillAmount;
  }
}