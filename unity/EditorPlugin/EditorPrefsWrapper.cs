﻿using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class EditorPrefsWrapper
  {
    public static string ExternalScriptEditor
    {
      get { return EditorPrefs.GetString("kScriptsDefaultApp"); }
      set { EditorPrefs.SetString("kScriptsDefaultApp", value); }
    }

    public static bool AutoRefresh
    {
      get { return EditorPrefs.GetBool("kAutoRefresh"); }
      set { EditorPrefs.SetBool("kAutoRefresh", value); }
    }

    public static int ScriptChangesDuringPlayOptions
    {
      // details in UnityEditor\PreferencesProvider.cs
      get { return EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0); }
    }
    
  }
}