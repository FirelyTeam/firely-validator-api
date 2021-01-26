﻿namespace Firely.Fhir.Validation
{
    public enum IssueSeverity
    {
        Fatal,
        Error,
        Warning,
        Information
    }

    public enum IssueType
    {
        Invalid,
        Invariant,
        BusinessRule,
        Incomplete,
        Structure,
        NotSupported,
        Informational,
        Exception,
        CodeInvalid
    }

    public class Issue
    {
        public int IssueNumber { get; }
        public IssueSeverity Severity { get; }
        public IssueType Type { get; }

        public Issue(int issueNumber, IssueSeverity severity, IssueType type)
        {
            IssueNumber = issueNumber;
            Severity = severity;
            Type = type;
        }

        public static Issue Create(int code, IssueSeverity severity, IssueType type) =>
            new Issue(issueNumber: code, severity: severity, type: type);

        // Validation resource instance errors
        public static readonly Issue CONTENT_ELEMENT_MUST_HAVE_VALUE_OR_CHILDREN = Create(1000, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_HAS_UNKNOWN_CHILDREN = Create(1001, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_HAS_INCORRECT_TYPE = Create(1003, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_MUST_MATCH_TYPE = Create(1004, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_VALUE_TOO_LONG = Create(1005, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_INVALID_PRIMITIVE_VALUE = Create(1006, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_DOES_NOT_MATCH_FIXED_VALUE = Create(1008, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_DOES_NOT_MATCH_PATTERN_VALUE = Create(1009, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_CANNOT_DETERMINE_TYPE = Create(1010, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_CHOICE_INVALID_INSTANCE_TYPE = Create(1011, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_FAILS_ERROR_CONSTRAINT = Create(1012, IssueSeverity.Error, IssueType.Invariant);
        public static readonly Issue CONTENT_ELEMENT_FAILS_WARNING_CONSTRAINT = Create(1013, IssueSeverity.Warning, IssueType.Invariant);
        public static readonly Issue CONTENT_REFERENCE_OF_INVALID_KIND = Create(1015, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_CONTAINED_REFERENCE_NOT_RESOLVABLE = Create(1016, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_UNPARSEABLE_REFERENCE = Create(1017, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_REFERENCE_NOT_RESOLVABLE = Create(1018, IssueSeverity.Warning, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_SMALL = Create(1019, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_PRIMITIVE_VALUE_TOO_LARGE = Create(1020, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_PRIMITIVE_VALUE_NOT_COMPARABLE = Create(1021, IssueSeverity.Warning, IssueType.Invalid);
        public static readonly Issue CONTENT_MISMATCHING_PROFILES = Create(1022, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_INVALID_FOR_REQUIRED_BINDING = Create(1023, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_INVALID_FOR_NON_REQUIRED_BINDING = Create(1024, IssueSeverity.Warning, IssueType.Invalid);
        public static readonly Issue CONTENT_TYPE_NOT_BINDEABLE = Create(1025, IssueSeverity.Information, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_FAILS_SLICING_RULE = Create(1026, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_SLICING_OUT_OF_ORDER = Create(1027, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_INCORRECT_OCCURRENCE = Create(1028, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue CONTENT_ELEMENT_NAME_DOESNT_MATCH_DEFINITION = Create(1029, IssueSeverity.Error, IssueType.Invalid);

        public static readonly Issue XSD_VALIDATION_ERROR = Create(1100, IssueSeverity.Error, IssueType.Invalid);
        public static readonly Issue XSD_VALIDATION_WARNING = Create(1101, IssueSeverity.Warning, IssueType.Invalid);
        public static readonly Issue XSD_CONTENT_POCO_PARSING_FAILED = Create(1102, IssueSeverity.Error, IssueType.Invalid);

        // Profile problems
        public static readonly Issue PROFILE_ELEMENTDEF_MIN_MAX_USES_UNORDERED_TYPE = Create(2000, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_MAX_USES_UNORDERED_TYPE = Create(2001, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_MAXLENGTH_NEGATIVE = Create(2002, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_CONTAINS_NULL_TYPE = Create(2003, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_CONTAINS_NO_TYPE_OR_NAMEREF = Create(2004, IssueSeverity.Warning, IssueType.BusinessRule);
        //        public static readonly Issue PROFILE_ELEMENTDEF_NO_PRIMITIVE_REGEX = def(2005, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_INVALID_NAMEREFERENCE = Create(2006, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_CARDINALITY_MISSING = Create(2007, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_IS_EMPTY = Create(2008, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_ELEMENTDEF_INVALID_FHIRPATH_EXPRESSION = Create(2009, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_NO_PROFILE_TO_VALIDATE_AGAINST = Create(2010, IssueSeverity.Warning, IssueType.Incomplete);
        public static readonly Issue PROFILE_ELEMENTDEF_INCORRECT = Create(2012, IssueSeverity.Warning, IssueType.BusinessRule);
        public static readonly Issue PROFILE_INSTANCE_MATCHES_MULTIPLE_SLICES = Create(2013, IssueSeverity.Warning, IssueType.Structure);

        // Unsupported 
        public static readonly Issue UNSUPPORTED_SLICING_NOT_SUPPORTED = Create(3000, IssueSeverity.Warning, IssueType.NotSupported);
        public static readonly Issue UNSUPPORTED_CONSTRAINT_WITHOUT_FHIRPATH = Create(3003, IssueSeverity.Warning, IssueType.Incomplete);
        public static readonly Issue UNSUPPORTED_MIN_MAX_QUANTITY = Create(3004, IssueSeverity.Warning, IssueType.NotSupported);
        public static readonly Issue UNSUPPORTED_BINDING_NOT_SUPPORTED_BY_SERVICE = Create(3006, IssueSeverity.Warning, IssueType.NotSupported);

        // Non-availability, incomplete data
        public static readonly Issue UNAVAILABLE_REFERENCED_PROFILE = Create(4000, IssueSeverity.Error, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_NEED_SNAPSHOT = Create(4002, IssueSeverity.Error, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_SNAPSHOT_GENERATION_FAILED = Create(4003, IssueSeverity.Error, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_NEED_DIFFERENTIAL = Create(4004, IssueSeverity.Error, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_REFERENCED_RESOURCE = Create(4005, IssueSeverity.Warning, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_TERMINOLOGY_SERVER = Create(4007, IssueSeverity.Error, IssueType.Incomplete);
        public static readonly Issue UNAVAILABLE_VALIDATE_CODE_FAILED = Create(4008, IssueSeverity.Error, IssueType.Incomplete);

        // Processing information
        public static readonly Issue PROCESSING_PROGRESS = Create(5000, IssueSeverity.Information, IssueType.Informational);
        // public static readonly Issue PROCESSING_CONSTRAINT_VALIDATION_INACTIVEX = Create(5001, IssueSeverity.Information, IssueType.Informational);
        public static readonly Issue PROCESSING_START_NESTED_VALIDATION = Create(5002, IssueSeverity.Information, IssueType.Informational);
        public static readonly Issue PROCESSING_CATASTROPHIC_FAILURE = Create(5003, IssueSeverity.Fatal, IssueType.Exception);

        // Terminology specific errors
        public static readonly Issue TERMINOLOGY_CODE_NOT_IN_VALUESET = Create(6001, IssueSeverity.Error, IssueType.CodeInvalid);
        public static readonly Issue TERMINOLOGY_ABSTRACT_CODE_NOT_ALLOWED = Create(6002, IssueSeverity.Error, IssueType.CodeInvalid);
        public static readonly Issue TERMINOLOGY_INCORRECT_DISPLAY = Create(6003, IssueSeverity.Warning, IssueType.CodeInvalid);
        public static readonly Issue TERMINOLOGY_SERVICE_FAILED = Create(6004, IssueSeverity.Warning, IssueType.NotSupported);
        public static readonly Issue TERMINOLOGY_NO_CODE_IN_INSTANCE = Create(6005, IssueSeverity.Error, IssueType.CodeInvalid);

        public static readonly Issue TODO = Create(-1, IssueSeverity.Error, IssueType.Invalid);

    }
}
