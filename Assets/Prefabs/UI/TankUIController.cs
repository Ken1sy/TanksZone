using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameScripts.Visuals
{
    public class TankUIController : MonoBehaviour
    {
        [Header("Настройки позиции и скрытия")]
        public bool hideForLocalPlayer = true;
        public float heightOffset = 4.5f;
        public CanvasGroup mainCanvasGroup;
        public LayerMask obstacleMask = 1;

        [Header("Масштабирование (Для 2D Экрана)")]
        public bool scaleWithDistance = true;
        public float shrinkFactor = 0.008f;
        public float minScaleMultiplier = 0.4f;
        public float maxScaleMultiplier = 1.0f;

        [Header("Элементы UI")]
        public Slider healthSlider;
        public Slider reloadSlider;
        public Text nameText;
        public Image rankImage;

        [Tooltip("Перетащи сюда все иконки званий (от Новобранца до Легенды), точно так же как в Гараже")]
        public Sprite[] rankIcons; // НОВОЕ: Массив картинок званий

        [Header("Всплывающий Урон")]
        public RectTransform damageRoot;
        public TMP_Text damageText;
        public CanvasGroup damageCanvasGroup;
        public float damageDisplayTime = 1.5f;
        public float damageFloatSpeed = 50f;

        private PlayerTankBrain _brain;
        private UnityEngine.Camera _mainCam;
        private Vector3 _initialDamagePos;
        private Coroutine _damageCoroutine;
        private RectTransform _rectTransform;
        private Vector3 _baseScale;

        public void Initialize(PlayerTankBrain targetBrain)
        {
            _brain = targetBrain;
            _rectTransform = GetComponent<RectTransform>();

            _baseScale = Vector3.one;
            _rectTransform.localScale = _baseScale;

            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = GetComponent<CanvasGroup>();
                if (mainCanvasGroup == null) mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (damageRoot != null)
            {
                _initialDamagePos = damageRoot.localPosition;
                damageRoot.gameObject.SetActive(false);

                if (damageCanvasGroup == null)
                {
                    damageCanvasGroup = damageRoot.GetComponent<CanvasGroup>();
                    if (damageCanvasGroup == null) damageCanvasGroup = damageRoot.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (reloadSlider != null)
            {
                reloadSlider.gameObject.SetActive(_brain.IsOwner);
            }
        }

        private void LateUpdate()
        {
            if (_brain == null) return;

            if (_mainCam == null)
            {
                _mainCam = UnityEngine.Camera.main;
                if (_mainCam == null) _mainCam = FindFirstObjectByType<UnityEngine.Camera>();
                if (_mainCam == null) return;
            }

            if (hideForLocalPlayer && _brain.IsOwner)
            {
                if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                return;
            }

            // ==========================================
            // ОБНОВЛЕНИЕ ЗНАЧЕНИЙ UI (Имя и Ранг)
            // ==========================================
            if (nameText != null) nameText.text = _brain.playerName.Value;

            // НОВОЕ: Установка иконки ранга
            if (rankImage != null && rankIcons != null && rankIcons.Length > 0)
            {
                // Звание 1 соответствует индексу 0, звание 2 -> индексу 1 и т.д.
                // Mathf.Clamp защитит от ошибки, если звание вдруг окажется больше размера массива
                int rankIndex = Mathf.Clamp(_brain.playerRank.Value - 1, 0, rankIcons.Length - 1);
                rankImage.sprite = rankIcons[rankIndex];
            }

            if (healthSlider != null)
            {
                healthSlider.maxValue = Mathf.Max(1f, _brain.maxHealth.Value);
                healthSlider.value = _brain.currentHealth.Value;
            }

            if (reloadSlider != null && _brain.weapon != null && _brain.IsOwner)
            {
                reloadSlider.maxValue = 1f;
                reloadSlider.value = _brain.weapon.GetReloadProgress();
            }

            // ОТСЛЕЖИВАНИЕ ЭКРАНА
            Vector3 worldPos = _brain.transform.position + Vector3.up * heightOffset;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

            if (screenPos.z < 0)
            {
                if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
                return;
            }

            _rectTransform.position = screenPos;

            // МАСШТАБИРОВАНИЕ
            if (scaleWithDistance)
            {
                float dist = Vector3.Distance(_brain.transform.position, _mainCam.transform.position);
                float mult = Mathf.Clamp(1f - (dist * shrinkFactor), minScaleMultiplier, maxScaleMultiplier);
                _rectTransform.localScale = _baseScale * mult;
            }

            // ПРОВЕРКА НА СТЕНЫ
            if (mainCanvasGroup != null)
            {
                Vector3 dirToCam = _mainCam.transform.position - worldPos;
                if (Physics.Raycast(worldPos, dirToCam.normalized, out RaycastHit hit, dirToCam.magnitude, obstacleMask))
                {
                    if (hit.collider.transform.root != _brain.transform.root) mainCanvasGroup.alpha = 0f;
                    else mainCanvasGroup.alpha = 1f;
                }
                else mainCanvasGroup.alpha = 1f;
            }
        }

        public void UpdateHealth(float current, float max) { }

        public void ShowDamage(float damageAmount)
        {
            // НОВОЕ: Проверяем глобальную настройку! Если игрок отключил урон - выходим из функции.
            if (!GameScripts.UI.SettingsMenuController.ShowDamage) return;

            if (damageRoot == null || damageText == null) return;
            if (_damageCoroutine != null) StopCoroutine(_damageCoroutine);
            _damageCoroutine = StartCoroutine(DamageRoutine(damageAmount));
        }

        private IEnumerator DamageRoutine(float damageAmount)
        {
            damageRoot.gameObject.SetActive(true);
            damageText.text = $"-{Mathf.RoundToInt(damageAmount)}";
            damageRoot.localPosition = _initialDamagePos;

            float elapsed = 0f;
            while (elapsed < damageDisplayTime)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / damageDisplayTime;

                damageRoot.localPosition = _initialDamagePos + Vector3.up * (percent * damageFloatSpeed);
                if (damageCanvasGroup != null) damageCanvasGroup.alpha = 1f - (percent * percent);

                yield return null;
            }
            damageRoot.gameObject.SetActive(false);
        }
    }
}