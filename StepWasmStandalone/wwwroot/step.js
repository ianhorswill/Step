const BLOCKEND = { 
  scope: 'title',
  begin: '^\\[end\\]',
  end: '$'
};

const DECLARATION = {
  scope: 'keyword',
  begin: '^\\[[^A-Z"0-9]',
  end: '$'
};

const QUOTED_STRING = {
  scope: 'string',
  begin: '"',
  end: '"'
};

const SMARTY_PANTS_QUOTED_STRING = {
  scope: 'string',
  begin: '\\u201c',
  end: '\\u201d'
};

const PIPE_STRING = {
  scope: 'string',
  begin: '\\|[^ ]',
  end: '\\|'
};

const SYMBOL_STRING = {
  scope: 'string',
  begin: '[a-z][a-zA-Z0-9_]*',
};

const OPERATOR = {
  scope: 'operator',
  begin: '\\|[ \\?]',
};

const GLOBAL_VARIABLE = {
    keywords: {
        built_in: ['Call', "Succeeds", "CallDiscardingStateChanges", "IgnoreOutput", "Begin", "And", "Or",
            "Not", "Fails", "NotAny", "FindAll", "FindUnique", "FindFirstNUnique", "FindAtMostNUnique",
            "DoAll", "AccumulateOutput", "AccumulateOutputWithSeparators", "ForEach", "Implies", "Once",
            "ExactlyOnce", "Max", "Min", "SaveText", "PreviousCall", "UniqueCall", "Parse", "TreeSearch",
            "=", "Different", ">", "<", ">=", "<=", "Paragraph", "NewLine", "FreshLine", "ForceSpace",
            "Fail", "Break", "InterpreterBreak", "Log", "LogBack", "Listing", "Throw", "BailOut", "StringForm",
            "WriteVerbatim", "Write", "WriteCapitalized", "WriteConcatenated", "Member", "Length", "Nth",
            "Cons", "Var", "NonVar", "Ground", "Nonground", "CopyTerm", "String", "Tuple", "FeatureStructure",
            "BinaryTask", "Empty", "EmptyMaxQueue", "EmptyMinQueue", "CountAttempts", "RandomIntegerInclusive",
            "RandomIntegerExclusive", "RandomFloat", "RandomElement", "Gaussian", "SampleFeatures",
            "Format", "Downcase", "Downcased", "Upcased", "Capitalized", "StartsWithVowel",
            "NounSingularPlural", "EnvironmentOption", "Hashtable", "Contains", "LinearInterpolate",
            "CompoundTask", "TaskMethod", "LastMethodCallFrame", "CallerChainAncestor", "GoalChainAncestor",
            "TaskCalls", "TaskSubtask", "Method", "Help", "Apropos", "ElStore", "ElDelete", "ElDump", "Mention",
            "VisualizeGraph", "VisualizeGraphNoRender"
        ]
    },
  scope: 'variable',
  begin: '[A-Z]\\w*'
};

const LOCAL_VARIABLE = {
  scope: 'variable',
  begin: '\\?\\w*'
};

const COMMENT = hljs.HASH_COMMENT_MODE;

const GENERAL_VALUE = [
  QUOTED_STRING,
  SMARTY_PANTS_QUOTED_STRING,
  PIPE_STRING,
  SYMBOL_STRING,
  OPERATOR,
  GLOBAL_VARIABLE,
  LOCAL_VARIABLE
]

const TUPLE = {
  begin: '\\[',
  beginScope: 'variable',
  end: '\\]',
  endScope: 'variable',
  scope: 'literal',
  contains: GENERAL_VALUE
};

GENERAL_VALUE.push(TUPLE);

const FEATURE_NAME = {
  match: '[a-zA-Z0-9]+:',
  scope: 'built_in'
}

const FEATURE_STRUCTURE = {
  begin: '{',
  beginScope: 'variable',
  scope: 'literal',
  end: '\\}',
  endScope: 'variable',
  contains: [FEATURE_NAME, GENERAL_VALUE]
}

GENERAL_VALUE.push(FEATURE_STRUCTURE)

const CALL = {
  begin: ' \\[ *(?!randomly|or|end|firstOf|case|cool|once)',
  scope: 'variable',
  end: '\\]',
  endsWithParent: true,
    contains: [
    TUPLE,
    QUOTED_STRING,
    SMARTY_PANTS_QUOTED_STRING,
    PIPE_STRING,
    SYMBOL_STRING,
    OPERATOR,
    GLOBAL_VARIABLE,
      LOCAL_VARIABLE
  ]
};

const TOPLEVELCALL = {
  begin: '^\\[',
  scope: 'variable',
  end: '$',
  contains: [
    TUPLE,
    FEATURE_STRUCTURE,
    QUOTED_STRING,
    SMARTY_PANTS_QUOTED_STRING,
    PIPE_STRING,
    SYMBOL_STRING,
    OPERATOR,
    GLOBAL_VARIABLE,
    LOCAL_VARIABLE,
    CALL
  ]
};

const VARIABLE_INTERPOLATION = {
  match: "[\\?\\^][a-zA-Z0-9_/\\+]+",
  scope: "variable"
};

const INLINE_KEYWORD = {
  scope: 'keyword',
  begin: '\\[(randomly|or|end|firstOf|case|cool|once)\\]'
}
const SINGLE_LINE_BODY = {
  scope: 'string',
  begin: ':',
  beginScope: 'string',
  end: '$',
  contains: [ INLINE_KEYWORD, CALL, VARIABLE_INTERPOLATION  ]
}

const METHOD_NAME = {
  scope: 'title.function',
  match: '[A-Z][a-zA-Z0-9_]*'
}

const TASK_DECLARATION = {
  beginKeywords: 'fluent task predicate',
  beginScope: 'keyword',
  end: '$',
  contains: [ METHOD_NAME, LOCAL_VARIABLE  ]
}

const MULTILINE_BODY = {
  scope: 'string',
  begin: '^[  ]',
  beginScope: 'punctuation',
  end: '$',
  endScope: 'title.function',
  contains: [ CALL, VARIABLE_INTERPOLATION ]
}

const METHOD_DECLARATION = {
  scope: 'title.function',
  begin: '[A-Z][a-zA-Z0-9_]*',
    contains: [
    SINGLE_LINE_BODY,
    MULTILINE_BODY
  ]
}

const PRIORITIZED_METHOD = {
  begin: '\\[[0-9\\.]+\\] ',
  beginScope: 'keyword',
  end: '$',
  contains: [
    METHOD_DECLARATION
  ]
}

hljs.registerLanguage('step', function() {
    return {
      keywords: {
          meta: 'predicate task randomly generator fallable',
          keyword: 'end set now initially',
          literal: ['false','true','null'],
      },
      scope: 'meta',
      contains: [
        {
          scope: 'title.function',
          begin: '^[A-Z][a-zA-Z0-9_]*',
        },
        BLOCKEND,
        PRIORITIZED_METHOD,
        TASK_DECLARATION,
        DECLARATION,
        TOPLEVELCALL,
        FEATURE_STRUCTURE,  
        TUPLE,
        QUOTED_STRING,
        SMARTY_PANTS_QUOTED_STRING,
        PIPE_STRING,
        SYMBOL_STRING,
        OPERATOR,
        GLOBAL_VARIABLE,
        LOCAL_VARIABLE,
        COMMENT,
        SINGLE_LINE_BODY,
        MULTILINE_BODY
      ]
    }
  })