using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static DaftAppleGames.VehicleEnhancements_BZ.VehicleEnhancementsPlugin_BZ;

namespace DaftAppleGames.VehicleEnhancements_BZ.UI
{
    internal class TimeAndWeatherController : MonoBehaviour
    {
        [SerializeField, Required] private GameObject timeAndWeatherRoot;
        [SerializeField, Required] private TextMeshProUGUI timeText;
        [SerializeField, Required] private RectTransform timeOfDayIndicator;
        [SerializeField, Required] private Image timeImage;
        [SerializeField, Required] private Sprite dayTimeSprite;
        [SerializeField, Required] private Sprite nightTimeSprite;
        [SerializeField, Required] private GameObject weatherIndicator;
        [SerializeField, Required] private Image weatherImage;
        [SerializeField, Required] private Sprite sunnyWeatherSprite;
        [SerializeField, Required] private Sprite partlyCloudyWeatherSprite;
        [SerializeField, Required] private Sprite cloudyWeatherSprite;
        [SerializeField, Required] private Sprite rainyWeatherSprite;
        [SerializeField, Required] private Sprite snowyWeatherSprite;
        [SerializeField, Required] private Sprite hailWeatherSprite;
        [SerializeField, Required] private Sprite lightningStormWeatherSprite;
        [SerializeField, Required] private Sprite fogWeatherSprite;
        [SerializeField, Required] private Sprite windBlizzardWeatherSprite;
        [SerializeField, Required] private Sprite dustStormWeatherSprite;
        [SerializeField, Required] private Sprite meteorWeatherSprite;
        [SerializeField, Required] private Sprite auroraWeatherSprite;

        private RectTransform previousMoon;
        private RectTransform nextMoon;
        private int lastDisplayedMinute = -1;
        private float nextWeatherUpdateTime;
        private EnhancedVehicle vehicle = EnhancedVehicle.Seatruck;

        internal void Configure(EnhancedVehicle selectedVehicle)
        {
            vehicle = selectedVehicle;
        }

        private void Awake()
        {
            if (!timeAndWeatherRoot || !timeText || !timeOfDayIndicator || !timeImage ||
                !dayTimeSprite || !nightTimeSprite || !weatherIndicator || !weatherImage ||
                !sunnyWeatherSprite || !partlyCloudyWeatherSprite ||
                !cloudyWeatherSprite || !rainyWeatherSprite || !snowyWeatherSprite ||
                !hailWeatherSprite || !lightningStormWeatherSprite || !fogWeatherSprite ||
                !windBlizzardWeatherSprite || !dustStormWeatherSprite ||
                !meteorWeatherSprite || !auroraWeatherSprite)
            {
                ModDebugLog.LogError("Could not find the time and weather indicator objects or sprites.");
                enabled = false;
                return;
            }

            if (timeImage.transform.parent != timeOfDayIndicator ||
                weatherImage.transform.parent != weatherIndicator.transform)
            {
                ModDebugLog.LogError("Time and weather images are not children of their indicators.");
                enabled = false;
                return;
            }

            if (!timeOfDayIndicator.GetComponent<RectMask2D>())
            {
                timeOfDayIndicator.gameObject.AddComponent<RectMask2D>();
            }

            timeImage.sprite = dayTimeSprite;
            timeImage.raycastTarget = false;
            previousMoon = CreateMoon("PreviousMoon");
            nextMoon = CreateMoon("NextMoon");
            timeText.SetText("--:--");
            weatherImage.enabled = false;
            timeAndWeatherRoot.SetActive(ConfigFile.IsTimeAndWeatherEnabled(vehicle));
        }

        private void Update()
        {
            bool showTimeAndWeather = ConfigFile.IsTimeAndWeatherEnabled(vehicle);
            if (timeAndWeatherRoot.activeSelf != showTimeAndWeather)
            {
                timeAndWeatherRoot.SetActive(showTimeAndWeather);
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

        private RectTransform CreateMoon(string objectName)
        {
            Image moonImage = Instantiate(timeImage, timeOfDayIndicator);
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

            float viewportHeight = timeOfDayIndicator.rect.height;
            float sunPosition = (dayFraction * 2.0f - 1.0f) * viewportHeight;
            timeImage.rectTransform.anchoredPosition = new Vector2(0.0f, sunPosition);
            previousMoon.anchoredPosition = new Vector2(0.0f, sunPosition + viewportHeight);
            nextMoon.anchoredPosition = new Vector2(0.0f, sunPosition - viewportHeight);
        }

        private void UpdateWeather()
        {
            WeatherManager weatherManager = WeatherManager.main;
            WeatherEvent weather = weatherManager ? weatherManager.GetCurrentWeatherConditions() : null;
            if (weather == null)
            {
                weatherImage.enabled = false;
                return;
            }

            WeatherParameters conditions = weather.parameters;
            Sprite sprite;
            if (conditions.lightningIntensity >= 0.25f)
            {
                sprite = lightningStormWeatherSprite;
            }
            else if (conditions.hailIntensity >= 0.1f)
            {
                sprite = hailWeatherSprite;
            }
            else if (conditions.meteorIntensity >= 0.25f)
            {
                sprite = meteorWeatherSprite;
            }
            else if (conditions.snowIntensity >= 0.2f && conditions.windSpeed >= 30.0f)
            {
                sprite = windBlizzardWeatherSprite;
            }
            else if (conditions.snowIntensity >= 0.1f)
            {
                sprite = snowyWeatherSprite;
            }
            else if (conditions.rainIntensity >= 0.1f)
            {
                sprite = rainyWeatherSprite;
            }
            else if (conditions.smokinessIntensity >= 0.25f && conditions.windSpeed >= 25.0f)
            {
                sprite = dustStormWeatherSprite;
            }
            else if (conditions.fogDensity >= 0.12f)
            {
                sprite = fogWeatherSprite;
            }
            else if (conditions.windSpeed >= 30.0f)
            {
                sprite = windBlizzardWeatherSprite;
            }
            else if (conditions.auroraBorealisIntensity >= 0.25f)
            {
                sprite = auroraWeatherSprite;
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

            if (weatherImage.sprite != sprite)
            {
                weatherImage.sprite = sprite;
            }

            weatherImage.enabled = true;
        }
    }
}
