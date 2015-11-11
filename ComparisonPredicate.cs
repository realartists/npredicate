using System;

namespace Predicate
{
    [Flags]
    public enum ComparisonPredicateOptions {
        CaseInsensitivePredicateOption = 0x01,
        DiacriticInsensitivePredicateOption = 0x02,
        NormalizedPredicateOption = 0x04,
    }

    public enum ComparisonPredicateModifier {
        DirectPredicateModifier = 0, // Do a direct comparison
        AllPredicateModifier, // ALL toMany.x = y
        AnyPredicateModifier // ANY toMany.x = y
    }

    public enum PredicateOperatorType {
        NSLessThanPredicateOperatorType = 0,
        NSLessThanOrEqualToPredicateOperatorType,
        NSGreaterThanPredicateOperatorType,
        NSGreaterThanOrEqualToPredicateOperatorType,
        NSEqualToPredicateOperatorType,
        NSNotEqualToPredicateOperatorType,
        NSMatchesPredicateOperatorType,
        NSLikePredicateOperatorType,
        NSBeginsWithPredicateOperatorType,
        NSEndsWithPredicateOperatorType,
        NSInPredicateOperatorType, // rhs contains lhs returns true
        NSCustomSelectorPredicateOperatorType,
        NSContainsPredicateOperatorType = 99, // lhs contains rhs returns true
        NSBetweenPredicateOperatorType
    }

    public class ComparisonPredicate
    {
        
    }
}

