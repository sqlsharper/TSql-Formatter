namespace CnSharp.Sql
{
    public class FormattingOptions
    {
        /// <summary>
        /// Gets or sets the number of spaces used for indentation
        /// </summary>
        public int IndentSize { get; set; } = 4;
        
        public bool IndentUseTab { get;set; } = true;
        /// <summary>
        /// Gets or sets whether SQL keywords should be converted to uppercase, null means no conversion
        /// </summary>
        public bool? KeywordsUppercase { get; set; } = true;

        /// <summary>
        /// Gets or sets whether comma-separated lists should be expanded to multiple lines
        /// </summary>
        public bool ExpandCommaLists { get; set; } = false;

        /// <summary>
        /// Gets or sets whether CASE statements should be expanded to multiple lines
        /// </summary>
        public bool ExpandCaseStatements { get; set; } = true;

        /// <summary>
        /// Gets or sets whether BETWEEN/AND statements should be expanded to multiple lines
        /// </summary>
        public bool ExpandBetweenAndStatements { get; set; } = false;

        /// <summary>
        /// Gets or sets whether IN lists should be expanded to multiple lines
        /// </summary>
        public bool ExpandInLists { get; set; } = true;
    }
}