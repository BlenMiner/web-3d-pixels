using System;

public static class Logger
{
    public static void Log(string message)
    {
#if UNITY
        UnityEngine.Debug.Log(message);
#else
        Console.WriteLine(message);
#endif
    }
    
    public static void LogWarning(string message)
    {
#if UNITY
        UnityEngine.Debug.LogWarning(message);
#else
        Console.WriteLine(message);
#endif
    }
    
    public static void Log(Exception ex)
    {
#if UNITY
        UnityEngine.Debug.LogException(ex);
#else
        Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
#endif
    }
    
    public static void LogOnServer(string message)
    {
#if !UNITY
        Console.WriteLine(message);
#endif
    }
    
    public static void LogOnClient(string message)
    {
#if UNITY
        UnityEngine.Debug.Log(message);
#endif
    }
}
