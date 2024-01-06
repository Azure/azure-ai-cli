using System;

public class ChatCompletionsCustomFunctions
{
    [HelperFunctionDescription("Gets the current weather for a location.")]
    public static string GetCurrentWeather(string location)
    {
        return $"The weather in {location} is 72 degrees and sunny.";
    }

    [HelperFunctionDescription("Gets the current date.")]
    public static string GetCurrentDate()
    {
        var date = DateTime.Now;
        return $"{date.Year}-{date.Month}-{date.Day}";
    }

    [HelperFunctionDescription("Gets the current time.")]
    public static string GetCurrentTime()
    {
        var date = DateTime.Now;
        return $"{date.Hour}:{date.Minute}:{date.Second}";
    }
}
