using System;
using JetBrains.Rider.Unity.Editor.Utils;
using JetBrains.Util;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class Notifications
  {
    public static void ShowAutoSaveNotificationIfAllowed()
    {
      if (RiderScriptableSingleton.Instance.AutoSaveWarningShownOnce) return;
      RiderScriptableSingleton.Instance.AutoSaveWarningShownOnce = true;
      
      // https://docs.unity3d.com/Manual/Preferences.html
      var notification =
        @"Auto save is enabled in Rider. This can cause unwanted recompilation and the loss of current state during play mode.
Consider changing the <i>Script Changes While Playing</i> setting in Unity Preferences {0} tab.";
      
      if (UnityUtils.UnityVersion >= new Version(2018, 2) && EditorPrefsWrapper.ScriptChangesDuringPlayOptions == 0)
      {
        Debug.LogWarning(string.Format(notification, "General"));
      }
      else if (UnityUtils.UnityVersion < new Version(2018, 2) && PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.RecompileAndContinuePlaying)
      {
        Debug.LogWarning(string.Format(notification, "Rider"));
      }
    }
  }
}