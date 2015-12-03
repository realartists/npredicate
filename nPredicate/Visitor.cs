namespace RealArtists.NPredicate {
  using System;
  using System.Linq;

  public interface IVisitor {
    void Visit(Predicate p);
    void Visit(Expr e);
  }

  public class Visitor : IVisitor {
    public virtual void Visit(Predicate p) { }
    public virtual void Visit(Expr e) { }
  }
  
  public class GuidRewriter : Visitor {
    private static bool LooksLikeGuid(string str) {
      Guid ignored;
      bool parseable = Guid.TryParse(str, out ignored);
      return parseable;
    }

    public override void Visit(Expr e) {
      if (e.ExpressionType == ExpressionType.ConstantValueExpressionType && e.ConstantValue is string && LooksLikeGuid(e.ConstantValue)) {
        e.ConstantValue = Guid.Parse(e.ConstantValue);
      }
    }
  }
}
