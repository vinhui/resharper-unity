using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Settings.Store;
using JetBrains.ReSharper.Psi.CSharp.Naming2;
using JetBrains.ReSharper.Psi.Naming.Settings;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ReSharper.Daemon.CSharp.Errors;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SolutionComponent]
    public class CodeStyleSettingsPatcher
    {
        public CodeStyleSettingsPatcher(Lifetime lifetime, SolutionSettings solutionSettings, UnityReferencesTracker tracker)
        {
            var lowerCaseNaming = new NamingPolicy(new NamingRule
            {
                NamingStyleKind = NamingStyleKinds.aaBb, Prefix = string.Empty, Suffix = string.Empty
            });
            
            tracker.IsUnitySolution.WhenTrue(lifetime, lt =>
            {  
                var settings = solutionSettings.BindForWritingToSolutionShared(); // .sln.DotSettings
                
                settings.SetIndexedValue((CSharpNamingSettings key) => key.PredefinedNamingRules, NamedElementKinds.PrivateInstanceFields, lowerCaseNaming);
                settings.SetIndexedValue((CSharpNamingSettings key) => key.PredefinedNamingRules, NamedElementKinds.PublicFields, lowerCaseNaming);
                
                settings.SetIndexedValue(HighlightingSettingsAccessor.InspectionSeverities, ArrangeTypeMemberModifiersWarning.HIGHLIGHTING_ID, Severity.DO_NOT_SHOW);
            });         
        }
    }
}