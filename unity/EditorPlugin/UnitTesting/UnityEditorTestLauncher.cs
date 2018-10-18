using System;
using System.Linq;
using System.Reflection;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;
using JetBrains.Util.Logging;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TestResult = JetBrains.Platform.Unity.EditorPluginModel.TestResult;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public partial class UnityEditorTestLauncher
  {
    private readonly UnitTestLaunch myLaunch;
    
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    private static string RunnerAddListener = "AddListener";

    public UnityEditorTestLauncher(UnitTestLaunch launch)
    {
      myLaunch = launch;
    }

    public void TryLaunchUnitTests()
    {
      try
      {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var testEditorAssembly = assemblies
          .FirstOrDefault(assembly => assembly.GetName().Name.Equals("UnityEditor.TestRunner"));
        var testEngineAssembly = assemblies
          .FirstOrDefault(assembly => assembly.GetName().Name.Equals("UnityEngine.TestRunner"));

        if (testEditorAssembly == null || testEngineAssembly == null)
        {
          ourLogger.Verbose(
            "Could not find UnityEditor.TestRunner or UnityEngine.TestRunner assemblies in current AppDomain");
          return;
        }

        var launcherTypeString = myLaunch.TestMode == TestMode.Edit ? 
          "UnityEditor.TestTools.TestRunner.EditModeLauncher" : 
          "UnityEditor.TestTools.TestRunner.PlaymodeLauncher";
        var launcherType = testEditorAssembly.GetType(launcherTypeString);
        if (launcherType == null)
        {
          string testEditorAssemblyProperties =  testEditorAssembly.GetTypes().Select(a=>a.Name).Aggregate((a, b)=> a+ ", " + b);
          throw new NullReferenceException($"Could not find {launcherTypeString} among {testEditorAssemblyProperties}");
        }
        
        var filterType = testEngineAssembly.GetType("UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter");
        if (filterType == null)
        {
          string testEngineAssemblyProperties = testEngineAssembly.GetTypes().Select(a=>a.Name).Aggregate((a, b)=> a+ ", " + b);
          throw new NullReferenceException($"Could not find \"UnityEngine.TestTools.TestRunner.GUI.TestRunnerFilter\" among {testEngineAssemblyProperties}");
        }
        
        var filter = Activator.CreateInstance(filterType);
        var fieldInfo = filter.GetType().GetField("testNames", BindingFlags.Instance | BindingFlags.Public);
        fieldInfo = fieldInfo??filter.GetType().GetField("names", BindingFlags.Instance | BindingFlags.Public);
        if (fieldInfo == null)
        {
          ourLogger.Verbose("Could not find testNames field via reflection");
          return;
        }
        
        var testNameStrings = (object) myLaunch.TestNames.ToArray();
        fieldInfo.SetValue(filter, testNameStrings);

        if (myLaunch.TestMode == TestMode.Play)
        {
          var playmodeTestsControllerSettingsTypeString = "UnityEngine.TestTools.TestRunner.PlaymodeTestsControllerSettings";
          var playmodeTestsControllerSettingsType = testEngineAssembly.GetType(playmodeTestsControllerSettingsTypeString);

          var runnerSettings = playmodeTestsControllerSettingsType.GetMethod("CreateRunnerSettings")
            .Invoke(null, new[] {filter});
          var activeScene = SceneManager.GetActiveScene();
          var bootstrapSceneInfo = runnerSettings.GetType().GetField("bootstrapScene", BindingFlags.Instance | BindingFlags.Public);
          bootstrapSceneInfo.SetValue(runnerSettings, activeScene.path);
          var originalSceneInfo = runnerSettings.GetType().GetField("originalScene", BindingFlags.Instance | BindingFlags.Public);
          originalSceneInfo.SetValue(runnerSettings, activeScene.path);
          
          var playModeLauncher = Activator.CreateInstance(launcherType,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null, new[] {runnerSettings},
            null);

          PlayModeSupport.PlayModeLauncherRun(playModeLauncher, runnerSettings, testEditorAssembly, testEngineAssembly);
        }
        else
        {
          object launcher;
          if (UnityUtils.UnityVersion >= new Version(2018, 1))
          {
            var enumType = testEngineAssembly.GetType("UnityEngine.TestTools.TestPlatform");
            if (enumType == null)
            {
              ourLogger.Verbose("Could not find TestPlatform field via reflection");
              return;
            }

            var assemblyProviderType = testEditorAssembly.GetType("UnityEditor.TestTools.TestRunner.TestInEditorTestAssemblyProvider");
            var testPlatformVal = 2; // All = 255, // 0xFF, EditMode = 2, PlayMode = 4,
            if (assemblyProviderType != null)
            {
              var assemblyProvider = Activator.CreateInstance(assemblyProviderType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null,
                new[] {Enum.ToObject(enumType, testPlatformVal)}, null);
              ourLogger.Log(LoggingLevel.INFO, assemblyProvider.ToString());
              launcher = Activator.CreateInstance(launcherType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, new[] {filter, assemblyProvider},
                null);
            }
            else
            {
              launcher = Activator.CreateInstance(launcherType,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null, new[] {filter, Enum.ToObject(enumType, testPlatformVal)},
                null);
            }
          }
          else
          {
            launcher = Activator.CreateInstance(launcherType,
              BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
              null, new[] {filter},
              null);
          }

          var runnerField = launcherType.GetField("m_EditModeRunner", BindingFlags.Instance | BindingFlags.NonPublic);
          if (runnerField == null)
          {
            ourLogger.Verbose("Could not find runnerField via reflection");
            return;
          }

          var runner = runnerField.GetValue(launcher);
          SupportAbort(runner);

          if (!AdviseTestStarted(runner, "m_TestStartedEvent", test =>
          {
            if (!(test is TestMethod)) return;
            ourLogger.Verbose("TestStarted : {0}", test.FullName);
            var tResult = new TestResult(TestEventsSender.GetIdFromNUnitTest(test), string.Empty, 0, Status.Running,
              TestEventsSender.GetIdFromNUnitTest(test.Parent));
            TestEventsSender.TestStarted(myLaunch, tResult);
          }))
            return;

          if (!AdviseTestFinished(runner, "m_TestFinishedEvent", result =>
          {
            if (!(result.Test is TestMethod)) return;
            var res = TestEventsSender.GetTestResult(result);
            TestEventsSender.TestFinished(myLaunch, TestEventsSender.GetTestResult(res));
          }))
            return;

          if (!AdviseSessionFinished(runner, "m_RunFinishedEvent", result =>
          {
            TestEventsSender.RunFinished(myLaunch);
          }))
            return;

          var runMethod = launcherType.GetMethod("Run", BindingFlags.Instance | BindingFlags.Public);
          if (runMethod == null)
          {
            ourLogger.Verbose("Could not find runMethod via reflection");
            return;
          }

          //run!
          runMethod.Invoke(launcher, null);
        }
      }
      catch (Exception e)
      {
        ourLogger.Error(e, "Exception while launching Unity Editor tests.");
      }
    }

    private static bool AdviseSessionFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mRunFinishedEventMethodInfo= runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mRunFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find m_RunFinishedEvent via reflection");
        return false;
      }

      var mRunFinished = mRunFinishedEventMethodInfo.GetValue(runner);
      var addListenerMethod = mRunFinished.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose($"Could not find {RunnerAddListener} of mRunFinished via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mRunFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
    }

    private static bool AdviseTestStarted(object runner, string fieldName, Action<ITest> callback)
    {
      var mTestStartedEventMethodInfo = runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestStartedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find mTestStartedEventMethodInfo via reflection");
        return false;
      }

      var mTestStarted = mTestStartedEventMethodInfo.GetValue(runner);
      var addListenerMethod =
        mTestStarted.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose($"Could not find {RunnerAddListener} of mTestStarted via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mTestStarted, new object[] {new UnityAction<ITest>(callback)});
      return true;
    }

    private static bool AdviseTestFinished(object runner, string fieldName, Action<ITestResult> callback)
    {
      var mTestFinishedEventMethodInfo = runner.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

      if (mTestFinishedEventMethodInfo == null)
      {
        ourLogger.Verbose("Could not find m_TestFinishedEvent via reflection");
        return false;
      }

      var mTestFinished = mTestFinishedEventMethodInfo.GetValue(runner);
      var addListenerMethod =
        mTestFinished.GetType().GetMethod(RunnerAddListener, BindingFlags.Instance | BindingFlags.Public);

      if (addListenerMethod == null)
      {
        ourLogger.Verbose("Could not find addListenerMethod via reflection");
        return false;
      }

      //subscribe for tests callbacks
      addListenerMethod.Invoke(mTestFinished, new object[] {new UnityAction<ITestResult>(callback)});
      return true;
    }
    
    private void SupportAbort(object runner)
    {
      var unityTestAssemblyRunnerField =
        runner.GetType().GetField("m_Runner", BindingFlags.Instance | BindingFlags.NonPublic);
      if (unityTestAssemblyRunnerField != null)
      {
        var unityTestAssemblyRunner = unityTestAssemblyRunnerField.GetValue(runner);
        var stopRunMethod = unityTestAssemblyRunner.GetType()
          .GetMethod("StopRun", BindingFlags.Instance | BindingFlags.Public);
        myLaunch.Abort.Set((lifetime, _) =>
        {
          ourLogger.Verbose("Call StopRun method via reflection.");
          var task = new RdTask<bool>();
          try
          {
            stopRunMethod.Invoke(unityTestAssemblyRunner, null);
            task.Set(true);
          }
          catch (Exception)
          {
            ourLogger.Verbose("Call StopRun method failed.");
            task.Set(false);
          }
          return task;
        });
      }
    }
  }
}
