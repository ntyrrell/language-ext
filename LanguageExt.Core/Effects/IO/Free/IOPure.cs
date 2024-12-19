using System;
using LanguageExt.DSL;
using LanguageExt.Traits;

namespace LanguageExt;

record IOPure<A>(A Value) : IO<A>
{
    public override IO<B> Map<B>(Func<A, B> f) =>
        IO<B>.Lift(IODsl.Map(Value, f));

    public override IO<B> Bind<B>(Func<A, K<IO, B>> f) =>
        new IOBind<A, B>(Value, f);
}
