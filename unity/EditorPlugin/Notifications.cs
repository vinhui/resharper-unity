using System;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class Notifications
  {
    public static void ShowAutoSaveNotificationIfAllowed()
    {
      if (RiderScriptableSingleton.Instance.AutoSaveWarningShownOnce || !PluginSettings.ShowAutoSaveWarning) return;
      
      var notification =
       @"AutoSaving files in Rider may trigger recompilation when switching back to Unity Editor, which may wipe live data if the Unity player is running.
       Consider changing <b>Script Changes While Playing</b> setting in Unity-Preferences-";
      if (UnityUtils.UnityVersion >= new Version(2018, 2) && EditorPrefsWrapper.ScriptChangesDuringPlayOptions == 0)
      {
        notification += "General";
        Debug.LogWarning(notification);
      }
      else if (UnityUtils.UnityVersion < new Version(2018, 2) && PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.RecompileAndContinuePlaying)
      {
        notification += "Rider";
        Debug.LogWarning(notification);
      }
    }
  }
}