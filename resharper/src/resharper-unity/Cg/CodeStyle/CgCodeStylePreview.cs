using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.OptionPages.CodeStyle;
using JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [CodePreviewPreparatorComponent]
    public class CgCodeStylePreview : CodePreviewPreparator
    {
        public override KnownLanguage Language => CgLanguage.Instance;
        public override ProjectFileType ProjectFileType => CgProjectFileType.Instance;
    }
}