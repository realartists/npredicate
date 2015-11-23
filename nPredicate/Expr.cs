namespace RealArtists.NPredicate {
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Linq;
  using System.Linq.Expressions;
  using System.Reflection;

  public enum ExpressionType {
    ConstantValueExpressionType = 0, // Expression that always returns the same value
    EvaluatedObjectExpressionType, // Expression that always returns the parameter object itself
    VariableExpressionType, // Expression that always returns whatever is stored at 'variable' in the bindings dictionary
    KeyPathExpressionType, // Expression that returns something that can be used as a key path
    FunctionExpressionType, // Expression that returns the result of evaluating a symbol
    UnionSetExpressionType, // Expression that returns the result of doing a unionSet: on two expressions that evaluate to flat collections (arrays or sets)
    IntersectSetExpressionType, // Expression that returns the result of doing an intersectSet: on two expressions that evaluate to flat collections (arrays or sets)
    MinusSetExpressionType, // Expression that returns the result of doing a minusSet: on two expressions that evaluate to flat collections (arrays or sets)
    SymbolicValueExpressionType = 11,
    VariableAssignmentExpressionType = 12,
    SubqueryExpressionType = 13,
    AggregateExpressionType = 14,
  }

  public abstract class Expr {
    public ExpressionType ExpressionType { get; protected set; }

    public dynamic ConstantValue { get; set; }

    public string KeyPath { get; set; }

    public string Function { get; set; }

    public string Variable { get; set; }

    public Expr Operand { get; set; }

    public IEnumerable<Expr> Arguments { get; set; }

    public Expr Collection { get; set; }

    public Predicate Predicate { get; set; }

    public Expr LeftExpression { get; set; }

    public Expr RightExpression { get; set; }

    public static Expr Parse(string format, params dynamic[] arguments) {
      return new PredicateParser(format, arguments).ParseExpr();
    }

    public static Expr MakeConstant(dynamic obj) {
      return new ConstantExpr(obj);
    }

    public static Expr MakeEvaluatedObject() {
      return new EvaluatedObjectExpr();
    }

    public static Expr MakeVariable(string variable) {
      return new VariableExpr(variable);
    }

    public static Expr MakeKeyPath(string keyPath) {
      return new KeyPathExpr(keyPath);
    }

    public static Expr MakeKeyPath(Expr operand, string keyPath) {
      return new KeyPathExpr(operand, keyPath);
    }

    public static Expr MakeFunction(string name, IEnumerable<Expr> arguments) {
      return new FunctionExpr(name, arguments);
    }

    public static Expr MakeFunction(string name, params Expr[] arguments) {
      return new FunctionExpr(name, arguments);
    }

    public static Expr MakeUnionSet(Expr left, Expr right) {
      return new SetExpr();
    }

    public static Expr MakeIntersectSet(Expr left, Expr right) {
      return new SetExpr();
    }

    public static Expr MakeMinusSet(Expr left, Expr right) {
      return new SetExpr();
    }

    public static Expr MakeSubquery(Expr expr, string variable, Predicate predicate) {
      return new SubqueryExpr(expr, variable, predicate);
    }

    public static Expr MakeAggregate(IEnumerable<Expr> components) {
      return new AggregateExpr(components);
    }

    public static Expr MakeSymbolic(SymbolicValueType symbolType) {
      return new SymbolicValueExpr(symbolType);
    }

    public static Expr MakeAssignment(string variable, Expr rhs) {
      return new VariableAssignmentExpr(variable, rhs);
    }

    public Expression<Func<T, V>> LinqExpression<T, V>(LinqDialect dialect = LinqDialect.Objects) {
      var bindings = new Dictionary<string, Expression>();
      ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
      bindings.Add(self.Name, self);
      return Expression.Lambda<Func<T, V>>(LinqExpression(bindings, dialect), self);
    }

    public V ValueWithObject<T, V>(T obj) {
      var expr = LinqExpression<T, V>();
      Func<T, V> func = expr.Compile();
      return func(obj);
    }

    public V Value<V>() {
      return ValueWithObject<dynamic, V>(null);
    }

    public abstract Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect);

    public abstract string Format { get; }

    public override string ToString() {
      return Format;
    }

    public virtual void Visit(IVisitor visitor) {
      visitor.Visit(this);
      Operand?.Visit(visitor);
      if (Arguments != null) {
        foreach (Expr arg in Arguments) {
          arg.Visit(visitor);
        }
      }
      Collection?.Visit(visitor);
      Predicate?.Visit(visitor);
      LeftExpression?.Visit(visitor);
      RightExpression?.Visit(visitor);
    }

    protected Expr() { }

  }

  class ConstantExpr : Expr {
    public ConstantExpr(dynamic value) {
      ExpressionType = ExpressionType.ConstantValueExpressionType;
      ConstantValue = value;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      return Expression.Constant(ConstantValue);
    }

    public override string Format {
      get {
        if (ConstantValue is string) {
          return "'" + ConstantValue.ToString() + "'";
        } else {
          return ConstantValue?.ToString() ?? "null";
        }
      }
    }
  }

  class EvaluatedObjectExpr : Expr {
    public EvaluatedObjectExpr() {
      ExpressionType = ExpressionType.EvaluatedObjectExpressionType;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      return bindings["SELF"];
    }

    public override string Format {
      get {
        return "SELF";
      }
    }
  }

  class VariableExpr : Expr {
    public VariableExpr(string variable) {
      this.ExpressionType = ExpressionType.VariableExpressionType;
      this.Variable = variable;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      return bindings[Variable];
    }

    public override string Format {
      get {
        return Variable;
      }
    }
  }

  class KeyPathExpr : Expr {

    public KeyPathExpr(string keyPath) {
      this.ExpressionType = ExpressionType.KeyPathExpressionType;
      this.KeyPath = keyPath;
    }

    public KeyPathExpr(Expr operand, string keyPath) {
      this.ExpressionType = ExpressionType.KeyPathExpressionType;
      this.Operand = operand;
      this.KeyPath = keyPath;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      string[] keys = KeyPath.Split('.');
      Expression result = Operand != null ? Operand.LinqExpression(bindings, dialect) : bindings["SELF"];
      foreach (string key in keys) {
        if (key.ToLower() == "self") {
          continue;
        }

        Expression propertyExpr = null;

        // Keypath evaluations in Cocoa are always safe.
        // That is, if any point along the list is nil,
        // The whole thing collapses down to nil.

        // This is the equivalent in C# of writing
        // a?.b?.c?.d

        if (key == "@count") // Special case @count, which is actually a method, not a property (Length is a property, but it isn't available on IEnumerable, which is what we probably have)
        {
          Type itemType;
          if (result.Type.IsSubclassOf(typeof(Array))) {
            itemType = Expression.ArrayIndex(result, Expression.Constant(0)).Type;
          } else {
            itemType = result.Type.GetGenericArguments()[0];
          }

          Type arg0OpenType = typeof(IEnumerable<>);

          var countOpenMethod = Utils.GetGenericMethod(typeof(System.Linq.Enumerable), "Count", new Type[] { arg0OpenType });
          var countMethod = countOpenMethod.MakeGenericMethod(new Type[] { itemType });

          propertyExpr = Expression.Call(null, countMethod, result);
        } else if (Utils.TypeIsEnumerable(result.Type)) {
          ParameterExpression t = Expression.Parameter(Utils.ElementType(result.Type));
          Expression eachPropertyExpr = Expression.Property(t, key);
          Expression map = Expression.Lambda(eachPropertyExpr, new ParameterExpression[] { t });
          var selectExpr = Utils.CallAggregate("Select", result, map);
          propertyExpr = selectExpr;
        } else {
          propertyExpr = Expression.Property(result, key);
        }

        if (dialect == LinqDialect.Objects) {
          var defaultSource = Expression.Default(result.Type);
          var defaultResult = Expression.Default(propertyExpr.Type);

          var isNilExpr = Expression.Equal(result, defaultSource);
          var conditionExpr = Expression.Condition(isNilExpr, defaultResult, propertyExpr);
          result = conditionExpr;
        } else {
          result = propertyExpr;
        }
      }

      return result;
    }

    public override string Format {
      get {
        if (Operand != null) {
          return Operand.Format + "." + KeyPath;
        } else {
          return KeyPath;
        }
      }
    }
  }

  class FunctionExpr : Expr {
    public FunctionExpr(string function, IEnumerable<Expr> arguments) {
      this.ExpressionType = ExpressionType.FunctionExpressionType;
      this.Function = function;
      this.Arguments = arguments;
    }

    // Expression that invokes one of the predefined functions. Will throw immediately if the selector is bad; will throw at runtime if the parameters are incorrect.
    // Predefined functions are:
    // name              parameter array contents               returns
    //-------------------------------------------------------------------------------------------------------------------------------------
    // sum:              NSExpression instances representing numbers        NSNumber 
    // count:            NSExpression instances representing numbers        NSNumber 
    // min:              NSExpression instances representing numbers        NSNumber  
    // max:              NSExpression instances representing numbers        NSNumber
    // average:          NSExpression instances representing numbers        NSNumber
    // median:           NSExpression instances representing numbers        NSNumber
    // mode:             NSExpression instances representing numbers        NSArray     (returned array will contain all occurrences of the mode)
    // stddev:           NSExpression instances representing numbers        NSNumber
    // add:to:           NSExpression instances representing numbers        NSNumber
    // from:subtract:    two NSExpression instances representing numbers    NSNumber
    // multiply:by:      two NSExpression instances representing numbers    NSNumber
    // divide:by:        two NSExpression instances representing numbers    NSNumber
    // modulus:by:       two NSExpression instances representing numbers    NSNumber
    // sqrt:             one NSExpression instance representing numbers     NSNumber
    // log:              one NSExpression instance representing a number    NSNumber
    // ln:               one NSExpression instance representing a number    NSNumber
    // raise:toPower:    one NSExpression instance representing a number    NSNumber
    // exp:              one NSExpression instance representing a number    NSNumber
    // floor:            one NSExpression instance representing a number    NSNumber
    // ceiling:          one NSExpression instance representing a number    NSNumber
    // abs:              one NSExpression instance representing a number    NSNumber
    // trunc:            one NSExpression instance representing a number    NSNumber
    // uppercase:    one NSExpression instance representing a string    NSString
    // lowercase:    one NSExpression instance representing a string    NSString
    // random            none                           NSNumber (integer) 
    // randomn:          one NSExpression instance representing a number    NSNumber (integer) such that 0 <= rand < param
    // now               none                           [NSDate now]
    // bitwiseAnd:with:  two NSExpression instances representing numbers    NSNumber    (numbers will be treated as NSInteger)
    // bitwiseOr:with:   two NSExpression instances representing numbers    NSNumber    (numbers will be treated as NSInteger)
    // bitwiseXor:with:  two NSExpression instances representing numbers    NSNumber    (numbers will be treated as NSInteger)
    // leftshift:by:     two NSExpression instances representing numbers    NSNumber    (numbers will be treated as NSInteger)
    // rightshift:by:    two NSExpression instances representing numbers    NSNumber    (numbers will be treated as NSInteger)
    // onesComplement:   one NSExpression instance representing a numbers   NSNumber    (numbers will be treated as NSInteger)
    // noindex:      an NSExpression                    parameter   (used by CoreData to indicate that an index should be dropped)
    // distanceToLocation:fromLocation:
    //                   two NSExpression instances representing CLLocations    NSNumber
    // length:           an NSExpression instance representing a string         NSNumber


    // --------- --------- --------- --------- --------- --------- ---------
    // Additionally, Ship defines the following functions for date math
    // --------- --------- --------- --------- --------- --------- ---------
    //
    // - (NSDate*)dateByAddingSeconds:(NSNumber*)seconds;
    // - (NSDate*)dateByAddingMinutes:(NSNumber*)minutes;
    // - (NSDate*)dateByAddingHours:(NSNumber*)hours;
    // - (NSDate*)dateByAddingDays:(NSNumber*)days;
    // - (NSDate*)dateByAddingMonths:(NSNumber*)months;
    // - (NSDate*)dateByAddingYears:(NSNumber*)years;
    //

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      var argumentExpressions = Arguments.Select(a => a.LinqExpression(bindings, dialect));
      var arg0 = argumentExpressions.FirstOrDefault();
      var arg1 = argumentExpressions.ElementAtOrDefault(1);

      switch (Function.ToLower()) {
        case "sum:":
          return Utils.CallAggregate("Sum", arg0);
        case "count:":
          return Utils.CallAggregate("Count", arg0);
        case "min:":
          return Utils.CallAggregate("Min", arg0);
        case "max:":
          return Utils.CallAggregate("Max", arg0);
        case "average:":
          return Utils.CallAggregate("Average", arg0);
        case "median:":
          break; // TODO
        case "mode:":
          break; // TODO
        case "stddev:":
          break; // TODO
        case "add:to:":
          return Expression.Add(arg0, arg1);
        case "from:subtract:":
          return Expression.Subtract(arg0, arg1);
        case "multiply:by:":
          return Expression.Multiply(arg0, arg1);
        case "divide:by:":
          return Expression.Divide(arg0, arg1);
        case "modulus:by:":
          return Expression.Modulo(arg0, arg1);
        case "sqrt:":
          return Utils.CallMath("Sqrt", arg0);
        case "log:":
          return Utils.CallMath("Log10", arg0);
        case "ln:":
          return Utils.CallMath("Log", arg0);
        case "raise:topower:":
          return Expression.Power(arg0, arg1);
        case "exp:":
          return Expression.Power(Expression.Constant(Math.E), arg0);
        case "floor:":
          return Utils.CallMath("Floor", arg0);
        case "ceiling":
          return Utils.CallMath("Ceiling", arg0);
        case "abs:":
          return Utils.CallMath("Abs", arg0);
        case "trunc:":
          return Utils.CallMath("Truncate", arg0);
        case "negate:":
          return Expression.Negate(arg0);
        case "uppercase:":
          return Utils.CallSafe(dialect, arg0, "ToUpper");
        case "lowercase:":
          return Utils.CallSafe(dialect, arg0, "ToLower");
        case "random":
          {
            var randExpr = Expression.Constant(Rand);
            var randInt = typeof(Random).GetMethod("Next", new Type[] { });
            return Expression.Call(randExpr, randInt);
          }
        case "randomn:":
          {
            var randExpr = Expression.Constant(Rand);
            var randN = typeof(Random).GetMethod("Next", new Type[] { typeof(int) });
            return Expression.Call(randExpr, randN, arg0);
          }
        case "now":
          {
            var utcNow = typeof(DateTime).GetProperty("UtcNow", BindingFlags.Static | BindingFlags.Public);
            return Expression.Property(null, utcNow);
          }
        case "bitwiseand:with:":
          return Expression.And(arg0, arg1);
        case "bitwiseor:with:":
          return Expression.Or(arg0, arg1);
        case "bitwisexor:with:":
          return Expression.ExclusiveOr(arg0, arg1);
        case "leftshift:by:":
          return Expression.LeftShift(arg0, arg1);
        case "rightshift:by:":
          return Expression.RightShift(arg0, arg1);
        case "onescomplement:":
          return Expression.OnesComplement(arg0);
        case "length:":
          return Utils.CallSafe(dialect, arg0, "Length");
        case "castobject:totype:":
          return Cast(arg0, arg1, dialect);
        case "objectfrom:withindex:":
          return GetObjectAtIndex(arg0, arg1, bindings);
        case "addseconds:":
        case "datebyaddingseconds:":
          return DateFunction("AddSeconds", arg0, arg1, dialect);
        case "addminutes:":
        case "datebyaddingminutes:":
          return DateFunction("AddMinutes", arg0, arg1, dialect);
        case "addhours:":
        case "datebyaddinghours:":
          return DateFunction("AddHours", arg0, arg1, dialect);
        case "adddays:":
        case "datebyaddingdays:":
          return DateFunction("AddDays", arg0, arg1, dialect);
        case "addmonths:":
        case "datebyaddingmonths:":
          return DateFunction("AddMonths", arg0, arg1, dialect);
        case "addyears:":
        case "datebyaddingyears:":
          return DateFunction("AddYears", arg0, arg1, dialect);
      }
      throw new NotImplementedException($"{Function} not implemented");
    }

    static Random Rand = new Random();

    private Expression DateFunction(string name, Expression arg0, Expression arg1, LinqDialect dialect) {
      if (dialect == LinqDialect.Objects) {
        return Expression.Call(arg0, name, null, Utils.AsDouble(arg1));
      } else {
        var method = typeof(DbFunctions).GetMethod(name, new Type[] { typeof(DateTime?), typeof(int?) });
        return Utils.AsNotNullable(Expression.Call(method, Utils.AsNullable(arg0), Utils.AsNullable(Utils.AsInt(arg1))));
      }
    }

    private Expression Cast(Expression arg0, Expression arg1, LinqDialect dialect) {
      var destType = (Arguments.ElementAt(1) as ConstantExpr)?.ConstantValue as string;

      if (destType != "NSNumber" && destType != "NSDate" && destType != "NSString") {
        throw new NotImplementedException($"Cannot cast to any type except for NSDate, NSString or NSNumber, but got {Arguments.ElementAt(1).Format}");
      }

      if (dialect == LinqDialect.EntityFramework) {
        return CastEntities(arg0, destType);
      } else {
        return CastObjects(arg0, destType);
      }
    }

    private Expression CastObjects(Expression arg0, string type) {
      var argType = arg0.Type;
      if (type == "NSNumber") {
        if (argType == typeof(string)) {
          var parseMethod = typeof(double).GetMethod("Parse", new Type[] { typeof(string) });
          return Expression.Call(parseMethod, arg0);
        } else if (Utils.IsTypeNumeric(argType)) {
          return arg0;
        } else if (argType == typeof(bool)) {
          return Expression.Condition(arg0, Expression.Constant(1), Expression.Constant(0));
        } else if (argType == typeof(DateTime)) {
          var castMethod = typeof(Utils).GetMethod("TimeIntervalSinceReferenceDate", new Type[] { typeof(DateTime) });
          return Expression.Call(castMethod, arg0);
        } else {
          throw new NotSupportedException($"Cannot cast type {argType} to NSNumber");
        }
      } else if (type == "NSString") {
        return Expression.Call(arg0, "ToString", null, null);
      } else if (type == "NSDate") {
        if (argType == typeof(string)) {
          var parseMethod = typeof(DateTime).GetMethod("Parse", new Type[] { typeof(string) });
          return Expression.Call(parseMethod, arg0);
        } else if (argType == typeof(double)) {
          var castMethod = typeof(Utils).GetMethod("DateTimeFromTimeIntervalSinceReferenceDate", new Type[] { typeof(double) });
          return Expression.Call(castMethod, arg0);
        } else if (argType == typeof(DateTime)) {
          return arg0;
        } else {
          throw new NotSupportedException($"Cannot cast Type {argType} to NSDate");
        }
      }

      return null;
    }

    private Expression CastEntities(Expression arg0, string type) {
      throw new NotSupportedException($"Cannot emit linq for {Format}. Casting is not supported in Linq to Entities.");
    }

    private Expression GetObjectAtIndex(Expression lhs, Expression rhs, Dictionary<string, Expression> bindings) {
      Expr indexExpr = Arguments.ElementAt(1);
      if (indexExpr is SymbolicValueExpr) {
        switch ((indexExpr as SymbolicValueExpr).SymbolType) {
          case SymbolicValueType.FIRST:
            rhs = Expression.Constant(0);
            break;
          case SymbolicValueType.LAST:
            rhs = Expression.Subtract(Utils.CallAggregate("Count", lhs), Expression.Constant(1));
            break;
          case SymbolicValueType.SIZE:
            return Utils.CallAggregate("Count", lhs);
        }
      }

      return Utils.CallAggregate("ElementAtOrDefault", lhs, rhs);
    }

    public override string Format {
      get {
        int colons = 0;
        foreach (char c in Function.ToCharArray()) {
          if (c == ':')
            colons++;
        }

        if (Arguments.Count() == 0) {
          return $"{Function}()";
        } else if (Arguments.Count() == 2 && colons == 1) {
          var arg0 = Arguments.ElementAt(0);
          var arg1 = Arguments.ElementAt(1);
          return $"FUNCTION({arg0.Format}, '{Function}', {arg1.Format})";
        } else {
          return $"FUNCTION('{Function}', {String.Join(", ", Arguments.Select(a => a.Format))})";
        }
      }
    }
  }


  class SetExpr : Expr {
    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      throw new NotImplementedException();
    }

    public override string Format {
      get {
        throw new NotImplementedException();
      }
    }
  }

  class SubqueryExpr : Expr {
    public SubqueryExpr(Expr collection, string variableName, Predicate predicate) {
      this.ExpressionType = ExpressionType.SubqueryExpressionType;
      this.Collection = collection;
      this.Variable = variableName;
      this.Predicate = predicate;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      // Generate code for
      // collection.Where(variable => predicate)

      var collectionExpression = Collection.LinqExpression(bindings, dialect);
      Type itemType = null;
      if (collectionExpression.Type.IsSubclassOf(typeof(Array))) {
        itemType = Expression.ArrayIndex(collectionExpression, Expression.Constant(0)).Type;
      } else {
        itemType = collectionExpression.Type.GetGenericArguments()[0];
      }

      ParameterExpression varExpr = Expression.Parameter(itemType, Variable);
      var subBindings = new Dictionary<string, Expression>(bindings);
      subBindings[Variable] = varExpr;
      var p = Predicate.LinqExpression(subBindings, dialect);

      Type arg0OpenType = typeof(IEnumerable<>);
      //Type arg0Type = arg0OpenType.MakeGenericType(new Type[] { itemType });

      Type arg1OpenType = typeof(Func<,>);
      Type arg1Type = arg1OpenType.MakeGenericType(new Type[] { itemType, typeof(Boolean) });

      var whereOpenMethod = Utils.GetGenericMethod(typeof(System.Linq.Enumerable), "Where", new Type[] { arg0OpenType, arg1OpenType });
      var whereMethod = whereOpenMethod.MakeGenericMethod(new Type[] { itemType });

      var lambda = Expression.Lambda(arg1Type, p, new ParameterExpression[] { varExpr });
      var whereExpression = Expression.Call(null, whereMethod, collectionExpression, lambda);

      return whereExpression;
    }

    public override string Format {
      get {
        return $"SUBQUERY({Collection.Format}, {Variable}, {Predicate})";
      }
    }
  }

  class AggregateExpr : Expr {
    public AggregateExpr(IEnumerable<Expr> components) {
      this.ExpressionType = ExpressionType.AggregateExpressionType;
      this.Arguments = components;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      var exprs = new List<Expression>();
      Type type = typeof(object);
      foreach (var e in Arguments) {
        var l = e.LinqExpression(bindings, dialect);
        type = l.Type;
        exprs.Add(l);
      }
      Type listOpenType = typeof(List<>);
      Type listType = listOpenType.MakeGenericType(new Type[] { type });
      if (exprs.Any()) {
        return Expression.ListInit(Expression.New(listType), exprs);
      } else {
        return Expression.New(listType);
      }
    }

    public override string Format {
      get {
        var delimited = String.Join(", ", Arguments.Select(a => a.Format));
        return "{ " + delimited + " }";
      }
    }
  }

  public enum SymbolicValueType {
    FIRST,
    LAST,
    SIZE
  }

  class SymbolicValueExpr : Expr {
    public SymbolicValueType SymbolType { get; private set; }

    public SymbolicValueExpr(SymbolicValueType symbolType) {
      SymbolType = symbolType;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      // SymbolicValueExpr is special. It needs to know about what it's being called on to do anything useful.
      // See FunctionExpr objectFrom:withIndex:
      return Expression.Constant(0);
    }

    public override string Format {
      get {
        switch (SymbolType) {
          case SymbolicValueType.FIRST:
            return "FIRST";
          case SymbolicValueType.LAST:
            return "LAST";
          case SymbolicValueType.SIZE:
            return "SIZE";
        }
        return null;
      }
    }
  }

  class VariableAssignmentExpr : Expr {
    public VariableAssignmentExpr(string lhs, Expr rhs) {
      Variable = lhs;
      RightExpression = rhs;
    }

    public override Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect) {
      var valueExpression = RightExpression.LinqExpression(bindings, dialect);
      var variableExpression = Expression.Variable(valueExpression.Type, Variable);
      bindings[Variable] = variableExpression;
      var assignExpression = Expression.Assign(variableExpression, valueExpression);
      Expression blockExpr = Expression.Block(
          new ParameterExpression[] { variableExpression },
          assignExpression
      );
      return blockExpr;
    }

    public override string Format {
      get {
        return $"{Variable} := {RightExpression.Format}";
      }
    }
  }
}

