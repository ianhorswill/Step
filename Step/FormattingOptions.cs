﻿namespace Step
{
    /// <summary>
    /// Used by TextUtilities.Untokenize to regenerate a string from the tokens generated by the Step code.
    /// </summary>
    public class FormattingOptions
    {
        /// <summary>
        /// Default formatting options
        /// </summary>
        public static FormattingOptions Default = new FormattingOptions();
        /// <summary>
        /// If true, capitalize words after a sentence-terminating period.
        /// </summary>
        public bool Capitalize = true;
        /// <summary>
        /// If true, include two spaces after a sentence-terminating period rather than one.
        /// </summary>
        public bool FrenchSpacing = true;
        /// <summary>
        /// Text to mark the end of a line
        /// </summary>
        public string LineSeparator = "\n";
        /// <summary>
        /// Text marking the end of a paragraph
        /// </summary>
        public string ParagraphMarker = "\n";

        /// <summary>
        /// Make a new FormattingOptions object
        /// </summary>
        public FormattingOptions()
        { }
    }
}
