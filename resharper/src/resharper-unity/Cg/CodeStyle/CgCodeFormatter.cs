using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing.TokenNodeTypes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util.Text;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.CodeStyle
{
    [Language(typeof(CgLanguage))]
    public class CgCodeFormatter : CodeFormatterBase<CgCodeFormattingSettingsKey>
    {
        private readonly CgFormattingInfoProvider myFormattingProvider;

        public CgCodeFormatter(
            PsiLanguageType language,
            CodeFormatterRequirements requirements,
            CgFormattingInfoProvider formattingProvider)
            : base(requirements)
        {
            myFormattingProvider = formattingProvider;
            LanguageType = language;
        }

        public override PsiLanguageType LanguageType { get; }

        public override bool SupportsWrapping => true;

        protected override CodeFormattingContext CreateFormatterContext(CodeFormatProfile profile, ITreeNode firstNode,
            ITreeNode lastNode,
            AdditionalFormatterParameters parameters, ICustomFormatterInfoProvider provider)
        {
            return new CodeFormattingContext(this, firstNode, lastNode, profile,
                FormatterLoggerProvider.FormatterLogger, parameters);
        }

        public override ITokenNode GetMinimalSeparator(ITokenNode leftToken, ITokenNode rightToken)
        {
            return null;
        }

        public override ITreeNode CreateSpace(string indent, ITreeNode replacedSpace)
        {
            return TreeElementFactory.CreateLeafElement(CgTokenNodeTypes.WHITESPACE, FormatterImplHelper.GetPooledWhitespace(indent), 0, indent.Length);
        }

        #if RIDER
        
        public override ITreeNode CreateNewLine(LineEnding lineEnding, NodeType lineBreakType = null)
        {
            var buf = lineEnding.GetPresentationAsBuffer();
            return TreeElementFactory.CreateLeafElement(CgTokenNodeTypes.NEW_LINE, buf, 0, buf.Length);
        }

        #else

        public override ITreeNode CreateNewLine(LineEnding lineEnding)
        {
            var buf = lineEnding.GetPresentationAsBuffer();
            return TreeElementFactory.CreateLeafElement(CgTokenNodeTypes.NEW_LINE, buf, 0, buf.Length);
        }
    
        #endif

        public override ITreeRange Format(ITreeNode firstElement, ITreeNode lastElement, CodeFormatProfile profile,
            AdditionalFormatterParameters parameters = null)
        {
            parameters = parameters ?? AdditionalFormatterParameters.Empty;
            var pointer = FormatterImplHelper.CreateRangePointer(firstElement, lastElement);
            
            DoFormat(
                firstElement,
                lastElement,
                profile,
                parameters);
            
            
            return FormatterImplHelper.PointerToRange(pointer, firstElement, lastElement);
        }

        private void DoFormat(ITreeNode firstElement, ITreeNode lastElement, CodeFormatProfile profile, AdditionalFormatterParameters parameters)
        {
            var firstNode = FormatterImplHelper.AdjustFirstNode(this, firstElement, lastElement);
            if (firstNode == null) return;

            // ReSharper disable ArgumentsStyleLiteral
            // ReSharper disable ArgumentsStyleOther
            // ReSharper disable ArgumentsStyleNamedExpression
            var formatterSettings = GetFormattingSettings(firstNode, parameters);
            DoDeclarativeFormat(
                formatterSettings,
                myFormattingProvider,                
                customProvider: null,
                formatTasks: new[] {new FormatTask(firstNode, lastElement, profile)},
                parameters: parameters,
                shouldDoIntAlign: null,                
                beforeFormat: null,
                afterFormat: null,
                doAdditionalFormat: false);
            
            // ReSharper restore ArgumentsStyleLiteral
            // ReSharper restore ArgumentsStyleOther
            // ReSharper restore ArgumentsStyleNamedExpression
        }

        public override void FormatInsertedNodes(ITreeNode nodeFirst, ITreeNode nodeLast, bool formatSurround)
        {
            FormatterImplHelper.FormatInsertedNodesHelper(this, nodeFirst, nodeLast, formatSurround);
        }

        public override ITreeRange FormatInsertedRange(ITreeNode nodeFirst, ITreeNode nodeLast, ITreeRange origin)
        {
            return FormatterImplHelper.FormatInsertedRangeHelper(this, nodeFirst, nodeLast, origin, true);
        }

        public override void FormatReplacedNode(ITreeNode oldNode, ITreeNode newNode)
        {
            FormatInsertedNodes(newNode, newNode, false);

            FormatterImplHelper.CheckForMinimumSeparator(this, newNode);
        }

        public override void FormatReplacedRange(ITreeNode first, ITreeNode last, ITreeRange oldNodes)
        {
            FormatInsertedNodes(first, last, false);

            FormatterImplHelper.CheckForMinimumSeparator(this, first, last);
        }

        public override void FormatDeletedNodes(ITreeNode parent, ITreeNode prevNode, ITreeNode nextNode)
        {
            FormatterImplHelper.FormatDeletedNodesHelper(this, parent, prevNode, nextNode, true);
        }
    }
}