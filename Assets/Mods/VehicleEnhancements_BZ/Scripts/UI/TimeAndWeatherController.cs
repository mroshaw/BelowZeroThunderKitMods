using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.UI
{
    internal class TimeAndWeatherController : MonoBehaviour
    {
        [SerializeField, Required] private TextMeshProUGUI timeText;
        [SerializeField, Required] private RectTransform dayNightIndicator;
        [SerializeField, Required] private Image dayTimeImage;
        [SerializeField, Required] private Sprite nightTimeSprite;
        [SerializeField, Required] private Image weatherIndicator;
        [SerializeField, Required] private Sprite sunnyWeatherSprite;
        [SerializeField, Required] private Sprite partlyCloudyWeatherSprite;
        [SerializeField, Required] private Sprite cloudyWeatherSprite;
        [SerializeField, Required] private Sprite rainyWeatherSprite;
        [SerializeField, Required] private Sprite snowyWeatherSprite;

        private RectTransform previousMoon;
        private RectTransform nextMoon;
        private int lastDisplayedMinute = -1;
        private float nextWeatherUpdateTime;
        private bool isVisible;
        private EnhancedVehicle vehicle = EnhancedVehicle.Seatruck;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!timeText || !dayNightIndicator || !dayTimeImage || !nightTimeSprite ||
                !weatherIndicator || !sunnyWeatherSprite || !partlyCloudyWeatherSprite ||
                !cloudyWeatherSprite || !rainyWeatherSprite || !snowyWeatherSprite)
            {
                ModDebugLog.LogError("Could not find the time and weather indicator objects or sprites.");
                enabled = false;
                return;
            }

            if (!dayNightIndicator.GetComponent<RectMask2D>())
            {
                dayNightIndicator.gameObject.AddComponent<RectMask2D>();
            }

            dayTimeImage.raycastTarget = false;
            previousMoon = CreateMoon("PreviousMoon");
            nextMoon = CreateMoon("NextMoon");
            timeText.SetText("--:--");
            weatherIndicator.enabled = false;
            SetVisibility(ConfigFile.IsTimeAndWeatherEnabled(vehicle));
        }

        private void Update()
        {
            bool showTimeAndWeather = ConfigFile.IsTimeAndWeatherEnabled(vehicle);
            if (isVisible != showTimeAndWeather)
            {
                SetVisibility(showTimeAndWeather);
            }

            if (!showTimeAndWeather)
            {
                return;
            }

            DayNightCycle dayNightCycle = DayNightCycle.main;
            if (dayNightCycle)
            {
                UpdateTime(dayNightCycle.GetDayScalar());
            }

            if (Time.unscaledTime >= nextWeatherUpdateTime)
            {
                nextWeatherUpdateTime = Time.unscaledTime + 1.0f;
                UpdateWeather();
            }
        }

        private void SetVisibility(bool visible)
        {
            isVisible = visible;
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(visible);
            }
        }

        private RectTransform CreateMoon(string objectName)
        {
            Image moonImage = Instantiate(dayTimeImage, dayNightIndicator);
            moonImage.name = objectName;
            moonImage.sprite = nightTimeSprite;
            moonImage.raycastTarget = false;
            return moonImage.rectTransform;
        }

        private void UpdateTime(float dayFraction)
        {
            int minuteOfDay = Mathf.FloorToInt(dayFraction * 1440.0f);
            if (minuteOfDay != lastDisplayedMinute)
            {
                lastDisplayedMinute = minuteOfDay;
                timeText.SetText("{0:00}:{1:00}", minuteOfDay / 60, minuteOfDay % 60);
            }

            float viewportHeight = dayNightIndicator.rect.height;
            float sunPosition = (dayFraction * 2.0f - 1.0f) * viewportHeight;
            dayTimeImage.rectTransform.anchoredPosition = new Vector2(0.0f, sunPosition);
            previousMoon.anchoredPosition = new Vector2(0.0f, sunPosition + viewportHeight);
            nextMoon.anchoredPosition = new Vector2(0.0f, sunPosition - viewportHeight);
        }

        private void UpdateWeather()
        {
            WeatherManager weatherManager = WeatherManager.main;
            WeatherEvent weather = weatherManager ? weatherManager.GetCurrentWeatherConditions() : null;
            if (weather == null)
            {
                weatherIndicator.enabled = false;
                return;
            }

            WeatherParameters conditions = weather.parameters;
            Sprite sprite;
            if (conditions.snowIntensity >= 0.1f || conditions.hailIntensity >= 0.1f)
            {
                sprite = snowyWeatherSprite;
            }
            else if (conditions.rainIntensity >= 0.1f)
            {
                sprite = rainyWeatherSprite;
            }
            else if (conditions.cloudCoverage >= 0.7f)
            {
                sprite = cloudyWeatherSprite;
            }
            else if (conditions.cloudCoverage >= 0.3f)
            {
                sprite = partlyCloudyWeatherSprite;
            }
            else
            {
                sprite = sunnyWeatherSprite;
            }

            if (weatherIndicator.sprite != sprite)
            {
                weatherIndicator.sprite = sprite;
            }

            weatherIndicator.enabled = true;
        }
    }
}
