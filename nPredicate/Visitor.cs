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
}
